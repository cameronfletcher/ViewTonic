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
        private IEventBuffer eventBuffer;

        private Type eventDispatcherType;
        private Type bufferType;

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
            this.eventBuffer = new EventBuffer(sequenceNumber);
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

        // TODO (Cameron): Consider how to handle disposable dependencies.
        IViewManager Configuration.Complete.Create()
        {
            if (this.sequenceResolver == null)
            {
                return new DefaultViewManager(new EventManager(), new SnapshotManager(views), true);
            }

            //var defaultEventDispatcher = new DefaultEventDispatcher(this.views);

            //if (typeof(DefaultEventDispatcher).Equals(this.eventDispatcherType))
            //{
            //    return defaultEventDispatcher;
            //}

            //Func<long, IOrderedBuffer> bufferFactory = number => typeof(OrderedBuffer).Equals(this.bufferType)
            //    ? new OrderedBuffer(number)
            //    : new IntelligentOrderedBuffer(number, this.eventResolver, this.eventResolverTimeout);

            //if (this.eventDispatcherType.Equals(typeof(OrderedEventDispatcher)))
            //{
            //    return new OrderedEventDispatcher(defaultEventDispatcher, sequenceResolver, bufferFactory(this.sequenceNumber));
            //}

            //return new ManagedEventDispatcher(this.views, this.sequenceResolver, this.sequenceRepository, bufferFactory, this.snapshotQuietTimeTimeout);

            var eventManager = new EventManager(
                this.sequenceResolver, 
                this.eventResolver, 
                this.eventResolverTimeout,
                this.eventBuffer ?? new EventBuffer(),
                true);

            var snapshotManager = new SnapshotManager(
                this.views,
                this.snapshotRepository,
                this.quietTimeTimeout);

            return new DefaultViewManager(eventManager, snapshotManager);
        }
    }
}
