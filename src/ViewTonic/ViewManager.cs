// <copyright file="ViewManager.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using ViewTonic.Persistence;
    using ViewTonic.Runtime;
    using ViewTonic.Sdk;

    public sealed class ViewManager : 
        Configuration.EventOrdering,
        Configuration.EventOrderingOptions,
        Configuration.EventResolvingWithMandatoryViewSnapshotting,
        Configuration.EventResolvingWithMandatoryViewSnapshottingOptions,
        Configuration.EventResolvingWithOptionalViewSnapshotting,
        Configuration.EventResolvingWithOptionalViewSnapshottingOptions,
        Configuration.ViewSnapshotting,
        Configuration.ViewSnapshottingOptions,
        Configuration.OptionalViewSnapshotting,
        Configuration.Complete
    {
        private View[] views;
        private ISequenceResolver sequenceResolver;
        private IEventResolver eventResolver;
        private IRepository<string, Snapshot> snapshotRepository;
        private int eventResolverTimeout;
        private int quietTimeTimeout;
        private long sequenceNumber;

        private ViewManager()
        {
        }

        public static Configuration.EventOrdering ForViews(params View[] views)
        {
            Guard.Against.NullOrEmptyOrNullElements(() => views);

            return new ViewManager { views = views };
        }

        private ViewManager OrderEventsUsing(ISequenceResolver sequenceResolver)
        {
            Guard.Against.Null(() => sequenceResolver);

            this.sequenceResolver = sequenceResolver;
            return this;
        }

        private ViewManager StartAtSequenceNumber(long sequenceNumber)
        {
            Guard.Against.Negative(() => sequenceNumber);

            this.sequenceNumber = sequenceNumber;
            return this;
        }

        private ViewManager ResolveMissingEventsUsing(IEventResolver eventResolver)
        {
            Guard.Against.Null(() => eventResolver);

            this.eventResolver = eventResolver;
            return this;
        }

        private ViewManager WithATimeoutOf(int seconds)
        {
            Guard.Against.NegativeTimeout(() => seconds);

            this.eventResolverTimeout = Timeout.Infinite.Equals(seconds) ? seconds : 1000 * seconds;
            return this;
        }

        private ViewManager SnapshotViewsUsing(IRepository<string, Snapshot> snapshotRepository)
        {
            Guard.Against.Null(() => snapshotRepository);

            this.snapshotRepository = snapshotRepository;
            return this;
        }

        private ViewManager WithAQuietTimeTimeoutOf(int seconds)
        {
            Guard.Against.NegativeTimeout(() => seconds);

            this.quietTimeTimeout = Timeout.Infinite.Equals(seconds) ? seconds : 1000 * seconds;
            return this;
        }

        Configuration.EventOrderingOptions Configuration.EventOrdering.OrderEventsByProperty(string eventPropertyName)
        {
            return this.OrderEventsUsing(new DefaultSequenceResolver(eventPropertyName));
        }

        Configuration.EventOrderingOptions Configuration.EventOrdering.OrderEventsUsing(Func<object, long> sequenceResolver)
        {
            return this.OrderEventsUsing(new DefaultSequenceResolver(sequenceResolver));
        }

        Configuration.EventOrderingOptions Configuration.EventOrdering.OrderEventsUsing(ISequenceResolver sequenceResolver)
        {
            return this.OrderEventsUsing(sequenceResolver);
        }

        Configuration.EventResolvingWithOptionalViewSnapshotting Configuration.EventOrderingOptions.StartAtSequenceNumber(long sequenceNumber)
        {
            return this.StartAtSequenceNumber(sequenceNumber);
        }

        Configuration.EventResolvingWithMandatoryViewSnapshottingOptions Configuration.EventResolvingWithMandatoryViewSnapshotting.ResolveMissingEventsUsing(Func<long, IEnumerable<object>> eventResolver)
        {
            return this.ResolveMissingEventsUsing(new DefaultEventResolver(eventResolver));
        }

        Configuration.EventResolvingWithMandatoryViewSnapshottingOptions Configuration.EventResolvingWithMandatoryViewSnapshotting.ResolveMissingEventsUsing(IEventResolver eventResolver)
        {
            return this.ResolveMissingEventsUsing(eventResolver);
        }

        Configuration.EventResolvingWithOptionalViewSnapshottingOptions Configuration.EventResolvingWithOptionalViewSnapshotting.ResolveMissingEventsUsing(Func<long, IEnumerable<object>> eventResolver)
        {
            return this.ResolveMissingEventsUsing(new DefaultEventResolver(eventResolver));
        }

        Configuration.EventResolvingWithOptionalViewSnapshottingOptions Configuration.EventResolvingWithOptionalViewSnapshotting.ResolveMissingEventsUsing(IEventResolver eventResolver)
        {
            return this.ResolveMissingEventsUsing(eventResolver);
        }

        Configuration.ViewSnapshotting Configuration.EventResolvingWithMandatoryViewSnapshottingOptions.WithATimeoutOf(int seconds)
        {
            return this.WithATimeoutOf(seconds);
        }

        Configuration.OptionalViewSnapshotting Configuration.EventResolvingWithOptionalViewSnapshottingOptions.WithATimeoutOf(int seconds)
        {
            return this.WithATimeoutOf(seconds);
        }

        Configuration.ViewSnapshottingOptions Configuration.ViewSnapshotting.SnapshotViewsUsing(IRepository<string, Snapshot> snapshotRepository)
        {
            return this.SnapshotViewsUsing(snapshotRepository);
        }

        Configuration.Complete Configuration.ViewSnapshottingOptions.WithAQuietTimeTimeoutOf(int seconds)
        {
            return this.WithAQuietTimeTimeoutOf(seconds);
        }

        IViewManager Configuration.Complete.Create()
        {
            if (this.sequenceResolver == null)
            {
                return new DefaultViewManager(new EventManager(), new SnapshotManager(this.views), true);
            }

            var snapshotManager = this.snapshotRepository == null
                ? new SnapshotManager(this.views)
                : new SnapshotManager(this.views, this.snapshotRepository, this.quietTimeTimeout);

            // HACK (Cameron): Massive, massive hack.
            var eventBuffer = this.snapshotRepository == null
                ? new EventBuffer(this.sequenceNumber)
                : new EventBuffer(snapshotManager.LowestPersistedSequenceNumber);

            var eventManager = this.eventResolver == null
                ? new EventManager(this.sequenceResolver, new EventManager.NullEventResolver(), Timeout.Infinite, eventBuffer, true)
                : new EventManager(this.sequenceResolver, this.eventResolver, this.eventResolverTimeout, eventBuffer, true);

            return new DefaultViewManager(eventManager, snapshotManager);
        }
    }
}
