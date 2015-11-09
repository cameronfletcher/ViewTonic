// <copyright file="EventManager.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class EventManager : IEventManager, IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        
        private readonly ISequenceResolver sequenceResolver;
        private readonly IEventResolver eventResolver;
        private readonly IEventBuffer eventBuffer;
        private readonly Timer timer;
        private readonly int timeout;

        private bool isDisposed;
        private bool disposeBuffer;

        public EventManager()
            : this(new NullSequenceResolver())
        {
        }

        public EventManager(ISequenceResolver sequenceResolver)
            : this(sequenceResolver, new NullEventResolver(), Timeout.Infinite, new EventBuffer(0L), true)
        {
        }

        public EventManager(ISequenceResolver sequenceResolver, IEventBuffer eventBuffer)
            : this(sequenceResolver, new NullEventResolver(), Timeout.Infinite, eventBuffer, false)
        {
        }

        public EventManager(ISequenceResolver sequenceResolver, IEventResolver eventResolver, int timeout)
            : this(sequenceResolver, eventResolver, timeout, new EventBuffer(0L), true)
        {
        }

        public EventManager(ISequenceResolver sequenceResolver, IEventResolver eventResolver, int timeout, IEventBuffer eventBuffer)
            : this(sequenceResolver, eventResolver, timeout, eventBuffer, false)
        {
        }

        internal EventManager(ISequenceResolver sequenceResolver, IEventResolver eventResolver, int timeout, IEventBuffer eventBuffer, bool disposeBuffer)
        {
            Trace.WriteLine("EventManager.ctor(ISequenceResolver sequenceResolver, IEventResolver eventResolver, IEventBuffer eventBuffer, int timeout, bool disposeBuffer)");

            Guard.Against.Null(() => sequenceResolver);
            Guard.Against.Null(() => eventResolver);
            Guard.Against.Null(() => eventBuffer);
            Guard.Against.NegativeTimeout(() => timeout);

            this.sequenceResolver = sequenceResolver;
            this.eventResolver = eventResolver;
            this.eventBuffer = eventBuffer;
            this.timeout = timeout;
            this.disposeBuffer = disposeBuffer;

            Trace.WriteLine("EventManager.ctor: Timer.ctor(Timeout.Infinite, Timeout.Infinite)");
            this.timer = new Timer(state => this.ResolveMissingEvents(this.cancellationTokenSource.Token), null, Timeout.Infinite, Timeout.Infinite);

            // NOTE (Cameron): This will immediately resolve all missing events from the specified sequence number (eg. event re-playing).
            this.IsEventReplaying = true;
            new Task(() => this.ResolveMissingEvents(this.cancellationTokenSource.Token)).Start();
        }

        public bool IsEventReplaying { get; private set; }

        public void Add(object eventPayload)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            var sequenceNumber = this.sequenceResolver.GetSequenceNumber(eventPayload);

            this.eventBuffer.Add(
                new Event
                {
                    SequenceNumber = sequenceNumber,
                    Payload = eventPayload,
                });
        }

        public Event Take(CancellationToken cancellationToken)
        {
            Trace.WriteLine("EventManager.Take(CancellationToken cancellationToken)");

            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            if (!this.timeout.Equals(Timeout.Infinite) && !this.IsEventReplaying)
            {
                Trace.WriteLine("EventManager.Take: Timer.Change(" + this.timeout.ToString() + ", Timeout.Infinite)");
                this.timer.Change(this.timeout, Timeout.Infinite);
            }

            var linkedSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, this.cancellationTokenSource.Token);

            try
            {
                return this.eventBuffer.Take(linkedSource.Token);
            }
            catch (OperationCanceledException)
            {
                if (this.isDisposed)
                {
                    this.cancellationTokenSource.Dispose();
                }

                throw;
            }
        }

        public bool TryTake(out Event value)
        {
            Trace.WriteLine("EventManager.TryTake(out Event value)");

            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            // TODO (Cameron): Consider what kind of functionality we want here with the timer.
            return this.eventBuffer.TryTake(out value);
        }

        public void ReplayEvents()
        {
            Trace.WriteLine("EventManager.ReplayEvents()");

            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            // NOTE (Cameron): This is not sufficient behaviour - we need to cancel and restart - will require another cancellation token methinks.
            if (this.IsEventReplaying)
            {
                throw new NotSupportedException("Already replaying events!");
            }

            Trace.WriteLine("EventManager.ReplayEvents: Timer.Change(Timeout.Infinite, Timeout.Infinite)");
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);

            this.eventBuffer.Clear();

            this.IsEventReplaying = true;
            new Task(() => this.ResolveMissingEvents(this.cancellationTokenSource.Token)).Start();
        }

        public void Dispose()
        {
            Trace.WriteLine("EventManager.Dispose()");

            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            this.cancellationTokenSource.Cancel();

            Trace.WriteLine("EventManager.Dispose: Timer.Dispose()");
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.timer.Dispose();

            if (this.disposeBuffer)
            {
                var disposable = this.eventBuffer as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private void ResolveMissingEvents(CancellationToken cancellationToken)
        {
            Trace.WriteLine("EventManager.ResolveMissingEvents()");

            // NOTE (Cameron): Check the resolver for any missing events from the last processed up to the latest event.
            var sequenceNumber = this.eventBuffer.NextSequenceNumber;

            foreach (var eventPayload in this.eventResolver.GetEventsFrom(sequenceNumber))
            {
                Trace.WriteLine("EventManager.ResolveMissingEvents: this.eventBuffer.Add({" + sequenceNumber.ToString() + "})");
                this.eventBuffer.Add(
                    new Event
                    {
                        SequenceNumber = sequenceNumber,
                        Payload = eventPayload,
                    });

                sequenceNumber++;

                if (cancellationToken.IsCancellationRequested || this.isDisposed)
                {
                    this.cancellationTokenSource.Dispose();
                    return;
                }
            }

            this.IsEventReplaying = false;

            if (!this.timeout.Equals(Timeout.Infinite))
            {
                try
                {
                    Trace.WriteLine("EventManager.ResolveMissingEvents: Timer.Change(" + this.timeout.ToString() + ", Timeout.Infinite)");
                    this.timer.Change(this.timeout, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // NOTE (Cameron): Dispose has been called during execution so swallow and leave to garbage collector.
                }
            }
        }

        internal class NullSequenceResolver : ISequenceResolver
        {
            private long sequenceNumber = 1L;

            public long GetSequenceNumber(object eventPayload)
            {
                return sequenceNumber++;
            }
        }

        internal class NullEventResolver : IEventResolver
        {
            public IEnumerable<object> GetEventsFrom(long sequenceNumber)
            {
                return Enumerable.Empty<object>();
            }
        }
    }
}
