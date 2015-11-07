// <copyright file="SnapshotManagerTests.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Unit
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading;
    using FluentAssertions;
    using ViewTonic.Persistence;
    using ViewTonic.Persistence.Memory;
    using ViewTonic.Runtime;
    using ViewTonic.Sdk;
    using ViewTonic.Tests.Sdk;
    using Xunit;
    using Xunit.Abstractions;

    public class SnapshotManagerTests : IDisposable
    {
        private readonly TestOutputListener listner;

        public SnapshotManagerTests(ITestOutputHelper testOutputHelper)
        {
            this.listner = new TestOutputListener(testOutputHelper);
            Trace.Listeners.Add(this.listner);
        }

        [Fact]
        public void CanDispatchToSingleView()
        {
            var repository = new MemoryRepository<string, SomeEvent>();
            var views = new View[] { new SomeView(repository) };
            var snapshotRepository = new MemoryRepository<string, Snapshot>();
            var @event = new SomeEvent { Id = "test" };
            using (var snapshotManager = new SnapshotManager(views, snapshotRepository, Timeout.Infinite))
            {
                // act
                snapshotManager.Dispatch(new Event { SequenceNumber = 1, Payload = @event });

                // assert
                repository.Get(@event.Id).Should().Be(@event);
            }
        }

        [Fact]
        public void CanDispatchToMultipleViews()
        {
            var firstRepository = new MemoryRepository<string, SomeEvent>();
            var secondRepository = new MemoryRepository<string, SomeEvent>();
            var views = new View[] { new SomeView(firstRepository), new SomeView(secondRepository) };
            var snapshotRepository = new MemoryRepository<string, Snapshot>();
            var @event = new SomeEvent { Id = "test" };
            using (var snapshotManager = new SnapshotManager(views, snapshotRepository, Timeout.Infinite))
            {
                // act
                snapshotManager.Dispatch(new Event { SequenceNumber = 1, Payload = @event });

                // assert
                firstRepository.Get(@event.Id).Should().Be(@event);
                secondRepository.Get(@event.Id).Should().Be(@event);
            }
        }

        [Fact]
        public void CanDispatchMultipleEventsToMultipleViews()
        {
            var firstRepository = new MemoryRepository<string, SomeEvent>();
            var secondRepository = new MemoryRepository<string, SomeEvent>();
            var views = new View[] { new SomeView(firstRepository), new SomeView(secondRepository) };
            var snapshotRepository = new MemoryRepository<string, Snapshot>();
            var firstEvent = new SomeEvent { Id = "test" };
            var secondEvent = new SomeEvent { Id = "test2" };
            using (var snapshotManager = new SnapshotManager(views, snapshotRepository, Timeout.Infinite))
            {
                // act
                snapshotManager.Dispatch(new Event { SequenceNumber = 1, Payload = firstEvent });
                snapshotManager.Dispatch(new Event { SequenceNumber = 2, Payload = secondEvent });

                // assert
                firstRepository.Get(firstEvent.Id).Should().Be(firstEvent);
                firstRepository.Get(secondEvent.Id).Should().Be(secondEvent);
                secondRepository.Get(firstEvent.Id).Should().Be(firstEvent);
                secondRepository.Get(secondEvent.Id).Should().Be(secondEvent);
            }
        }

        [Fact]
        public void CanSnapshotSingleView()
        {
            var repository = new TestRepository();
            var views = new View[] { new SomeView(repository) };
            var snapshotRepository = new MemoryRepository<string, Snapshot>();
            var @event = new SomeEvent { Id = "test" };
            using (var snapshotManager = new SnapshotManager(views, snapshotRepository, 20))
            {
                snapshotManager.Dispatch(new Event { SequenceNumber = 1, Payload = @event });

                // act
                var preSnapshotEvent = repository.Get(@event.Id);
                repository.WaitUntilSnapshotSaveEnded();
                var postSnapshotEvent = repository.Get(@event.Id);

                // assert
                preSnapshotEvent.Should().BeNull();
                postSnapshotEvent.Should().Be(@event);
            }
        }

        // can snapshot whilst dispatching
        [Fact]
        public void CanSnapshotSingleViewDuringDispatch()
        {
            var repository = new TestRepository();
            var views = new View[] { new SomeView(repository) };
            var snapshotRepository = new MemoryRepository<string, Snapshot>();
            var firstEvent = new SomeEvent { Id = "test" };
            var secondEvent = new SomeEvent { Id = "test2" };
            using (var snapshotManager = new SnapshotManager(views, snapshotRepository, 20))
            {
                snapshotManager.Dispatch(new Event { SequenceNumber = 1, Payload = firstEvent });

                // act
                var preSnapshotEvent = repository.Get(secondEvent.Id);
                repository.WaitUntilSnapshotSaveStarted();
                snapshotManager.Dispatch(new Event { SequenceNumber = 2, Payload = secondEvent });

                repository.WaitUntilSnapshotSaveEnded();
                repository.WaitUntilSnapshotSaveEnded();
                var postSnapshotEvent = repository.Get(secondEvent.Id);

                // assert
                preSnapshotEvent.Should().BeNull();
                postSnapshotEvent.Should().Be(secondEvent);
            }
        }
        
        // cannot snapshot twice
        // does not repeatedly snapshot if not required

        // can load from previous snapshot
        // can recover

        private class SomeView : View
        {
            private readonly IRepository<string, SomeEvent> repository;

            public SomeView(IRepository<string, SomeEvent> repository)
            {
                this.repository = repository;
            }

            public void Consume(SomeEvent @event)
            {
                this.repository.AddOrUpdate(@event.Id, @event);
            }
        }

        private class SomeEvent
        {
            public string Id { get; set; }
        }

        private class TestRepository : MemoryRepository<string, SomeEvent>
        {
            private readonly AutoResetEvent startBulkUpdate = new AutoResetEvent(false);
            private readonly AutoResetEvent endBulkUpdate = new AutoResetEvent(false);

            public override void BulkUpdate(IEnumerable<KeyValuePair<string, SomeEvent>> addOrUpdate, IEnumerable<string> remove)
            {
                Trace.WriteLine("TestRepository.BulkUpdate: this.startBulkUpdate.Set();");
                this.startBulkUpdate.Set();

                base.BulkUpdate(addOrUpdate, remove);

                Trace.WriteLine("TestRepository.BulkUpdate: this.endBulkUpdate.Set();");
                this.endBulkUpdate.Set();
            }

            public void WaitUntilSnapshotSaveStarted()
            {
                Trace.WriteLine("TestRepository.WaitUntilSnapshotSaveStarted: this.startBulkUpdate.WaitOne();");
                this.startBulkUpdate.WaitOne();

                Trace.WriteLine("TestRepository.WaitUntilSnapshotSaveStarted: (exiting...)");
            }

            public void WaitUntilSnapshotSaveEnded()
            {
                Trace.WriteLine("TestRepository.WaitUntilSnapshotSaveEnded: this.endBulkUpdate.WaitOne();");
                this.endBulkUpdate.WaitOne();

                Trace.WriteLine("TestRepository.WaitUntilSnapshotSaveEnded: (exiting...)");
            }
        }

        public void Dispose()
        {
            Trace.Listeners.Remove(this.listner);
            Trace.Close();
        }
    }
}
