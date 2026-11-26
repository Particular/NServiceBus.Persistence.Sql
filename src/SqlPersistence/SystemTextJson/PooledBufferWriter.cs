using System;
using System.Buffers;

// Arrays larger than MaxPooledSize are dropped on return so one huge record doesn't keep them in the shared pool.
sealed class PooledBufferWriter<T> : IBufferWriter<T>, IDisposable
{
    internal const int InitialSize = 4096;
    internal const int MaxPooledSize = 1024 * 1024;

    readonly ArrayPool<T> pool;
    T[] buffer;
    int written;

    public PooledBufferWriter(ArrayPool<T> pool, int initialSize = InitialSize)
    {
        this.pool = pool;
        buffer = pool.Rent(initialSize);
    }

    public ReadOnlySpan<T> WrittenSpan
    {
        get
        {
            ObjectDisposedException.ThrowIf(buffer is null, this);
            return buffer.AsSpan(0, written);
        }
    }

    public void Advance(int count)
    {
        ObjectDisposedException.ThrowIf(buffer is null, this);
        ArgumentOutOfRangeException.ThrowIfNegative(count);
        if (count > buffer.Length - written)
        {
            throw new InvalidOperationException("Cannot advance past the end of the buffer.");
        }

        written += count;
    }

    public Memory<T> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return buffer.AsMemory(written);
    }

    public Span<T> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return buffer.AsSpan(written);
    }

    public void Dispose()
    {
        if (buffer is null)
        {
            return;
        }

        Return(buffer);
        buffer = null;
        written = 0;
    }

    void EnsureCapacity(int sizeHint)
    {
        ObjectDisposedException.ThrowIf(buffer is null, this);
        ArgumentOutOfRangeException.ThrowIfNegative(sizeHint);

        var required = (long)written + Math.Max(sizeHint, 1);
        if (required <= buffer.Length)
        {
            return;
        }

        if (required > Array.MaxLength)
        {
            throw new OutOfMemoryException($"Buffer exceeds the maximum array length of {Array.MaxLength}.");
        }

        var newBuffer = pool.Rent((int)Math.Min(Math.Max(required, buffer.Length * 2L), Array.MaxLength));
        buffer.AsSpan(0, written).CopyTo(newBuffer);

        var oldBuffer = buffer;
        buffer = newBuffer;
        Return(oldBuffer);
    }

    void Return(T[] array)
    {
        if (array.Length <= MaxPooledSize)
        {
            pool.Return(array, clearArray: true);
        }
    }
}