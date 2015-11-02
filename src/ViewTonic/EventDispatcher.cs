// <copyright file="EventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System;
    using ViewTonic.Configuration;
    using ViewTonic.Persistence;
    using ViewTonic.Sdk;

    public sealed class EventDispatcher : 
        Configure.EventSequencing,
        Configure.EventSequencingOptions, 
        Configure.EventResolution,
        Configure.EventResolutionPublisherOptions,
        Configure.EventResolutionConsumerOptions, 
        Configure.Complete
    {
        private View[] views;
        private ISequenceResolver sequenceResolver;
        private long sequenceNumber;
        private IRepository<string, SequenceInfo> sequenceRepository;
        private Func<long, object> eventResolver;
        private int publisherTimeout;
        private int consumerTimeout;

        private Type eventDispatcherType;
        private Type bufferType;

        private EventDispatcher()
        {
        }

        public static Configure.EventSequencing ForViews(params View[] views)
        {
            Guard.Against.NullOrEmptyOrNullElements(() => views);

            return new EventDispatcher
            {
                views = views,
                eventDispatcherType = typeof(DefaultEventDispatcher),
            };
        }

        public Configure.EventSequencingOptions SequenceEventsUsing(ISequenceResolver sequenceResolver)
        {
            Guard.Against.Null(() => sequenceResolver);

            this.bufferType = typeof(OrderedBuffer);
            this.eventDispatcherType = typeof(OrderedEventDispatcher);
            this.sequenceResolver = sequenceResolver;
            return this;
        }

        public Configure.EventSequencingOptions SequenceEventsByProperty(string eventProperty)
        {
            throw new NotImplementedException();
        }

        public Configure.EventResolution StartAtSequenceNumber(long sequenceNumber)
        {
            Guard.Against.Negative(() => sequenceNumber);

            this.sequenceNumber = sequenceNumber;
            return this;
        }

        public Configure.EventResolution SnapshotViewsUsing(IRepository<string, SequenceInfo> sequenceRepository)
        {
            Guard.Against.Null(() => sequenceRepository);

            // NOTE (Cameron): This is the most involved of the configuration options.
            this.eventDispatcherType = typeof(ManagedEventDispatcher);
            this.sequenceRepository = sequenceRepository;
            return this;
        }

        public Configure.EventResolutionPublisherOptions ResolveMissingEventsUsing(Func<long, object> eventResolver)
        {
            Guard.Against.Null(() => eventResolver);

            this.bufferType = typeof(IntelligentOrderedBuffer);
            this.eventResolver = eventResolver;
            return this;
        }

        public Configure.EventResolutionConsumerOptions WithAPublisherTimeoutOf(int milliseconds)
        {
            this.publisherTimeout = milliseconds;
            return this;
        }

        public Configure.Complete AndAConsumerTimeoutOf(int milliseconds)
        {
            this.consumerTimeout = milliseconds;
            return this;
        }

        // TODO (Cameron): Consider how to handle disposable dependencies.
        public IEventDispatcher Create()
        {
            var defaultEventDispatcher = new DefaultEventDispatcher(this.views);
            
            if (typeof(DefaultEventDispatcher).Equals(this.eventDispatcherType))
            {
                return defaultEventDispatcher;
            }

            Func<long, IOrderedBuffer> bufferFactory = number => typeof(OrderedBuffer).Equals(this.bufferType)
                ? new OrderedBuffer(number)
                : new IntelligentOrderedBuffer(number, this.eventResolver, this.publisherTimeout, this.consumerTimeout);

            if (this.eventDispatcherType.Equals(typeof(OrderedEventDispatcher)))
            {
                return new OrderedEventDispatcher(defaultEventDispatcher, sequenceResolver, bufferFactory(this.sequenceNumber));
            }

            return new ManagedEventDispatcher(this.views, this.sequenceResolver, this.sequenceRepository, bufferFactory);
        }
    }
}
