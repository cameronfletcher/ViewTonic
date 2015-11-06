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
            EventSequencingOptions SequenceEventsUsing(Func<object, long> sequenceResolver);

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
            EventResolutionOptions ResolveMissingEventsUsing(Func<long, object> eventResolver);

            EventResolutionOptions ResolveMissingEventsUsing(IEventResolver eventResolver);
        }

        public interface EventResolutionOptions
        {
            Complete WithATimeoutOf(int milliseconds);
        }

        public interface Complete
        {
            IEventDispatcher Create();
        }
    }
}
