// <copyright file="IntelligentBufferTests.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Unit
{
    using System.Globalization;
    using System.Threading;
    using FluentAssertions;
    using ViewTonic.Sdk;
    using Xunit;

    public class IntelligentBufferTests : BufferTests
    {
        public IntelligentBufferTests()
            : base(new IntelligentOrderedBuffer(2, sequenceNumber => null, Timeout.Infinite))
        {
        }

        public class AdditionalTests
        {
            [Fact]
            public void CanRecoverFromMissingEvent()
            {
                // arrange
                var callCount = 0;
                var buffer = new IntelligentOrderedBuffer(
                    2,
                    sequenceNumber => callCount++ == 0 ? null : string.Format(CultureInfo.InvariantCulture, "event{0}", sequenceNumber),
                    20);

                // act
                buffer.Add(4, "event4");
                var item = buffer.Take(new CancellationToken());

                // assert
                item.Should().NotBeNull();
                item.SequenceNumber.Should().Be(3);
                item.Value.Should().Be("event3");
            }

            [Fact]
            public void CanRecoverWithEventReplaying()
            {
                // arrange
                var callCount = 0;
                var buffer = new IntelligentOrderedBuffer(
                    2,
                    sequenceNumber => callCount++ == 0 ? string.Format(CultureInfo.InvariantCulture, "event{0}", sequenceNumber) : null,
                    Timeout.Infinite);

                // act
                buffer.Add(4, "event4");
                var item = buffer.Take(new CancellationToken());

                // assert
                item.Should().NotBeNull();
                item.SequenceNumber.Should().Be(3);
                item.Value.Should().Be("event3");
            }
        }
    }
}
