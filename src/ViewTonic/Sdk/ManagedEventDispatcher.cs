// <copyright file="ManagedEventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using ViewTonic.Persistence;

    public sealed class ManagedEventDispatcher : IEventDispatcher
    {
        private static readonly SequenceInfo ZeroSequenceInfo = new SequenceInfo { SequenceNumber = 0L };

        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly object @lock = new object();

        private readonly Dictionary<View, long> views;
        private readonly Timer timer;

        private readonly ISequenceResolver sequenceResolver;
        private readonly IRepository<string, SequenceInfo> sequenceRepository;
        private readonly int quietTimeTimeout;

        private readonly IOrderedBuffer buffer;

        private long sequenceNumber;
        private bool processingSnapshot;
        private bool quietTimeDisturbed;
        private bool isDisposed;
        
        public ManagedEventDispatcher(
            IEnumerable<View> views, 
            ISequenceResolver sequenceResolver, 
            IRepository<string, SequenceInfo> sequenceRepository,
            Func<long, IOrderedBuffer> bufferFactory,
            int quietTimeTimeout)
        {
            Guard.Against.NullOrEmptyOrNullElements(() => views);

            this.views = views.ToDictionary(
                view => view, 
                view => 
                {
                    var sequenceInfo = sequenceRepository.Get(view.GetType().FullName);
                    return sequenceInfo == null ? 0L : sequenceInfo.SequenceNumber;
                });

            this.sequenceResolver = sequenceResolver;
            this.sequenceRepository = sequenceRepository;
            this.quietTimeTimeout = quietTimeTimeout;

            this.sequenceNumber = this.views.Values.Min();

            this.buffer = bufferFactory.Invoke(this.sequenceNumber);

            var task = new Task(() => this.Run(this.cancellationTokenSource.Token));
            task.ContinueWith(t => this.cancellationTokenSource.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            task.Start();

            this.timer = new Timer(this.TimerCallback, null, this.quietTimeTimeout, Timeout.Infinite);
        }

        public void Dispatch(object @event)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            var sequenceNumber = this.sequenceResolver.GetSequenceNumber(@event);
            this.buffer.Add(sequenceNumber, @event);
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.cancellationTokenSource.Cancel();
            this.isDisposed = true;
        }

        private void Run(CancellationToken token)
        {
            while (true)
            {
                OrderedItem item = null;
                try
                {
                    item = this.buffer.Take(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // NOTE (Cameron): The items here are coming out of the buffer in the correct order.
                this.Dispatch(item);
            }
        }

        private void Dispatch(OrderedItem item)
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);

            lock (this.@lock)
            {
                foreach (var view in views.Select(kvp => new { Impl = kvp.Key, SequenceNumber = kvp.Value }).AsParallel())
                {
                    if (item.SequenceNumber > view.SequenceNumber)
                    {
                        view.Impl.Apply(item.Value);
                    }
                }

                this.sequenceNumber = item.SequenceNumber;
            }

            if (this.processingSnapshot)
            {
                this.quietTimeDisturbed = true;
            }
            else
            {
                this.timer.Change(this.quietTimeTimeout, Timeout.Infinite);
            }
        }

        private void TimerCallback(object state)
        {
            long snapshotSequenceNumber;

            var processedViews = new List<View>();

            while (processedViews.Count < this.views.Count)
            {
                this.processingSnapshot = true;

                // take first view with lowest sequence number
                var itemsInScope = this.views
                    .Where(kvp => !processedViews.Contains(kvp.Key))
                    .Select(kvp => new { View = kvp.Key, SequenceNumber = kvp.Value });

                var item = itemsInScope
                    .FirstOrDefault(view => view.SequenceNumber == itemsInScope.Min(x => x.SequenceNumber));

                lock (this.@lock)
                {
                    item.View.Snapshot();
                    snapshotSequenceNumber = this.sequenceNumber;
                }

                this.sequenceRepository.AddOrUpdate(item.View.GetType().FullName, ZeroSequenceInfo);
                item.View.Flush();
                this.sequenceRepository.AddOrUpdate(item.View.GetType().FullName, new SequenceInfo { SequenceNumber = snapshotSequenceNumber });

                this.processingSnapshot = false;

                if (this.quietTimeDisturbed)
                {
                    this.quietTimeDisturbed = false;
                    this.timer.Change(this.quietTimeTimeout, Timeout.Infinite);
                    return;
                }
            }
        }
    }
}
