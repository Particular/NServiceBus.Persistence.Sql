using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;

sealed class CharArrayTextWriter : TextWriter
{
    internal const int InitialSize = 4096;
    static readonly Encoding EncodingValue = new UnicodeEncoding(false, false);
    char[] chars = new char[InitialSize];
    int next;
    int length = InitialSize;

    public override Encoding Encoding => EncodingValue;

    public override void Write(char value)
    {
        Ensure(1);
        chars[next] = value;
        next += 1;
    }

    void Ensure(int i)
    {
        var required = next + i;
        if (required < length)
        {
            return;
        }

        while (required >= length)
        {
            length *= 2;
        }
        Array.Resize(ref chars, length);
    }

    public override void Write(char[] buffer, int index, int count)
    {
        Ensure(count);
        Array.Copy(buffer, index, chars, next, count);
        next += count;
    }

    public override void Write(string value)
    {
        var length = value.Length;
        Ensure(length);
        value.CopyTo(0, chars, next, length);
        next += length;
    }

    public override Task WriteAsync(char value)
    {
        Write(value);
        return CompletedTask;
    }

    public override Task WriteAsync(string value)
    {
        Write(value);
        return CompletedTask;
    }

    public override Task WriteAsync(char[] buffer, int index, int count)
    {
        Write(buffer, index, count);
        return CompletedTask;
    }

    public override Task WriteLineAsync(char value)
    {
        WriteLine(value);
        return CompletedTask;
    }

    public override Task WriteLineAsync(string value)
    {
        WriteLine(value);
        return CompletedTask;
    }

    public override Task WriteLineAsync(char[] buffer, int index, int count)
    {
        WriteLine(buffer, index, count);
        return CompletedTask;
    }

    public override Task FlushAsync()
    {
        return CompletedTask;
    }

    public void Release()
    {
        Clear();
        pool.Push(this);
    }

    static readonly ConcurrentStack<CharArrayTextWriter> pool = new ConcurrentStack<CharArrayTextWriter>();
    static readonly Task CompletedTask = Task.FromResult(true);

    public static CharArrayTextWriter Lease()
    {
        if (pool.TryPop(out var writer))
        {
            return writer;
        }

        return new CharArrayTextWriter();
    }

    public ArraySegment<char> ToCharSegment()
    {
        return new ArraySegment<char>(chars, 0, next);
    }

    void Clear()
    {
        next = 0;
    }

    public int Size => next;
}