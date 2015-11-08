// <copyright file="Configuration.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System;
    using System.Collections.Generic;
    using ViewTonic.Persistence;
    using ViewTonic.Runtime;

    public static class Configuration
    {
        public interface EventOrdering : Complete
        {
            EventOrderingOptions OrderEventsByProperty(string eventPropertyName);

            EventOrderingOptions OrderEventsUsing(Func<object, long> sequenceResolver);

            EventOrderingOptions OrderEventsUsing(ISequenceResolver sequenceResolver);
        }

        public interface EventOrderingOptions : EventResolvingWithMandatoryViewSnapshotting, ViewSnapshotting
        {
            EventResolvingWithOptionalViewSnapshotting StartAtSequenceNumber(long sequenceNumber);
        }

        public interface EventResolvingWithMandatoryViewSnapshotting
        {
            EventResolvingWithMandatoryViewSnapshottingOptions ResolveMissingEventsUsing(Func<long, IEnumerable<object>> eventResolver);

            EventResolvingWithMandatoryViewSnapshottingOptions ResolveMissingEventsUsing(IEventResolver eventResolver);
        }

        public interface EventResolvingWithMandatoryViewSnapshottingOptions
        {
            ViewSnapshotting WithATimeoutOf(int seconds);
        }

        public interface EventResolvingWithOptionalViewSnapshotting : Complete
        {
            EventResolvingWithOptionalViewSnapshottingOptions ResolveMissingEventsUsing(Func<long, IEnumerable<object>> eventResolver);

            EventResolvingWithOptionalViewSnapshottingOptions ResolveMissingEventsUsing(IEventResolver eventResolver);
        }

        public interface EventResolvingWithOptionalViewSnapshottingOptions
        {
            OptionalViewSnapshotting WithATimeoutOf(int seconds);
        }

        public interface ViewSnapshotting
        {
            ViewSnapshottingOptions SnapshotViewsUsing(IRepository<string, Snapshot> snapshotRepository);
        }

        public interface ViewSnapshottingOptions
        {
            Complete WithAQuietTimeTimeoutOf(int seconds);
        }

        public interface Complete
        {
            IViewManager Create();
        }

        public interface OptionalViewSnapshotting : ViewSnapshotting, Complete
        {
        }
    }

}
