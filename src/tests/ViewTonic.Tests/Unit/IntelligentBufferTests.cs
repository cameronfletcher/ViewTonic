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
            : base(new IntelligentOrderedBuffer(2, sequenceNumber => null, 1000, 1000))
        {
        }

        [Fact]
        public void CanResolveOnConsumerTimeout()
        {
            // arrange
            var buffer = new IntelligentOrderedBuffer(2, sequenceNumber => string.Format(CultureInfo.InvariantCulture, "event{0}", sequenceNumber), 1000, 100);

            // act
            buffer.Add(4, "event4");
            var item = buffer.Take(new CancellationToken());

            // assert
            item.Should().NotBeNull();
            item.SequenceNumber.Should().Be(3);
            item.Value.Should().Be("event3");
        }

        [Fact]
        public void CanResolveOnPublisherTimeout()
        {
            // arrange
            var buffer = new IntelligentOrderedBuffer(
                2, 
                sequenceNumber => (sequenceNumber == 3) ? string.Format(CultureInfo.InvariantCulture, "event{0}", sequenceNumber) : null, 
                100, 
                1000);

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
