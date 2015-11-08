// <copyright file="EventBufferTests.cs" company="ViewTonic contributors">
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

    public class EventBufferTests : IDisposable
    {
        private readonly EventBuffer buffer;

        public EventBufferTests()
        {
            this.buffer = new EventBuffer(2);
        }

        [Fact]
        public void CannotGetItemFromBufferWithNoItems()
        {
            // arrange
            Event @event;

            // act
            var hasContents = this.buffer.TryTake(out @event);

            // assert
            hasContents.Should().BeFalse();
            @event.Should().BeNull();
        }

        [Fact]
        public void CannotGetItemFromBufferWithAlreadyProcessedItem()
        {
            // arrange
            Event @event;

            // act
            this.buffer.Add(new Event { SequenceNumber = 1, Payload = "value" });
            var hasContents = this.buffer.TryTake(out @event);

            // assert
            hasContents.Should().BeFalse();
            @event.Should().BeNull();
        }

        [Fact]
        public void CanGetItemFromBufferWithPendingItem()
        {
            // arrange
            var expectedValue = "expected";
            Event @event;

            // act
            this.buffer.Add(new Event { SequenceNumber = 1, Payload = "value" });
            this.buffer.Add(new Event { SequenceNumber = 2, Payload = "value2" });
            this.buffer.Add(new Event { SequenceNumber = 3, Payload = expectedValue });
            var hasContents = this.buffer.TryTake(out @event);

            // assert
            hasContents.Should().BeTrue();
            @event.Should().NotBeNull();
            @event.SequenceNumber.Should().Be(3);
            @event.Payload.Should().Be(expectedValue);
        }

        [Fact]
        public void CanGetMultipleItemsFromBufferWithPendingItems()
        {
            // arrange
            var expectedValue = "expected";
            Event @event;

            // act
            this.buffer.Add(new Event { SequenceNumber = 1, Payload = "value" });
            this.buffer.Add(new Event { SequenceNumber = 2, Payload = "value2" });
            this.buffer.Add(new Event { SequenceNumber = 3, Payload = "value3" });
            this.buffer.Add(new Event { SequenceNumber = 4, Payload = expectedValue });
            var hasContents = this.buffer.TryTake(out @event);
            hasContents = this.buffer.TryTake(out @event);

            // assert
            hasContents.Should().BeTrue();
            @event.Should().NotBeNull();
            @event.SequenceNumber.Should().Be(4);
            @event.Payload.Should().Be(expectedValue);
        }

        [Fact]
        public void CanGetItemFromBufferWithMissingItems()
        {
            // arrange
            var expectedValue = "expected";
            Event @event;
            Event otherEvent;

            // act
            this.buffer.Add(new Event { SequenceNumber = 1, Payload = "value" });
            this.buffer.Add(new Event { SequenceNumber = 2, Payload = "value2" });
            this.buffer.Add(new Event { SequenceNumber = 3, Payload = expectedValue });
            this.buffer.Add(new Event { SequenceNumber = 5, Payload = "value5" });
            var hasContents = this.buffer.TryTake(out @event);
            var hasMoreContents = this.buffer.TryTake(out otherEvent);

            // assert
            hasContents.Should().BeTrue();
            @event.Should().NotBeNull();
            @event.SequenceNumber.Should().Be(3);
            @event.Payload.Should().Be(expectedValue);
            hasMoreContents.Should().BeFalse();
            otherEvent.Should().BeNull();
        }

        [Fact]
        public void CanGetMultipleItemsFromBufferWithOutOfOrderItems()
        {
            // arrange
            var expectedValue = "expected";
            Event @event;

            // act
            this.buffer.Add(new Event { SequenceNumber = 1, Payload = "value" });
            this.buffer.Add(new Event { SequenceNumber = 2, Payload = "value2" });
            this.buffer.Add(new Event { SequenceNumber = 3, Payload = "value3" });
            this.buffer.Add(new Event { SequenceNumber = 5, Payload = "value5" });
            this.buffer.Add(new Event { SequenceNumber = 6, Payload = expectedValue });
            this.buffer.Add(new Event { SequenceNumber = 4, Payload = "value4" });
            var hasContents = this.buffer.TryTake(out @event);
            hasContents = this.buffer.TryTake(out @event);
            hasContents = this.buffer.TryTake(out @event);
            hasContents = this.buffer.TryTake(out @event);

            // assert
            hasContents.Should().BeTrue();
            @event.Should().NotBeNull();
            @event.SequenceNumber.Should().Be(6);
            @event.Payload.Should().Be(expectedValue);
        }

        [Fact]
        public void CanGetItemFromBufferOnBlockingThread()
        {
            // arrange
            var expectedValue = "expected";
            Event @event;
            var token = new CancellationToken();

            // act
            new Task(
                () =>
                {
                    this.buffer.Add(new Event { SequenceNumber = 1, Payload = "value" });
                    this.buffer.Add(new Event { SequenceNumber = 2, Payload = "value" });
                    this.buffer.Add(new Event { SequenceNumber = 3, Payload = expectedValue });
                }).Start();

            @event = this.buffer.Take(token);

            // assert
            @event.Should().NotBeNull();
            @event.SequenceNumber.Should().Be(3);
            @event.Payload.Should().Be(expectedValue);
        }

        [Fact]
        public void CanCancelBlockingTake()
        {
            // arrange
            var takeComplete = new ManualResetEvent(false);
            Event @event = null;
            var tokenSource = new CancellationTokenSource();

            // act
            new Task(
                () =>
                {
                    try
                    {
                        @event = this.buffer.Take(tokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        takeComplete.Set();
                    }
                }).Start();

            tokenSource.Cancel();
            takeComplete.WaitOne();

            // assert
            @event.Should().BeNull();
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
