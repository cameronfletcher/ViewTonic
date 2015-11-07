//// <copyright file="EventResolving.cs" company="ViewTonic contributors">
////  Copyright (c) ViewTonic contributors. All rights reserved.
//// </copyright>

//namespace ViewTonic.Tests.Feature
//{
//    using System.Collections.Generic;
//    using System.Threading;
//    using FluentAssertions;
//    using ViewTonic.Persistence;
//    using ViewTonic.Persistence.Memory;
//    using ViewTonic.Sdk;
//    using Xbehave;

//    // NOTE (Cameron): Event replaying *only* ever occurs on startup.
//    public class EventResolving
//    {
//        [Scenario]
//        public void CanEventReplayOnCleanStartupWithNoEvents(
//            Dictionary<long, SomeEvent> eventStore,
//            IRepository<long, SomeEvent> viewRepository,
//            View view,
//            IViewManager viewManager)
//        {
//            var currentSequenceNumber = 0L;

//            "Given an empty event store"._(() => eventStore = new Dictionary<long, SomeEvent>());

//            "and a view repository"._(() => viewRepository = new MemoryRepository<long, SomeEvent>());

//            "and a view"._(() => view = new SomeView(viewRepository));

//            "When an event dispatcher is created"._(
//                () => viewManager =
//                    ViewManager.ForViews(view)
//                    //.SequenceEventsByProperty("SequenceNumber")
//                        .OrderEventsUsing(@event => ++currentSequenceNumber)
//                            .StartAtSequenceNumber(0)
//                        .ResolveMissingEventsUsing(
//                            sequenceNumber =>
//                            {
//                                SomeEvent @event;
//                                return eventStore.TryGetValue(sequenceNumber, out @event) ? @event : null;
//                            })
//                            .WithATimeoutOf(Timeout.Infinite)
//                        .Create());

//            "Then"._(() =>
//            {
//                // ???
//            });
//        }

//        [Scenario]
//        public void CanEventReplayOnCleanStartup(
//            Dictionary<long, SomeEvent> eventStore,
//            IRepository<long, SomeEvent> viewRepository,
//            View view,
//            IViewManager viewManager)
//        {
//            var currentSequenceNumber = 0L;

//            "Given an event store"._(
//                () => eventStore = 
//                    new Dictionary<long, SomeEvent>
//                    {
//                        { 1, new SomeEvent { SequenceNumber = 1, Value = "event1" } },
//                        { 2, new SomeEvent { SequenceNumber = 2, Value = "event2" } },
//                    });

//            "and a view repository"._(() => viewRepository = new MemoryRepository<long, SomeEvent>());

//            "and a view"._(() => view = new SomeView(viewRepository));

//            "When an event dispatcher is created"._(
//                () => viewManager = 
//                    ViewManager.ForViews(view)
//                        //.SequenceEventsByProperty("SequenceNumber")
//                        .OrderEventsUsing(@event => ++currentSequenceNumber)
//                            .StartAtSequenceNumber(0)
//                        .ResolveMissingEventsUsing(
//                            sequenceNumber => 
//                            {
//                                SomeEvent @event;
//                                return eventStore.TryGetValue(sequenceNumber, out @event) ? @event : null;
//                            })
//                            .WithATimeoutOf(Timeout.Infinite)
//                        .Create());

//            "Then"._(() => 
//                {
//                    var event1 = viewRepository.Get(1);
//                    event1.Should().NotBeNull();
//                    event1.Should().BeOfType<SomeEvent>();
//                    event1.As<SomeEvent>().SequenceNumber.Should().Be(1);
//                    event1.As<SomeEvent>().Value.Should().Be("event1");

//                    var event2 = viewRepository.Get(2);
//                    event2.Should().NotBeNull();
//                    event2.Should().BeOfType<SomeEvent>();
//                    event2.As<SomeEvent>().SequenceNumber.Should().Be(2);
//                    event2.As<SomeEvent>().Value.Should().Be("event2");
//                });
//        }

//        public void CanEventReplayOnDirtyStartup()
//        {
//        }

//        public void CanEventReplayOnStartupWithBufferedMessages()
//        {
//        }

//        public void CanEventReplayOnStartupWithDuplicateBufferedMessages()
//        {
//        }

//        public void CanEventReplayOnStartupWithMissingBufferedMessages()
//        {
//        }

//        public void CanEventReplayWhilstRunning()
//        {
//            // to include event replay status with notification
//        }

//        // basic

//        // sequenced

//        // sequenced with resolver

//        // (managed) sequenced

//        // (managed) sequenced with resolver

//        public class SomeView : View
//        {
//            private readonly IRepository<long, SomeEvent> repository;

//            public SomeView(IRepository<long, SomeEvent> repository)
//            {
//                this.repository = repository;
//            }

//            public void Consume(SomeEvent @event)
//            {
//                this.repository.AddOrUpdate(@event.SequenceNumber, @event);
//            }
//        }

//        public class SomeEvent
//        {
//            public long SequenceNumber { get; set; }

//            public string Value { get; set; }
//        }
//    }
//}
