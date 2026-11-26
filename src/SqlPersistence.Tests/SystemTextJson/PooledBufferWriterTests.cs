using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

public class PooledBufferWriterTests
{
    [Test]
    public void RentsTheInitialBufferFromThePool()
    {
        var pool = new RecordingPool();

        using var writer = new PooledBufferWriter<byte>(pool);

        Assert.That(pool.RentedLengths, Is.EqualTo(new[] { PooledBufferWriter<byte>.InitialSize }));
    }

    [Test]
    public void GrowthPreservesContent()
    {
        var pool = new RecordingPool();
        using var writer = new PooledBufferWriter<byte>(pool, 4);
        var content = new byte[10_000];
        new Random(42).NextBytes(content);

        foreach (var chunk in content.Chunk(3))
        {
            chunk.CopyTo(writer.GetSpan(chunk.Length));
            writer.Advance(chunk.Length);
        }

        Assert.That(writer.WrittenSpan.ToArray(), Is.EqualTo(content));
    }

    [Test]
    public void GetSpanProvidesAtLeastTheRequestedSize()
    {
        using var writer = new PooledBufferWriter<byte>(new RecordingPool(), 4);

        Assert.That(writer.GetSpan(1000).Length, Is.GreaterThanOrEqualTo(1000));
        Assert.That(writer.GetMemory(5000).Length, Is.GreaterThanOrEqualTo(5000));
    }

    [Test]
    public void DisposeReturnsTheBufferClearedAndOnlyOnce()
    {
        var pool = new RecordingPool();
        var writer = new PooledBufferWriter<byte>(pool);
        writer.GetSpan(1)[0] = 42;
        writer.Advance(1);

        writer.Dispose();
        writer.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(pool.Returned, Has.Count.EqualTo(1));
            Assert.That(pool.ClearedOnReturn, Is.EqualTo(new[] { true }));
        });
    }

    [Test]
    public void GrowthReturnsTheOutgrownBufferCleared()
    {
        var pool = new RecordingPool();
        using var writer = new PooledBufferWriter<byte>(pool, 16);
        var first = pool.Rented[0];

        writer.GetSpan(1000);

        Assert.Multiple(() =>
        {
            Assert.That(pool.Returned, Has.Count.EqualTo(1));
            Assert.That(pool.Returned[0], Is.SameAs(first));
            Assert.That(pool.ClearedOnReturn, Is.EqualTo(new[] { true }));
        });
    }

    [TestCase(1, true)]
    [TestCase(PooledBufferWriter<byte>.MaxPooledSize, true)]
    [TestCase(PooledBufferWriter<byte>.MaxPooledSize + 1, false)]
    public void ReturnsOnlyBuffersUpToTheLimit(int size, bool expectedToBeReturned)
    {
        var pool = new RecordingPool();
        var writer = new PooledBufferWriter<byte>(pool, size);
        var buffer = pool.Rented[0];

        writer.Dispose();

        Assert.That(pool.Returned.Contains(buffer), Is.EqualTo(expectedToBeReturned));
    }

    [Test]
    public void OutgrownBuffersBeyondTheLimitAreNotReturned()
    {
        var pool = new RecordingPool();
        var writer = new PooledBufferWriter<byte>(pool);

        writer.GetSpan(PooledBufferWriter<byte>.MaxPooledSize + 1);
        writer.GetSpan(PooledBufferWriter<byte>.MaxPooledSize * 3);
        writer.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(pool.Rented, Has.Count.EqualTo(3));
            Assert.That(pool.Returned, Has.Count.EqualTo(1));
            Assert.That(pool.Returned[0].Length, Is.LessThanOrEqualTo(PooledBufferWriter<byte>.MaxPooledSize));
        });
    }

    [Test]
    public void AdvancingPastTheBufferThrows()
    {
        using var writer = new PooledBufferWriter<byte>(new RecordingPool(), 16);

        Assert.Throws<InvalidOperationException>(() => writer.Advance(17));
    }

    [Test]
    public void UsingTheWriterAfterDisposeThrows()
    {
        var writer = new PooledBufferWriter<byte>(new RecordingPool());
        writer.Dispose();

        Assert.Throws<ObjectDisposedException>(() => writer.GetSpan());
    }

    sealed class RecordingPool : ArrayPool<byte>
    {
        public List<byte[]> Rented { get; } = [];
        public List<int> RentedLengths { get; } = [];
        public List<byte[]> Returned { get; } = [];
        public List<bool> ClearedOnReturn { get; } = [];

        public override byte[] Rent(int minimumLength)
        {
            var array = new byte[minimumLength];
            Rented.Add(array);
            RentedLengths.Add(minimumLength);
            return array;
        }

        public override void Return(byte[] array, bool clearArray = false)
        {
            Returned.Add(array);
            ClearedOnReturn.Add(clearArray);
        }
    }
}