// <copyright file="EventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System;
using System.Collections.Generic;
using ViewTonic.Sdk;

    public sealed class EventDispatcher : IEventDispatcher
    {
        private readonly Dictionary<long, string> bufferedEvents = new Dictionary<long, string>();

        private readonly ISequenceNumberResolver sequenceNumberResolver;

        private long currentSequenceNumber;

        public EventDispatcher()
            : this(new DefaultSequenceNumberResolver())
        {
        }

        public EventDispatcher(ISequenceNumberResolver sequenceNumberResolver)
        {
        }

        public void Dispatch<T>(T @event)
        {
            var sequenceNumber = this.sequenceNumberResolver.GetSequenceNumber(@event);
            if (sequenceNumber <= this.currentSequenceNumber)
            {
                // NOTE (Cameron): We have already processed this event.
                return;
            }

            if (sequenceNumber > this.currentSequenceNumber + 1)
            {
                this.bufferedEvents.Add(sequenceNumber, @event.ToString());
                return;
            }

            // process
        }
    }
}
