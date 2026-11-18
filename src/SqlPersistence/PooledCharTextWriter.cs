using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

sealed class PooledCharTextWriter : TextWriter
{
    internal const int InitialSize = 4096;
    // Larger buffers are dropped so a rare huge saga doesn't park its whole growth chain in the shared pool.
    internal const int MaxPooledSize = 1024 * 1024;
    static readonly Encoding EncodingValue = new UnicodeEncoding(false, false);

    char[] buffer = ArrayPool<char>.Shared.Rent(InitialSize);
    int length;

    public override Encoding Encoding => EncodingValue;

    public override void Write(char value)
    {
        EnsureCapacity(1);
        buffer[length++] = value;
    }

    public override void Write(char[] chars, int index, int count) => Write(chars.AsSpan(index, count));

    public override void Write(string value) => Write(value.AsSpan());

    public override void Write(ReadOnlySpan<char> chars)
    {
        EnsureCapacity(chars.Length);
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

    public override Task WriteAsync(ReadOnlyMemory<char> chars, CancellationToken cancellationToken = default)
    {
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

    public override string ToString() => new(buffer, 0, length);

    // Not done in Dispose because JsonTextWriter closes the writer before ToString is called.
    public void ReturnBuffer()
    {
        var toReturn = buffer;
        buffer = [];
        length = 0;
        Return(toReturn);
    }

    void EnsureCapacity(int count)
    {
        if (buffer.Length - length < count)
        {
            Grow(count);
        }
    }

    void Grow(int count)
    {
        // Saga data becomes a string, which is shorter than the longest array
        const int MaxStringLength = 0x3FFFFFDF;
        var required = (long)length + count;
        if (required > MaxStringLength)
        {
            throw new OutOfMemoryException($"Saga data exceeds the maximum string length of {MaxStringLength} characters.");
        }

        var newSize = (int)Math.Max(required, Math.Min(buffer.Length * 2L, MaxStringLength));
        var newBuffer = ArrayPool<char>.Shared.Rent(newSize);
        buffer.AsSpan(0, length).CopyTo(newBuffer);

        var oldBuffer = buffer;
        buffer = newBuffer;
        Return(oldBuffer);
    }

    static void Return(char[] array)
    {
        if (array.Length is > 0 and <= MaxPooledSize)
        {
            ArrayPool<char>.Shared.Return(array);
        }
    }
}
