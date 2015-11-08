// <copyright file="SimplistPossibleThing.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Feature
{
    using System.Threading;
    using FluentAssertions;
    using ViewTonic.Persistence;
    using ViewTonic.Persistence.Memory;
    using Xbehave;

    public class EventOrdering
    {
        [Scenario]
        public void DispatchingEventsInTheWrongOrder(
            IRepository<string, SomeData> repository,
            View view,
            ISequenceResolver sequenceResolver,
            IViewManager viewManager,
            SomeEvent firstEvent,
            SomeEvent secondEvent)
        {
            "Given a view repository"._(() => repository = new MemoryRepository<string, SomeData>());
            "and a view"._(() => view = new SomeView(repository));
            "and a sequence resolver"._(() => sequenceResolver = new CustomSequenceResolver());
            "and a view manager"._(ctx => viewManager = 
                ViewManager.ForViews(view).OrderEventsUsing(sequenceResolver).StartAtSequenceNumber(0).Create().Using(ctx));
            "and a first event"._(() => firstEvent = new SomeEvent { Sequence = 1, Id = "test" });
            "and a second event"._(() => secondEvent = new SomeEvent { Sequence = 2, Id = "test2" });
            "when those events are dispatched out of order"._(
                () => 
                {
                    viewManager.QueueForDispatch(secondEvent);
                    viewManager.QueueForDispatch(firstEvent);
                });
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "then the view received the second event last"._(() => repository.Get("root").LastEventId.Should().Be(secondEvent.Id));
            "and the view received two events"._(() => repository.Get("root").EventCount.Should().Be(2));
        }

        [Scenario]
        public void DispatchingEventsInTheWrongOrderWithANonZeroStartingSequenceNumber(
            IRepository<string, SomeData> repository,
            View view,
            ISequenceResolver sequenceResolver,
            IViewManager viewManager,
            SomeEvent firstEvent,
            SomeEvent secondEvent)
        {
            "Given a view repository"._(() => repository = new MemoryRepository<string, SomeData>());
            "and a view"._(() => view = new SomeView(repository));
            "and a sequence resolver"._(() => sequenceResolver = new CustomSequenceResolver());
            "and a view manager"._(ctx => viewManager =
                ViewManager.ForViews(view).OrderEventsUsing(sequenceResolver).StartAtSequenceNumber(2).Create().Using(ctx));
            "and a first event"._(() => firstEvent = new SomeEvent { Sequence = 2, Id = "test2" });
            "and a second event"._(() => secondEvent = new SomeEvent { Sequence = 3, Id = "test3" });
            "when those events are dispatched out of order"._(
                () =>
                {
                    viewManager.QueueForDispatch(secondEvent);
                    viewManager.QueueForDispatch(firstEvent);
                });
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "then the view received the second event last"._(() => repository.Get("root").LastEventId.Should().Be(secondEvent.Id));
            "and the view received two events"._(() => repository.Get("root").EventCount.Should().Be(1));
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
    }
}
