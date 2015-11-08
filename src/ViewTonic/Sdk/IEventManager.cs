// <copyright file="IEventManager.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Threading;

    public interface IEventManager
    {
        bool IsEventReplaying { get; }

        void Add(object eventPayload);

        Event Take(CancellationToken cancellationToken);

        bool TryTake(out Event value);

        void ReplayEvents();
    }
}
