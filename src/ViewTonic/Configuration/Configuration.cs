// <copyright file="Configuration.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Configuration
{
    using System;
    using ViewTonic.Persistence;
    using ViewTonic.Sdk;

    public sealed class Configure
    {
        private Configure()
        {
        }

        public interface EventSequencing : Complete
        {
            EventSequencingOptions SequenceEventsUsing(ISequenceResolver sequenceResolver);

            EventSequencingOptions SequenceEventsByProperty(string eventProperty);
        }

        public interface EventSequencingOptions
        {
            EventResolution StartAtSequenceNumber(long sequenceNumber);

            EventResolution SnapshotViewsUsing(IRepository<string, SequenceInfo> sequenceRepository);
        }

        public interface EventResolution : Complete
        {
            EventResolutionPublisherOptions ResolveMissingEventsUsing(Func<long, object> eventResolver);
        }

        public interface EventResolutionPublisherOptions
        {
            EventResolutionConsumerOptions WithAPublisherTimeoutOf(int milliseconds);
        }

        public interface EventResolutionConsumerOptions
        {
            Complete AndAConsumerTimeoutOf(int milliseconds);
        }

        public interface Complete
        {
            IEventDispatcher Create();
        }
    }
}
