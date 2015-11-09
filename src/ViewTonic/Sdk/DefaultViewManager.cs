// <copyright file="DefaultViewManager.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class DefaultViewManager : IViewManager, IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly IEventManager eventManager;
        private readonly ISnapshotManager snapshotManager;

        private bool isDisposed;
        private bool disposeDependencies;

        public DefaultViewManager(IEventManager eventManager, ISnapshotManager snapshotManager)
            : this(eventManager, snapshotManager, false)
        {
        }

        internal DefaultViewManager(IEventManager eventManager, ISnapshotManager snapshotManager, bool disposeDependencies)
        {
            Guard.Against.Null(() => eventManager);
            Guard.Against.Null(() => snapshotManager);

            this.eventManager = eventManager;
            this.snapshotManager = snapshotManager;

            var task = new Task(() => this.Run(this.cancellationTokenSource.Token));
            task.ContinueWith(t => this.cancellationTokenSource.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            task.Start();
        }

        public void QueueForDispatch(object @event)
        {
            this.eventManager.Add(@event);
        }

        public void TriggerEventReplaying()
        {
            this.eventManager.ReplayEvents();
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            this.cancellationTokenSource.Cancel();

            if (this.disposeDependencies)
            {
                var disposable = this.eventManager as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }

                disposable = this.snapshotManager as IDisposable;
                if (disposable != null)
                {
                    disposable.Dispose();
                }
            }
        }

        private void Run(CancellationToken token)
        {
            while (!this.isDisposed)
            {
                Event @event = null;
                try
                {
                    @event = this.eventManager.Take(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // NOTE (Cameron): The items here are coming out of the buffer in the correct order.
                this.snapshotManager.Dispatch(@event);
            }
        }
    }
}
