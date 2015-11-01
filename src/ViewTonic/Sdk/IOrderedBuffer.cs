// <copyright file="IOrderedBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Threading;

    public interface IOrderedBuffer
    {
        void Add(long sequenceNumber, object value);

        OrderedItem Take(CancellationToken cancellationToken);

        bool TryTake(out OrderedItem value);
    }
}
