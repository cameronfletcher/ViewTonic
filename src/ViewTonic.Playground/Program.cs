namespace Playground
{
    using System;
    using System.Collections.Generic;
    using ViewTonic;
    using ViewTonic.Persistence.Memory;
    using ViewTonic.Sdk;
    using System.Linq;
    using ViewTonic.Persistence.Hybrid;

    internal class Program
    {
        public static void Main(string[] args)
        {
            var repository1 = new MemoryRepository<long, Data.Thing>();
            var repository1h = new HybridRepository<long, Data.Thing>(repository1);
            var repository2 = new MemoryRepository<long, Data.Thing>();
            var repository3 = new MemoryRepository<string, Data.Things>();

            var view1 = new Views.Thing(repository1h);
            var view2 = new Views.Things(repository2, repository3);

            var events = new List<object>
            {
                { new Events.ThingCreated { Checkpoint = 1, Id = 1, Name = "A" } },
                { new Events.ThingCreated { Checkpoint = 2, Id = 2, Name = "B" } },
                { new Events.ThingCreated { Checkpoint = 3, Id = 3, Name = "C" } },
                { new Events.ThingUpdated { Checkpoint = 5, Id = 1, Name = "A_changed" } },
                { new Events.ThingUpdated { Checkpoint = 4, Id = 1, Name = "A_changed_mistake" } },
                { new Events.ThingDeleted { Checkpoint = 6, Id = 2 } },
            };

            var sequenceResolver = new CustomSequenceResolver();
            var snapshotRepository = new MemoryRepository<string, SequenceInfo>();

            #region WireUp

            //var x = EventDispatcher
            //    .ForViews(view1, view2)
            //    .Create();

            //var y = EventDispatcher
            //    .ForViews(view1, view2)
            //    .SequenceEventsUsing(sequenceResolver)
            //        .StartAtSequenceNumber(0)
            //    .Create();

            //var z = EventDispatcher
            //    .ForViews(view1, view2)
            //    .SequenceEventsUsing(sequenceResolver)
            //        .StartAtSequenceNumber(1)
            //    .ResolveMissingEventsUsing(null)
            //        .WithAPublisherTimeoutOf(500)
            //        .AndAConsumerTimeoutOf(500)
            //    .Create();

            //var a = EventDispatcher
            //    .ForViews(view1, view2)
            //    .SequenceEventsUsing(sequenceResolver)
            //    .SnapshotViewsUsing(snapshotRepository)
            //    .ResolveMissingEventsUsing(n => events.SingleOrDefault(e => sequenceResolver.GetSequenceNumber(e) == n))
            //        .WithAPublisherTimeoutOf(500)
            //        .AndAConsumerTimeoutOf(500)
            //    .Create();

            var b = EventDispatcher
                .ForViews(view1, view2)
                .SequenceEventsUsing(sequenceResolver)
                .SnapshotViewsUsing(snapshotRepository)
                .Create();

            var eventDispatcher = b; // defaultEventDispatcher;

            #endregion

            foreach (var @event in events.Where(e => !new long[] { 2, 6 }.Contains(sequenceResolver.GetSequenceNumber(e))))
            {
                eventDispatcher.Dispatch(@event);
            }

            Console.ReadLine();

            //orderedEventDispatcher.Dispose();
        }

        private class CustomSequenceResolver : ISequenceResolver
        {
            public long GetSequenceNumber(object message)
            {
                var thingCreated = message as Events.ThingCreated;
                if (thingCreated != null)
                {
                    return thingCreated.Checkpoint;
                }

                var ThingUpdated = message as Events.ThingUpdated;
                if (ThingUpdated != null)
                {
                    return ThingUpdated.Checkpoint;
                }

                var thingDeleted = message as Events.ThingDeleted;
                if (thingDeleted != null)
                {
                    return thingDeleted.Checkpoint;
                }

                throw new NotSupportedException();
            }
        }
    }
}
