// <copyright file="SnapshotManager.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using ViewTonic.Persistence;
using ViewTonic.Persistence.Memory;
using ViewTonic.Runtime;

    public sealed class SnapshotManager : ISnapshotManager, IDisposable
    {
        private readonly Dictionary<View, Snapshot> snapshots;
        private readonly IRepository<string, Snapshot> snapshotRepository;
        private readonly Timer timer;
        private readonly int timeout;
        private readonly object @lock = new object();

        private bool isDisposed;
        private bool isSnapshotting;
        private bool dispatchFlag;

        public SnapshotManager(IEnumerable<View> views)
            : this(views, new MemoryRepository<string, Snapshot>(), Timeout.Infinite)
        {
        }

        public SnapshotManager(IEnumerable<View> views, IRepository<string, Snapshot> snapshotRepository, int quietTimeTimeout)
        {
            Trace.WriteLine("SnapshotManager.ctor(IEnumerable<View> views, IRepository<string, Snapshot> snapshotRepository, int quietTimeTimeout)");

            Guard.Against.NullOrEmptyOrNullElements(() => views);
            Guard.Against.Null(() => snapshotRepository);
            Guard.Against.NegativeTimeout(() => quietTimeTimeout);

            this.snapshotRepository = snapshotRepository;
            this.timeout = quietTimeTimeout;

            this.snapshots = views.ToDictionary(
                view => view,
                view => snapshotRepository.Get(view.GetType().FullName) ?? new Snapshot { ViewName = view.GetType().FullName });

            this.timer = new Timer(state => this.TimerCallback(), null, Timeout.Infinite, Timeout.Infinite);

            if (!Timeout.Infinite.Equals(quietTimeTimeout))
            {
                this.snapshots.Keys.ToList().ForEach(view => view.PrepareRepositoriesForSnapshotting());

                Trace.WriteLine("SnapshotManager.ctor: Timer.Change(" + this.timeout.ToString() + ", Timeout.Infinite)");
                this.timer.Change(this.timeout, Timeout.Infinite);
            }
        }

        private void Save()
        {
            this.snapshots.Values.ToList().ForEach(snapshot => this.snapshotRepository.AddOrUpdate(snapshot.ViewName, snapshot));
        }

        public void Dispose()
        {
            Trace.WriteLine("SnapshotManager.Dispose()");

            if (this.isDisposed)
            {
                return;
            }

            this.isDisposed = true;

            Trace.WriteLine("SnapshotManager.Dispose: Timer.Dispose()");
            this.timer.Change(Timeout.Infinite, Timeout.Infinite);
            this.timer.Dispose();
        }

        public void Dispatch(Event @event)
        {
            Trace.WriteLine("SnapshotManager.Dispatch(Event @event)");

            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            lock (this.@lock)
            {
                this.snapshots
                    .AsParallel()
                    .Select(
                        x =>
                        new
                        {
                            View = x.Key,
                            Snapshot = x.Value
                        })
                    .Where(x => x.Snapshot.ProcessedSequenceNumber < @event.SequenceNumber)
                    .ForAll(
                        x =>
                        {
                            x.View.Apply(@event.Payload);
                            x.Snapshot.ProcessedSequenceNumber = @event.SequenceNumber;
                        });
            }

            if (this.timeout.Equals(Timeout.Infinite))
            {
                return;
            }

            if (this.isSnapshotting)
            {
                this.dispatchFlag = true;
                return;
            }

            Trace.WriteLine("SnapshotManager.Dispatch: Timer.Change(" + this.timeout.ToString() + ", Timeout.Infinite)");
            this.timer.Change(this.timeout, Timeout.Infinite);
        }

        private void TimerCallback()
        {
            Trace.WriteLine("SnapshotManager.TimerCallback()");

            this.dispatchFlag = false;
            this.isSnapshotting = true;

            do
            {
                var view = this.snapshots
                    .Select(
                        x =>
                        new
                        {
                            Value = x.Key,
                            Snapshot = x.Value,
                            Delta = x.Value.ProcessedSequenceNumber - x.Value.PersistedSequenceNumber,
                        })
                    .Where(x => x.Delta != 0)
                    .OrderByDescending(x => x.Delta)
                    .ThenBy(x => x.Snapshot.ViewName)
                    .FirstOrDefault();

                if (view == null)
                {
                    // NOTE (Cameron): All views are persisted and up to date.
                    this.isSnapshotting = false;
                    return;
                }

                long snapshotNumber = 0L;
                lock (this.@lock)
                {
                    view.Value.Snapshot();
                    snapshotNumber = view.Snapshot.ProcessedSequenceNumber;
                }

                view.Snapshot.PersistedSequenceNumber = 0L;
                this.snapshotRepository.AddOrUpdate(view.Snapshot.ViewName, view.Snapshot);

                view.Value.Flush();

                view.Snapshot.PersistedSequenceNumber = snapshotNumber;
                this.snapshotRepository.AddOrUpdate(view.Snapshot.ViewName, view.Snapshot);

                if (/*cancellationToken.IsCancellationRequested ||*/ this.isDisposed)
                {
                    //this.cancellationTokenSource.Dispose();
                    return;
                }
            }
            while (!this.dispatchFlag);

            if (this.dispatchFlag)
            {
                try
                {
                    Trace.WriteLine("SnapshotManager.TimerCallback: Timer.Change(" + this.timeout.ToString() + ", Timeout.Infinite)");
                    this.timer.Change(this.timeout, Timeout.Infinite);
                }
                catch (ObjectDisposedException)
                {
                    // NOTE (Cameron): Dispose has been called during execution so swallow and leave to garbage collector.
                }
            }

            this.isSnapshotting = false;
        }
    }
}
