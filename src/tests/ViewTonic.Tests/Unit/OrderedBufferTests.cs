namespace ViewTonic.Tests.Unit
{
    using System;
    using System.Threading;
    using FluentAssertions;
    using ViewTonic.Sdk;
    using Xunit;

    public class OrderedBufferTests
    {
        [Fact]
        public void CannotGetItemFromBufferWithNoItems()
        {
            // arrange
            var buffer = new OrderedBuffer(2);
            OrderedBuffer.Item item;

            // act
            var hasContents = buffer.TryTake(out item);

            // assert
            hasContents.Should().BeFalse();
            item.Should().BeNull();
        }

        [Fact]
        public void CannotGetItemFromBufferWithAlreadyProcessedItem()
        {
            // arrange
            var buffer = new OrderedBuffer(2);
            OrderedBuffer.Item item;

            // act
            buffer.Add(1, "value");
            var hasContents = buffer.TryTake(out item);

            // assert
            hasContents.Should().BeFalse();
            item.Should().BeNull();
        }

        [Fact]
        public void CanGetItemFromBufferWithPendingItem()
        {
            // arrange
            var buffer = new OrderedBuffer(2);
            var expectedValue = "expected";
            OrderedBuffer.Item item;

            // act
            buffer.Add(1, "value");
            buffer.Add(2, "value2");
            buffer.Add(3, expectedValue);
            var hasContents = buffer.TryTake(out item);

            // assert
            hasContents.Should().BeTrue();
            item.Should().NotBeNull();
            item.SequenceNumber.Should().Be(3);
            item.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void CanGetMultipleItemsFromBufferWithPendingItems()
        {
            // arrange
            var buffer = new OrderedBuffer(2);
            var expectedValue = "expected";
            OrderedBuffer.Item item;

            // act
            buffer.Add(1, "value");
            buffer.Add(2, "value2");
            buffer.Add(3, "value3");
            buffer.Add(4, expectedValue);
            var hasContents = buffer.TryTake(out item);
            hasContents = buffer.TryTake(out item);

            // assert
            hasContents.Should().BeTrue();
            item.Should().NotBeNull();
            item.SequenceNumber.Should().Be(4);
            item.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void CanGetItemFromBufferWithMissingItems()
        {
            // arrange
            var buffer = new OrderedBuffer(2);
            var expectedValue = "expected";
            OrderedBuffer.Item item;
            OrderedBuffer.Item otherItem;

            // act
            buffer.Add(1, "value");
            buffer.Add(2, "value2");
            buffer.Add(3, expectedValue);
            buffer.Add(5, "value5");
            var hasContents = buffer.TryTake(out item);
            var hasMoreContents = buffer.TryTake(out otherItem);

            // assert
            hasContents.Should().BeTrue();
            item.Should().NotBeNull();
            item.SequenceNumber.Should().Be(3);
            item.Value.Should().Be(expectedValue);
            hasMoreContents.Should().BeFalse();
            otherItem.Should().BeNull();
        }

        [Fact]
        public void CanGetMultipleItemsFromBufferWithOutOfOrderItems()
        {
            // arrange
            var buffer = new OrderedBuffer(2);
            var expectedValue = "expected";
            OrderedBuffer.Item item;

            // act
            buffer.Add(1, "value");
            buffer.Add(2, "value2");
            buffer.Add(3, "value3");
            buffer.Add(5, expectedValue);
            buffer.Add(4, "value4");
            var hasContents = buffer.TryTake(out item);
            hasContents = buffer.TryTake(out item);
            hasContents = buffer.TryTake(out item);

            // assert
            hasContents.Should().BeTrue();
            item.Should().NotBeNull();
            item.SequenceNumber.Should().Be(5);
            item.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void CanGetItemFromBufferOnBlockingThread()
        {
            // arrange
            var buffer = new OrderedBuffer(2);
            var expectedValue = "expected";
            OrderedBuffer.Item item;
            var token = new CancellationToken();

            // act
            new Thread(
                () =>
                {
                    buffer.Add(1, "value");
                    buffer.Add(2, "value2");
                    buffer.Add(3, expectedValue);
                }).Start();

            item = buffer.Take(token);

            // assert
            item.Should().NotBeNull();
            item.SequenceNumber.Should().Be(3);
            item.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void CanCancelBlockingTake()
        {
            // arrange
            var takeComplete = new ManualResetEvent(false);
            var buffer = new OrderedBuffer(2);
            OrderedBuffer.Item item = null;
            var tokenSource = new CancellationTokenSource();

            // act
            new Thread(
                () =>
                {
                    try
                    {
                        item = buffer.Take(tokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        takeComplete.Set();
                    }
                }).Start();

            tokenSource.Cancel();
            takeComplete.WaitOne();

            // assert
            item.Should().BeNull();
        }
    }
}
