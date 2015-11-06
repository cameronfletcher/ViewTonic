// <copyright file="IntelligentOrderedBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    public sealed class IntelligentOrderedBuffer : OrderedBuffer, IDisposable
    {
        private readonly HashSet<long> buffer = new HashSet<long>();
        private readonly Timer timer;

        private readonly IEventResolver eventResolver;
        private readonly int timeout;

        private bool isDisposed;

        public IntelligentOrderedBuffer(long sequenceNumber, Func<long, object> eventResolver, int timeout)
            : this(sequenceNumber, new DefaultEventResolver(eventResolver), timeout)
        {
        }

        public IntelligentOrderedBuffer(long sequenceNumber, IEventResolver eventResolver, int timeout)
            : base(sequenceNumber)
        {
            this.eventResolver = eventResolver;
            this.timeout = timeout;

            // NOTE (Cameron): This will immediately resolve all missing events from the specified sequence number (eg. event re-playing).
            this.timer = new Timer(this.EnsureSmoothRunning, null, 0, Timeout.Infinite);
        }

        protected override void ItemAddedToBuffer(long sequenceNumber)
        {
            this.buffer.Add(sequenceNumber);
        }

        protected override void ItemRemovedFromBuffer(long sequenceNumber)
        {
            this.buffer.Remove(sequenceNumber);
        }

        public override bool TryTake(out OrderedItem value)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            var success = base.TryTake(out value);
            if (success)
            {
                this.timer.Change(this.timeout, Timeout.Infinite);
            }

            return success;
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.timer.Dispose();
            this.isDisposed = true;
        }

        private void EnsureSmoothRunning(object state)
        {
            if (this.HasItemsQueued)
            {
                // NOTE (Cameron): Items are queued so there must be no consumers => stop checking until a dequeue operation takes place.
                return;
            }

            // NOTE (Cameron): No items queued so we check the resolver for any additional items that were never added up to the first buffer hit.
            var sequenceNumber = this.NextSequenceNumber;
            object item;
            do
            {
                item = this.eventResolver.GetEvent(sequenceNumber);
                if (item != null)
                {
                    this.Add(sequenceNumber, item);
                    sequenceNumber++;
                }
            }
            while (item != null && !this.buffer.Contains(sequenceNumber));

            this.timer.Change(this.timeout, Timeout.Infinite);
        }
    }
}
