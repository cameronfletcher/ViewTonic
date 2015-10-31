namespace ViewTonic.Tests.Unit
{
    using ViewTonic.Sdk;

    public class OrderedBufferTests : BufferTests
    {
        public OrderedBufferTests()
            : base(new OrderedBuffer(2))
        {
        }
    }
}
