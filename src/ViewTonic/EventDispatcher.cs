// <copyright file="EventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System;
    using ViewTonic.Sdk;

    public sealed class EventDispatcher : IEventDispatcher
    {
        public EventDispatcher()
            : this(new DefaultSequenceNumberResolver())
        {
        }

        public EventDispatcher(ISequenceNumberResolver sequenceNumberResolver)
        {
        }

        public void Dispatch<T>(T @event)
        {
            throw new NotImplementedException();
        }
    }
}
