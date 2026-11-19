using System.Buffers;
using Newtonsoft.Json;

// Only for JsonTextReader: JsonTextWriter returns its old buffer before renting a new one, so a failed rent would return it twice.
sealed class JsonArrayPool(ArrayPool<char> pool) : IArrayPool<char>
{
    // Larger buffers are dropped so one huge saga or outbox record doesn't keep them in the shared pool.
    internal const int MaxPooledSize = 1024 * 1024;

    public static readonly JsonArrayPool Instance = new(ArrayPool<char>.Shared);

    public char[] Rent(int minimumLength) => pool.Rent(minimumLength);

    public void Return(char[] array)
    {
        if (array is { Length: > 0 and <= MaxPooledSize })
        {
            pool.Return(array, clearArray: true);
        }
    }
}
