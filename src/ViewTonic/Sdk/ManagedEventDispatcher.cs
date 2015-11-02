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

        private readonly ISequenceResolver sequenceResolver;
        private readonly IRepository<string, SequenceInfo> sequenceRepository;

        private readonly IOrderedBuffer buffer;


        private bool isDisposed;
        
        private long sequenceNumber;

        public ManagedEventDispatcher(
            IEnumerable<View> views, 
            ISequenceResolver sequenceResolver, 
            IRepository<string, SequenceInfo> sequenceRepository,
            Func<long, IOrderedBuffer> bufferFactory)
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

            this.buffer = bufferFactory.Invoke(this.views.Values.Min());

            var task = new Task(() => this.Run(this.cancellationTokenSource.Token));
            task.ContinueWith(t => this.cancellationTokenSource.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            task.Start();
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
            
            this.EndDispatch();
        }

        private void BeginDispatch()
        {
            // stop timer
        }

        private void EndDispatch()
        {
            // start timer
        }

        private void TimerCallback()
        {
            // take first view with lowest sequence number
            var sequenceNumber = 0L;
            View view = null;

            // get all hybrid repositories
            List<ISnapshotRepository> repositories = null;

            // lock
            // wait until end dispatch is called - it may not be being called
            // snapshot
            repositories.ForEach(repository => repository.TakeSnapshot());
            // allow end dispatch to finish (if it's running)
            // unlock

            this.sequenceRepository.AddOrUpdate(view.GetType().FullName, new SequenceInfo { SequenceNumber = 0L });
            repositories.ForEach(repository => repository.FlushSnapshot());
            this.sequenceRepository.AddOrUpdate(view.GetType().FullName, new SequenceInfo { SequenceNumber = sequenceNumber });

            // save 
            // if no activity then continue with next view else return
        }
    }
}
