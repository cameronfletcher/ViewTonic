// <copyright file="IntelligentBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Collections.Concurrent;
    using System.Threading;

    public class OrderedBuffer
    {
        private readonly ConcurrentDictionary<long, Item> buffer = new ConcurrentDictionary<long, Item>();
        private readonly BlockingCollection<Item> queue = new BlockingCollection<Item>();

        private readonly object @lock = new object();

        private long nextSequenceNumber;

        public OrderedBuffer(long sequenceNumber)
        {
            Guard.Against.Negative(() => sequenceNumber);

            this.nextSequenceNumber =  sequenceNumber + 1;
        }

        public void Add(long sequenceNumber, object value)
        {
            Guard.Against.Negative(() => sequenceNumber);
            Guard.Against.Null(() => value);

            if (sequenceNumber < this.nextSequenceNumber || this.buffer.ContainsKey(sequenceNumber))
            {
                // NOTE (Cameron): We have already buffered this item.
                return;
            }

            var item = new Item
            {
                SequenceNumber = sequenceNumber,
                Value = value,
            };

            lock (@lock)
            {
                if (sequenceNumber != this.nextSequenceNumber)
                {
                    this.buffer.TryAdd(sequenceNumber, item);
                    return;
                }

                this.queue.Add(item);
                this.nextSequenceNumber = sequenceNumber + 1;

                while (this.buffer.TryRemove(this.nextSequenceNumber, out item))
                {
                    this.queue.Add(item);
                }
            }
        }

        public Item Take(CancellationToken cancellationToken)
        {
            Item value = null;
            this.queue.TryTake(out value, -1, cancellationToken);
            return value;
        }

        public bool TryTake(out Item value)
        {
            return this.queue.TryTake(out value, 0);
        }

        // TODO (Cameron): Consider struct?
        public class Item
        {
            public long SequenceNumber { get; set; }

            public object Value { get; set; }
        }
    }
}
