using System.Buffers;
using Newtonsoft.Json;

sealed class JsonArrayPool : IArrayPool<char>
{
    // Larger buffers are dropped so one huge saga or outbox record doesn't keep them in the shared pool.
    internal const int MaxPooledSize = 1024 * 1024;

    public static readonly JsonArrayPool Instance = new();

    JsonArrayPool()
    {
    }

    public char[] Rent(int minimumLength) => ArrayPool<char>.Shared.Rent(minimumLength);

    public void Return(char[] array)
    {
        if (array.Length is > 0 and <= MaxPooledSize)
        {
            ArrayPool<char>.Shared.Return(array);
        }
    }
}
