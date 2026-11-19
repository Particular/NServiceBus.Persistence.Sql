using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

sealed class PooledCharTextWriter : TextWriter
{
    internal const int InitialSize = 4096;
    // Beyond this the writer moves to StringBuilder chunks, so huge payloads don't need one large contiguous working buffer.
    internal const int MaxPooledSize = 1024 * 1024;
    const int MaxChunkSize = 8000;
    const int MaxStringLength = 0x3FFFFFDF;
    static readonly Encoding EncodingValue = new UnicodeEncoding(false, false);

    char[] buffer = ArrayPool<char>.Shared.Rent(InitialSize);
    int length;
    StringBuilder overflow;

    public override Encoding Encoding => EncodingValue;

    public override void Write(char value)
    {
        if ((uint)length < (uint)buffer.Length)
        {
            buffer[length++] = value;
            return;
        }

        if (overflow is not null && overflow.Length < MaxStringLength)
        {
            overflow.Append(value);
            return;
        }

        Write(new ReadOnlySpan<char>(in value));
    }

    public override void Write(char[] chars, int index, int count)
    {
        ArgumentNullException.ThrowIfNull(chars);
        Write(chars.AsSpan(index, count));
    }

    public override void Write(string value) => Write(value.AsSpan());

    public override void Write(ReadOnlySpan<char> chars)
    {
        if (overflow is null && buffer.Length - length < chars.Length)
        {
            MakeRoom(chars.Length);
        }

        if (overflow is not null)
        {
            AppendToOverflow(chars);
            return;
        }

        chars.CopyTo(buffer.AsSpan(length));
        length += chars.Length;
    }

    public override Task WriteAsync(char value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(string value)
    {
        Write(value);
        return Task.CompletedTask;
    }

    public override Task WriteAsync(char[] chars, int index, int count)
    {
        Write(chars, index, count);
        return Task.CompletedTask;
    }

    public override void WriteLine(ReadOnlySpan<char> chars)
    {
        Write(chars);
        WriteLine();
    }

    public override Task WriteAsync(ReadOnlyMemory<char> chars, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
        {
            return Task.FromCanceled(cancellationToken);
        }

        Write(chars.Span);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char value)
    {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(string value)
    {
        WriteLine(value);
        return Task.CompletedTask;
    }

    public override Task WriteLineAsync(char[] chars, int index, int count)
    {
        WriteLine(chars, index, count);
        return Task.CompletedTask;
    }

    public override Task FlushAsync() => Task.CompletedTask;

    public override string ToString() => overflow?.ToString() ?? new string(buffer, 0, length);

    // Not done in Dispose because JsonTextWriter closes the writer before ToString is called.
    public void ReturnBuffer()
    {
        Return(buffer);
        buffer = [];
        length = 0;
        overflow = null;
    }

    void MakeRoom(int count)
    {
        var required = (long)length + count;
        if (required > MaxPooledSize)
        {
            MoveToOverflow();
            return;
        }

        var newBuffer = ArrayPool<char>.Shared.Rent((int)Math.Max(required, Math.Min(buffer.Length * 2L, MaxPooledSize)));
        buffer.AsSpan(0, length).CopyTo(newBuffer);

        var oldBuffer = buffer;
        buffer = newBuffer;
        Return(oldBuffer);
    }

    void MoveToOverflow()
    {
        overflow = new StringBuilder();
        AppendToOverflow(buffer.AsSpan(0, length));

        Return(buffer);
        buffer = [];
        length = 0;
    }

    void AppendToOverflow(ReadOnlySpan<char> chars)
    {
        if ((long)overflow.Length + chars.Length > MaxStringLength)
        {
            throw new OutOfMemoryException($"Serialized data exceeds the maximum string length of {MaxStringLength} characters.");
        }

        if (chars.Length <= MaxChunkSize)
        {
            overflow.Append(chars);
            return;
        }

        // Appended in slices because one large append makes StringBuilder allocate one large chunk.
        while (!chars.IsEmpty)
        {
            var slice = chars[..Math.Min(chars.Length, MaxChunkSize)];
            overflow.Append(slice);
            chars = chars[slice.Length..];
        }
    }

    static void Return(char[] array)
    {
        if (array.Length > 0)
        {
            ArrayPool<char>.Shared.Return(array, clearArray: true);
        }
    }
}
