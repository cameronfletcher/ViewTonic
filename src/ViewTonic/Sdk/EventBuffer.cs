// <copyright file="EventBuffer.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Concurrent;
    using System.Diagnostics;
    using System.Threading;

    public sealed class EventBuffer : IEventBuffer, IDisposable
    {
        private readonly ConcurrentDictionary<long, Event> buffer = new ConcurrentDictionary<long, Event>();
        private readonly object @lock = new object();

        private BlockingCollection<Event> queue = new BlockingCollection<Event>(100000);

        public EventBuffer()
            : this(0L)
        {
        }

        public EventBuffer(long sequenceNumber)
        {
            Trace.WriteLine("EventBuffer: .ctor(long sequenceNumber)");

            this.NextSequenceNumber = sequenceNumber + 1;
        }

        public long NextSequenceNumber { get; private set; }

        public bool IsEmpty
        {
            get { return this.queue.Count == 0; }
        }

        public void Add(Event @event)
        {
            Trace.WriteLine("EventBuffer: Add(Event @event)");

            Guard.Against.Null(() => @event);
            Guard.Against.Negative(() => @event.SequenceNumber);
            Guard.Against.Null(() => @event.Payload);

            lock (this.@lock)
            {
                if (@event.SequenceNumber < this.NextSequenceNumber || this.buffer.ContainsKey(@event.SequenceNumber))
                {
                    // NOTE (Cameron): We have already buffered this item.
                    return;
                }

                if (@event.SequenceNumber != this.NextSequenceNumber)
                {
                    this.buffer.TryAdd(@event.SequenceNumber, @event);
                    return;
                }

                this.queue.Add(@event);
                this.NextSequenceNumber = @event.SequenceNumber + 1;

                while (this.buffer.TryRemove(this.NextSequenceNumber, out @event))
                {
                    this.queue.Add(@event);
                    this.NextSequenceNumber++;
                }
            }
        }

        public Event Take(CancellationToken cancellationToken)
        {
            Trace.WriteLine("EventBuffer: Take(CancellationToken cancellationToken)");

            Event value = null;
            this.queue.TryTake(out value, Timeout.Infinite, cancellationToken);
            return value;
        }

        public bool TryTake(out Event value)
        {
            Trace.WriteLine("EventBuffer: TryTake(out Event value)");

            return this.queue.TryTake(out value, 0);
        }

        public void Clear()
        {
            Trace.WriteLine("EventBuffer: Clear()");

            lock (this.@lock)
            {
                this.buffer.Clear();

                // LINK (Cameron): http://stackoverflow.com/questions/8001133/how-to-empty-a-blockingcollection
                var oldQueue = this.queue;
                this.queue = new BlockingCollection<Event>(100000);
                oldQueue.Dispose();
                
                this.NextSequenceNumber = 1L;
            }
        }

        public void Dispose()
        {
            Trace.WriteLine("EventBuffer: Dispose()");

            this.queue.Dispose();
        }
    }
}
