// <copyright file="BufferTests.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Unit
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using ViewTonic.Sdk;
    using Xunit;

    public abstract class BufferTests : IDisposable
    {
        private readonly OrderedBuffer buffer;

        protected BufferTests(OrderedBuffer buffer)
        {
            this.buffer = buffer;
        }

        [Fact]
        public void CannotGetItemFromBufferWithNoItems()
        {
            // arrange
            OrderedItem item;

            // act
            var hasContents = this.buffer.TryTake(out item);

            // assert
            hasContents.Should().BeFalse();
            item.Should().BeNull();
        }

        [Fact]
        public void CannotGetItemFromBufferWithAlreadyProcessedItem()
        {
            // arrange
            OrderedItem item;

            // act
            this.buffer.Add(1, "value");
            var hasContents = this.buffer.TryTake(out item);

            // assert
            hasContents.Should().BeFalse();
            item.Should().BeNull();
        }

        [Fact]
        public void CanGetItemFromBufferWithPendingItem()
        {
            // arrange
            var expectedValue = "expected";
            OrderedItem item;

            // act
            this.buffer.Add(1, "value");
            this.buffer.Add(2, "value2");
            this.buffer.Add(3, expectedValue);
            var hasContents = this.buffer.TryTake(out item);

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
            var expectedValue = "expected";
            OrderedItem item;

            // act
            this.buffer.Add(1, "value");
            this.buffer.Add(2, "value2");
            this.buffer.Add(3, "value3");
            this.buffer.Add(4, expectedValue);
            var hasContents = this.buffer.TryTake(out item);
            hasContents = this.buffer.TryTake(out item);

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
            var expectedValue = "expected";
            OrderedItem item;
            OrderedItem otherItem;

            // act
            this.buffer.Add(1, "value");
            this.buffer.Add(2, "value2");
            this.buffer.Add(3, expectedValue);
            this.buffer.Add(5, "value5");
            var hasContents = this.buffer.TryTake(out item);
            var hasMoreContents = this.buffer.TryTake(out otherItem);

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
            var expectedValue = "expected";
            OrderedItem item;

            // act
            this.buffer.Add(1, "value");
            this.buffer.Add(2, "value2");
            this.buffer.Add(3, "value3");
            this.buffer.Add(5, "value5");
            this.buffer.Add(6, expectedValue);
            this.buffer.Add(4, "value4");
            var hasContents = this.buffer.TryTake(out item);
            hasContents = this.buffer.TryTake(out item);
            hasContents = this.buffer.TryTake(out item);
            hasContents = this.buffer.TryTake(out item);

            // assert
            hasContents.Should().BeTrue();
            item.Should().NotBeNull();
            item.SequenceNumber.Should().Be(6);
            item.Value.Should().Be(expectedValue);
        }

        [Fact]
        public void CanGetItemFromBufferOnBlockingThread()
        {
            // arrange
            var expectedValue = "expected";
            OrderedItem item;
            var token = new CancellationToken();

            // act
            new Task(
                () =>
                {
                    this.buffer.Add(1, "value");
                    this.buffer.Add(2, "value2");
                    this.buffer.Add(3, expectedValue);
                }).Start();

            item = this.buffer.Take(token);

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
            OrderedItem item = null;
            var tokenSource = new CancellationTokenSource();

            // act
            new Task(
                () =>
                {
                    try
                    {
                        item = this.buffer.Take(tokenSource.Token);
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

        public void Dispose()
        {
            var disposable = this.buffer as IDisposable;
            if (disposable != null)
            {
                disposable.Dispose();
            }
        }
    }
}
