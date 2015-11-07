// <copyright file="IEventBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Threading;

    public interface IEventBuffer
    {
        long NextSequenceNumber { get; }

        bool IsEmpty { get; }

        void Add(Event @event);

        Event Take(CancellationToken cancellationToken);

        bool TryTake(out Event value);

        void Clear();
    }
}
