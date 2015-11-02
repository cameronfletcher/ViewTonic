// <copyright file="OrderedBufferTests.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

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
