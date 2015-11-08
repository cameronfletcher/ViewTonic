// <copyright file="SimplistPossibleThing.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Feature
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using FluentAssertions;
    using ViewTonic.Persistence;
    using ViewTonic.Persistence.Memory;
    using ViewTonic.Runtime;
    using Xbehave;

    public class ViewPersistence
    {
        [Scenario]
        public void InitialViewPersistence(
            IRepository<string, SomeData> repository,
            View view,
            ISequenceResolver sequenceResolver,
            IRepository<string, Snapshot> snapshotRepository,
            IViewManager viewManager,
            SomeEvent firstEvent,
            SomeEvent secondEvent,
            SomeData initialData,
            SomeData subsequentData)
        {
            "Given a view repository"._(() => repository = new MemoryRepository<string, SomeData>());
            "and a view"._(() => view = new SomeView(repository));
            "and a sequence resolver"._(() => sequenceResolver = new CustomSequenceResolver());
            "and a snapshot repository"._(() => snapshotRepository = new MemoryRepository<string, Snapshot>());
            "and a view manager"._(ctx => viewManager =
                ViewManager.ForViews(view)
                    .OrderEventsUsing(sequenceResolver)
                    .SnapshotViewsUsing(snapshotRepository).WithAQuietTimeTimeoutOf(2)
                    .Create().Using(ctx));
            "and a first event"._(() => firstEvent = new SomeEvent { Sequence = 1, Id = "test" });
            "and a second event"._(() => secondEvent = new SomeEvent { Sequence = 2, Id = "test2" });
            "when those events are dispatched"._(
                () =>
                {
                    viewManager.QueueForDispatch(firstEvent);
                    viewManager.QueueForDispatch(secondEvent);
                });
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "and the repository is queried initially"._(() => initialData = repository.Get("root"));
            "and the snapshot is given time to process"._(() => Thread.Sleep(2000));
            "and the repository is queried subsequently"._(() => subsequentData = repository.Get("root"));
            "then the initial query should be empty"._(() => initialData.Should().BeNull());
            "and the view received the second event last"._(() => subsequentData.LastEventId.Should().Be(secondEvent.Id));
            "and the view received two events"._(() => subsequentData.EventCount.Should().Be(2));
        }

        [Scenario]
        public void SubsequentViewPersistence(
            IRepository<string, SomeData> repository,
            View view,
            ISequenceResolver sequenceResolver,
            IRepository<string, Snapshot> snapshotRepository,
            IViewManager viewManager,
            SomeEvent firstEvent,
            SomeEvent secondEvent,
            SomeData initialData,
            SomeData actualInitialData,
            SomeData subsequentData)
        {
            "Given a view repository"._(() => repository = new MemoryRepository<string, SomeData>());
            "and some intial data"._(
                () => initialData = new SomeData
                {
                    LastEventId = "test2",
                    EventCount = 2,
                });
            "and the repository contains the initial data"._(() => repository.AddOrUpdate("root", initialData));
            "and a view"._(() => view = new SomeView(repository));
            "and a sequence resolver"._(() => sequenceResolver = new CustomSequenceResolver());
            "and a snapshot repository"._(() => snapshotRepository = new MemoryRepository<string, Snapshot>());
            "and the snapshot repository contains the initial snapshot data"._(
                () => snapshotRepository.AddOrUpdate(
                    view.GetType().FullName,
                    new Snapshot
                    {
                        ViewName = view.GetType().FullName,
                        PersistedSequenceNumber = 2,
                    }));
            "and a view manager"._(ctx => viewManager =
                ViewManager.ForViews(view)
                    .OrderEventsUsing(sequenceResolver)
                    .SnapshotViewsUsing(snapshotRepository).WithAQuietTimeTimeoutOf(2)
                    .Create().Using(ctx));
            "and a third event"._(() => firstEvent = new SomeEvent { Sequence = 3, Id = "test3" });
            "and a fourth event"._(() => secondEvent = new SomeEvent { Sequence = 4, Id = "test4" });
            "when those events are dispatched"._(
                () =>
                {
                    viewManager.QueueForDispatch(firstEvent);
                    viewManager.QueueForDispatch(secondEvent);
                });
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "and the repository is queried initially"._(() => actualInitialData = repository.Get("root"));
            "and the snapshot is given time to process"._(() => Thread.Sleep(2000));
            "and the repository is queried subsequently"._(() => subsequentData = repository.Get("root"));
            "then the initial query should be empty"._(() => actualInitialData.Should().Be(initialData));
            "and the view received the second event last"._(() => subsequentData.LastEventId.Should().Be(secondEvent.Id));
            "and the view received two events"._(() => subsequentData.EventCount.Should().Be(4));
        }

        public class SomeView : View
        {
            private readonly IRepository<string, SomeData> repository;

            public SomeView(IRepository<string, SomeData> repository)
            {
                this.repository = repository;
            }

            public void Consume(SomeEvent @event)
            {
                var data = this.repository.Get("root") ?? new SomeData();

                this.repository.AddOrUpdate(
                    "root",
                    new SomeData
                    {
                        LastEventId = @event.Id,
                        EventCount = data.EventCount + 1,
                    });
            }
        }

        public class SomeEvent
        {
            public int Sequence { get; set; }

            public string Id { get; set; }
        }

        public class SomeData
        {
            public string LastEventId { get; set; }

            public int EventCount { get; set; }
        }

        private class CustomSequenceResolver : ISequenceResolver
        {
            public long GetSequenceNumber(object eventPayload)
            {
                var @event = (SomeEvent)eventPayload;
                return @event.Sequence;
            }
        }

        private class CustomEventResolver : IEventResolver
        {
            private List<SomeEvent> eventStore;

            public CustomEventResolver(List<SomeEvent> eventStore)
            {
                this.eventStore = eventStore;
            }

            public IEnumerable<object> GetEventsFrom(long sequenceNumber)
            {
                return this.eventStore.Skip((int)sequenceNumber - 1);
            }
        }
    }
}
