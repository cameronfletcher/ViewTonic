// <copyright file="EventManagerTests.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using FluentAssertions;
    using ViewTonic.Sdk;
    using ViewTonic.Tests.Sdk;
    using Xunit;
    using Xunit.Abstractions;

    public class EventManagerTests : IDisposable
    {
        private readonly TestOutputListener listner;

        public EventManagerTests(ITestOutputHelper testOutputHelper)
        {
            this.listner = new TestOutputListener(testOutputHelper);
            Trace.Listeners.Add(this.listner);
        }

        [Fact]
        public void CanReplayNoEventsFromZero()
        {
            // arrange
            var eventStore = new EventStore();
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, Timeout.Infinite))
            using (var cts = new CancellationTokenSource(200))
            {
                // act
                Action action = () => manager.Take(cts.Token);

                // assert
                action.ShouldThrow<OperationCanceledException>();
            }
        }

        [Fact]
        public void CanReplayEventsFromZero()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(10);
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, Timeout.Infinite))
            using (var cts = new CancellationTokenSource())
            {
                var events = new List<Event>() { null /* compensate for non zero-based */ };

                // act
                do
                {
                    var @event = manager.Take(cts.Token);
                    events.Add(@event);
                }

                // assert
                while (@events.Count <= 10);
                @events.ShouldContainEvents(1, 10);
            }
        }

        [Fact]
        public void CanReplayNoEventsFromNonZero()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(10);
            using (var buffer = new EventBuffer(10))
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, Timeout.Infinite, buffer))
            using (var cts = new CancellationTokenSource(200))
            {
                // act
                Action action = () => manager.Take(cts.Token);

                // assert
                action.ShouldThrow<OperationCanceledException>();
            }
        }

        [Fact]
        public void CanAvoidReplayWithNoEventResolverSpecified()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(10);
            using (var manager = new EventManager(eventStore.SequenceResolver))
            using (var cts = new CancellationTokenSource(200))
            {
                // act
                Action action = () => manager.Take(cts.Token);

                // assert
                action.ShouldThrow<OperationCanceledException>();
            }
        }

        [Fact]
        public void CanReplayEventsFromNonZero()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(10);
            using (var buffer = new EventBuffer(5))
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, Timeout.Infinite, buffer))
            using (var cts = new CancellationTokenSource())
            {
                var events = new List<Event>().AlreadyHaving(5);

                // act
                do
                {
                    var @event = manager.Take(cts.Token);
                    events.Add(@event);
                }

                // assert
                while (@events.Count <= 10);
                @events.ShouldContainEvents(6, 10);
            }
        }

        [Fact]
        public void CanForceReplayEvents()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(10);
            using (var buffer = new EventBuffer(10))
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, Timeout.Infinite, buffer))
            using (var cts = new CancellationTokenSource())
            {
                var events = new List<Event>() { null /* compensate for non zero-based */ };

                // act
                manager.ReplayEvents();
                do
                {
                    var @event = manager.Take(cts.Token);
                    events.Add(@event);
                }

                // assert
                while (@events.Count <= 10);
                @events.ShouldContainEvents(1, 10);
            }
        }

        [Fact]
        public void CanResolveSingleMissingEvent()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(2);
            using (var buffer = new EventBuffer(2))
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, 20, buffer))
            using (var cts = new CancellationTokenSource())
            {
                var events = new List<Event>().AlreadyHaving(2);
                eventStore.WaitForCall();

                // act
                var task = new Task(
                    () =>
                    {
                        var @event = manager.Take(cts.Token);
                        events.Add(@event);
                    });

                task.Start();
                eventStore.HasAdditionalEventCount(1);
                Task.WaitAll(task);

                // assert
                @events.ShouldContainEvents(3, 3);
            }
        }

        [Fact]
        public void CanResolveMultipleMissingEvents()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(2);
            using (var buffer = new EventBuffer(2))
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, 20, buffer))
            using (var cts = new CancellationTokenSource())
            {
                var events = new List<Event>().AlreadyHaving(2);
                eventStore.WaitForCall();

                // act
                var task = new Task(
                    () =>
                    {
                        do
                        {
                            var @event = manager.Take(cts.Token);
                            events.Add(@event);
                        }
                        while (@events.Count <= 5);
                    });

                task.Start();
                eventStore.HasAdditionalEventCount(3);
                Task.WaitAll(task);

                // assert
                @events.ShouldContainEvents(3, 5);
            }
        }

        [Fact]
        public void CanResolveMultipleNonContiguousMissingEvents()
        {
            // arrange
            var eventStore = new EventStore().WithEventCount(2);
            using (var buffer = new EventBuffer(2))
            using (var manager = new EventManager(eventStore.SequenceResolver, eventStore.EventResolver, 20, buffer))
            using (var cts = new CancellationTokenSource())
            {
                var events = new List<Event>().AlreadyHaving(2);
                eventStore.WaitForCall();

                // act
                manager.Add(EventStore.Event.Number(5));

                var task = new Task(
                    () =>
                    {
                        do
                        {
                            var @event = manager.Take(cts.Token);
                            events.Add(@event);
                        }
                        while (@events.Count <= 10);
                    });

                task.Start();
                eventStore.HasAdditionalEventCount(8);
                Task.WaitAll(task);

                // assert
                @events.ShouldContainEvents(3, 10);
            }
        }

        // TODO:
        // event replay followed by missing event
        // test of everything together
        // disposal during event replay
        // event replay during event replay

        public void Dispose()
        {
            Trace.Listeners.Remove(this.listner);
            Trace.Close();
        }
    }
}