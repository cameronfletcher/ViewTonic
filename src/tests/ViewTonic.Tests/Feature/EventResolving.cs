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
    using Xbehave;

    public class EventResolving
    {
        [Scenario]
        public void EventReplayingOnStartUp(
            List<SomeEvent> eventStore,
            IRepository<string, SomeData> repository,
            View view,
            ISequenceResolver sequenceResolver,
            IEventResolver eventResolver,
            IViewManager viewManager)
        {
            "Given an event store with three events"._(() => eventStore = 
                new List<SomeEvent>
                {
                    new SomeEvent { Sequence = 1, Id = "test" },
                    new SomeEvent { Sequence = 2, Id = "test2" },
                    new SomeEvent { Sequence = 3, Id = "test3" },
                });
            "and a view repository"._(() => repository = new MemoryRepository<string, SomeData>());
            "and a view"._(() => view = new SomeView(repository));
            "and a sequence resolver"._(() => sequenceResolver = new CustomSequenceResolver());
            "and an event resolver"._(() => eventResolver = new CustomEventResolver(eventStore));
            "when a view manager is created"._(ctx => viewManager =
                ViewManager.ForViews(view)
                    .OrderEventsUsing(sequenceResolver).StartAtSequenceNumber(0)
                    .ResolveMissingEventsUsing(eventResolver).WithATimeoutOf(Timeout.Infinite)
                    .Create().Using(ctx));
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "then the view received the third event last"._(() => repository.Get("root").LastEventId.Should().Be(eventStore[2].Id));
            "and the view received three events"._(() => repository.Get("root").EventCount.Should().Be(3));
        }

        [Scenario]
        public void EventReplayingFromNonZeroOnStartUp(
            List<SomeEvent> eventStore,
            IRepository<string, SomeData> repository,
            View view,
            ISequenceResolver sequenceResolver,
            IEventResolver eventResolver,
            IViewManager viewManager)
        {
            "Given an event store with three events"._(() => eventStore =
                new List<SomeEvent>
                {
                    new SomeEvent { Sequence = 1, Id = "test" },
                    new SomeEvent { Sequence = 2, Id = "test2" },
                    new SomeEvent { Sequence = 3, Id = "test3" },
                    new SomeEvent { Sequence = 4, Id = "test4" },
                });
            "and a view repository"._(() => repository = new MemoryRepository<string, SomeData>());
            "and a view"._(() => view = new SomeView(repository));
            "and a sequence resolver"._(() => sequenceResolver = new CustomSequenceResolver());
            "and an event resolver"._(() => eventResolver = new CustomEventResolver(eventStore));
            "when a view manager is created"._(ctx => viewManager =
                ViewManager.ForViews(view)
                    .OrderEventsUsing(sequenceResolver).StartAtSequenceNumber(2)
                    .ResolveMissingEventsUsing(eventResolver).WithATimeoutOf(Timeout.Infinite)
                    .Create().Using(ctx));
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "then the view received the third event last"._(() => repository.Get("root").LastEventId.Should().Be(eventStore[3].Id));
            "and the view received three events"._(() => repository.Get("root").EventCount.Should().Be(2));
        }

        [Scenario]
        public void EventReplayingFromNonZeroOnStartUpWithDuplicateBufferedMessages(
            List<SomeEvent> eventStore,
            IRepository<string, SomeData> repository,
            View view,
            ISequenceResolver sequenceResolver,
            IEventResolver eventResolver,
            IViewManager viewManager)
        {
            "Given an event store with three events"._(() => eventStore =
                new List<SomeEvent>
                {
                    new SomeEvent { Sequence = 1, Id = "test" },
                    new SomeEvent { Sequence = 2, Id = "test2" },
                    new SomeEvent { Sequence = 3, Id = "test3" },
                    new SomeEvent { Sequence = 4, Id = "test4" },
                });
            "and a view repository"._(() => repository = new MemoryRepository<string, SomeData>());
            "and a view"._(() => view = new SomeView(repository));
            "and a sequence resolver"._(() => sequenceResolver = new CustomSequenceResolver());
            "and an event resolver"._(() => eventResolver = new CustomEventResolver(eventStore));
            "when a view manager is created"._(ctx => viewManager =
                ViewManager.ForViews(view)
                    .OrderEventsUsing(sequenceResolver).StartAtSequenceNumber(2)
                    .ResolveMissingEventsUsing(eventResolver).WithATimeoutOf(Timeout.Infinite)
                    .Create().Using(ctx));
            "and an old message has been buffered"._(() => viewManager.QueueForDispatch(eventStore[2]));
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "then the view received the third event last"._(() => repository.Get("root").LastEventId.Should().Be(eventStore[3].Id));
            "and the view received three events"._(() => repository.Get("root").EventCount.Should().Be(2));
        }

        public class SomeView : View
        {
            private readonly IRepository<string, SomeData> repository;

            private int eventCount;

            public SomeView(IRepository<string, SomeData> repository)
            {
                this.repository = repository;
            }

            public void Consume(SomeEvent @event)
            {
                this.eventCount++;

                this.repository.AddOrUpdate(
                    "root",
                    new SomeData
                    {
                        LastEventId = @event.Id,
                        EventCount = eventCount,
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
