// <copyright file="IntelligentBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class OrderedBuffer
    {
        private readonly ConcurrentDictionary<long, Item> buffer = new ConcurrentDictionary<long, Item>();
        private readonly ConcurrentQueue<Item> queue = new ConcurrentQueue<Item>();

        private long nextSequenceNumber;

        public OrderedBuffer(long sequenceNumber)
        {
            Guard.Against.Negative(() => sequenceNumber);

            this.nextSequenceNumber = sequenceNumber + 1;
        }

        public void Add(long sequenceNumber, object value)
        {
            Guard.Against.Negative(() => sequenceNumber);
            Guard.Against.Null(() => value);

            if (sequenceNumber < this.nextSequenceNumber)
            {
                // NOTE (Cameron): We have already processed this event.
                return;
            }

            var item = new Item
            {
                SequenceNumber = sequenceNumber,
                Value = value,
            };

            if (sequenceNumber > this.nextSequenceNumber)
            {
                // NOTE (Cameron): An out-of-order item is being buffered.
                this.buffer.TryAdd(sequenceNumber, item);
                return;
            }

            this.queue.Enqueue(item);
            this.nextSequenceNumber++;

            Item nextItem = null;
            while (this.buffer.TryRemove(nextSequenceNumber, out nextItem))
            {
                this.queue.Enqueue(nextItem);
            }
        }

        public bool TryTake(out Item value)
        {
            return this.queue.TryDequeue(out value);
        }

        // TODO (Cameron): Consider struct?
        public class Item
        {
            public long SequenceNumber { get; set; }

            public object Value { get; set; }
        }
    }
}
