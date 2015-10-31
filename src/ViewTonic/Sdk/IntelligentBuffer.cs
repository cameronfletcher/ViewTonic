// <copyright file="IntelligentBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using ViewTonic.Sdk;

    public sealed class IntelligentBuffer
    {
        private readonly Dictionary<long, object> bufferedEvents = new Dictionary<long, object>();

        private readonly BlockingCollection<SequencedItem> blockingCollection = new BlockingCollection<SequencedItem>();

        private readonly Func<long, object> resolver;
        private readonly int resolverWaitTimeMilliseconds;

        private long lastProcessed;

        public IntelligentBuffer(Func<long, object> resolver, int resolverWaitTimeMilliseconds)
        {
            this.resolver = resolver;
            this.resolverWaitTimeMilliseconds = resolverWaitTimeMilliseconds;
        }

        public void StartBufferring(long sequenceNumber)
        {
            this.lastProcessed = sequenceNumber;

            // wait 5 second before first check?
            // reset background timer for last hit (every minute?)
        }

        public void StopBufferring()
        {

        }

        public void Add(long sequenceNumber, object @event)
        {
            // reset timer
            if (sequenceNumber <= this.lastProcessed)
            {
                // NOTE (Cameron): We have already processed this event.
                return;
            }

            if (sequenceNumber > this.lastProcessed + 1)
            {
                this.bufferedEvents.Add(sequenceNumber, @event.ToString());
                // set timer
                return;
            }

            // process
        }

        public object Take(out long sequenceNumber)
        {
            var item = this.blockingCollection.Take();
            sequenceNumber = item.SequenceNumber;
            return item.Value;
        }

        // TODO (Cameron): Consider struct?
        private class SequencedItem
        {
            public long SequenceNumber { get; set; }

            public object Value { get; set; }
        }
    }
}
