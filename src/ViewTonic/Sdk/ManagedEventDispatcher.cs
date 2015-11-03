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
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly Dictionary<View, long> views;
        private readonly Timer timer;

        private readonly ISequenceResolver sequenceResolver;
        private readonly IRepository<string, SequenceInfo> sequenceRepository;
        private readonly int quietTimeTimeout;

        private readonly IOrderedBuffer buffer;

        private long sequenceNumber;
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
            this.BeginDispatch();

            foreach (var view in views.Select(kvp => new { Impl = kvp.Key, SequenceNumber = kvp.Value }))
            {
                if (item.SequenceNumber > view.SequenceNumber)
                {
                    view.Impl.Apply(item.Value);
                }
            }

            this.sequenceNumber = item.SequenceNumber;

            this.EndDispatch();
        }

        private void BeginDispatch()
        {
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        private void EndDispatch()
        {
            this.timer.Change(this.quietTimeTimeout, Timeout.Infinite);
        }

        private void TimerCallback(object state)
        {
            // take first view with lowest sequence number
            var item = this.views
                .Select(kvp => new { View = kvp.Key, SequenceNumber = kvp.Value })
                .FirstOrDefault(view => view.SequenceNumber == views.Values.Min());

            // lock
            // wait until end dispatch is called - it may not be being called
            // snapshot
            item.View.Snapshot();
            // allow end dispatch to finish (if it's running)
            // unlock

            this.sequenceRepository.AddOrUpdate(item.View.GetType().FullName, new SequenceInfo { SequenceNumber = 0L });
            item.View.Flush();
            this.sequenceRepository.AddOrUpdate(item.View.GetType().FullName, new SequenceInfo { SequenceNumber = this.sequenceNumber });

            // save 
            // if no activity then continue with next view else return
        }
    }
}
