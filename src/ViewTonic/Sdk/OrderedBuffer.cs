// <copyright file="OrderedBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Collections.Concurrent;
    using System.Threading;

    public class OrderedBuffer : IOrderedBuffer
    {
        private readonly ConcurrentDictionary<long, OrderedItem> buffer = new ConcurrentDictionary<long, OrderedItem>();
        private readonly BlockingCollection<OrderedItem> queue = new BlockingCollection<OrderedItem>();

        private readonly object @lock = new object();

        public OrderedBuffer(long sequenceNumber)
        {
            Guard.Against.Negative(() => sequenceNumber);

            this.NextSequenceNumber =  sequenceNumber + 1;
        }

        protected long NextSequenceNumber { get; private set; }

        protected bool HasItemsBuffered
        {
            get { return !this.buffer.IsEmpty; }
        }

        protected bool HasItemsQueued
        {
            get { return this.queue.Count > 0; }
        }

        public void Add(long sequenceNumber, object value)
        {
            Guard.Against.Negative(() => sequenceNumber);
            Guard.Against.Null(() => value);

            lock (@lock)
            {
                if (sequenceNumber < this.NextSequenceNumber || this.buffer.ContainsKey(sequenceNumber))
                {
                    // NOTE (Cameron): We have already buffered this item.
                    return;
                }

                var item = new OrderedItem
                {
                    SequenceNumber = sequenceNumber,
                    Value = value,
                };

                if (sequenceNumber != this.NextSequenceNumber)
                {
                    this.buffer.TryAdd(item.SequenceNumber, item);
                    this.ItemAddedToBuffer(item.SequenceNumber);
                    return;
                }

                this.queue.Add(item);
                this.NextSequenceNumber = sequenceNumber + 1;

                while (this.buffer.TryRemove(this.NextSequenceNumber, out item))
                {
                    this.queue.Add(item);
                    this.ItemRemovedFromBuffer(item.SequenceNumber);
                    this.NextSequenceNumber++;
                }
            }
        }

        public OrderedItem Take(CancellationToken cancellationToken)
        {
            OrderedItem value = null;
            this.queue.TryTake(out value, -1, cancellationToken);
            return value;
        }

        public virtual bool TryTake(out OrderedItem value)
        {
            return this.queue.TryTake(out value, 0);
        }

        protected virtual void ItemAddedToBuffer(long sequenceNumber)
        {
        }

        protected virtual void ItemRemovedFromBuffer(long sequenceNumber)
        {
        }
    }
}
