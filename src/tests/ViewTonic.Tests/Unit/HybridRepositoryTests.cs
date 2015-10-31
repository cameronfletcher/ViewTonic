namespace ViewTonic.Tests.Unit
{
    using ViewTonic.Persistence.Hybrid;
    using ViewTonic.Persistence.Memory;
    using Xunit;
    using FluentAssertions;
    using ViewTonic.Persistence;
    using System.Collections.Generic;
    using System.Threading;
    using ViewTonic.Tests.Sdk;

    public class HybridRepositoryTests : RepositoryTests
    {
        private const string Add = "ADD";
        private const string Update = "UPDATE";
        private const string Remove = "REMOVE";

        public HybridRepositoryTests()
            : base(new HybridRepository<string, string>(new MemoryRepository<string, string>()))
        {
        }

        [Fact]
        public void CanGetWithNoInjectedDataAndNoLongRunningFlushing()
        {
            // arrange
            var injectedRepository = new MemoryRepository<string, string>();
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            hybridRepository.AddOrUpdate(Update, "update");
            hybridRepository.AddOrUpdate(Remove, "remove");

            // act
            hybridRepository.AddOrUpdate(Add, "add");
            hybridRepository.AddOrUpdate(Update, "update_changed");
            hybridRepository.Remove(Remove);


            var add = hybridRepository.Get(Add);
            var update = hybridRepository.Get(Update);
            var remove = hybridRepository.Get(Remove);

            // assert
            add.Should().Be("add");
            update.Should().Be("update_changed");
            remove.Should().BeNull();
        }

        [Fact]
        public void CanGetWithInjectedDataAndNoLongRunningFlushing()
        {
            // arrange
            var injectedRepository = new MemoryRepository<string, string>();
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            injectedRepository.AddOrUpdate(Update, "update");
            injectedRepository.AddOrUpdate(Remove, "remove");

            // act
            hybridRepository.AddOrUpdate(Add, "add");
            hybridRepository.AddOrUpdate(Update, "update_changed");
            hybridRepository.Remove(Remove);


            var add = hybridRepository.Get(Add);
            var update = hybridRepository.Get(Update);
            var remove = hybridRepository.Get(Remove);

            // assert
            add.Should().Be("add");
            update.Should().Be("update_changed");
            remove.Should().BeNull();
        }

        [Fact]
        public void CanGetWithNoInjectedDataAndLongRunningFlushing()
        {
            // arrange
            var flush = new ManualResetEvent(false);
            var injectedRepository = new TestRepository(flush, null);
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            hybridRepository.AddOrUpdate(Update, "update");
            hybridRepository.AddOrUpdate(Remove, "remove");

            // act
            hybridRepository.AddOrUpdate(Add, "add");
            hybridRepository.AddOrUpdate(Update, "update_changed");
            hybridRepository.Remove(Remove);

            new Thread(() => hybridRepository.Flush()).Start();
            flush.WaitOne();

            // NOTE (Cameron): The hybrid repository is now in a 'flushing' state
            var add = hybridRepository.Get(Add);
            var update = hybridRepository.Get(Update);
            var remove = hybridRepository.Get(Remove);

            // assert
            add.Should().Be("add");
            update.Should().Be("update_changed");
            remove.Should().BeNull();

            flush.Dispose();
        }

        [Fact]
        public void CanGetWithInjectedDataAndLongRunningFlushing()
        {
            // arrange
            var flush = new ManualResetEvent(false);
            var injectedRepository = new TestRepository(flush, null);
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            injectedRepository.AddOrUpdate(Update, "update");
            injectedRepository.AddOrUpdate(Remove, "remove");

            // act
            hybridRepository.AddOrUpdate(Add, "add");
            hybridRepository.AddOrUpdate(Update, "update_changed");
            hybridRepository.Remove(Remove);

            new Thread(() => hybridRepository.Flush()).Start();
            flush.WaitOne();

            // NOTE (Cameron): The hybrid repository is now in a 'flushing' state
            var add = hybridRepository.Get(Add);
            var update = hybridRepository.Get(Update);
            var remove = hybridRepository.Get(Remove);

            // assert
            add.Should().Be("add");
            update.Should().Be("update_changed");
            remove.Should().BeNull();

            flush.Dispose();
        }

        [Fact]
        public void CanGetWithInjectedDataAndChangesDuringLongRunningFlushing()
        {
            // arrange
            var flush = new ManualResetEvent(false);
            var injectedRepository = new TestRepository(flush, null);
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            injectedRepository.AddOrUpdate(Update, "update");
            injectedRepository.AddOrUpdate(Remove, "remove");

            // act
            new Thread(() => hybridRepository.Flush()).Start();
            flush.WaitOne();

            // NOTE (Cameron): The hybrid repository is now in a 'flushing' state
            hybridRepository.AddOrUpdate(Add, "add");
            hybridRepository.AddOrUpdate(Update, "update_changed");
            hybridRepository.Remove(Remove);

            var add = hybridRepository.Get(Add);
            var update = hybridRepository.Get(Update);
            var remove = hybridRepository.Get(Remove);

            // assert
            add.Should().Be("add");
            update.Should().Be("update_changed");
            remove.Should().BeNull();

            flush.Dispose();
        }

        [Fact]
        public void CanFlushWithNoInjectedDataAndNoLongRunningFlushing()
        {
            // arrange
            var injectedRepository = new MemoryRepository<string, string>();
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            hybridRepository.AddOrUpdate(Update, "update");
            hybridRepository.AddOrUpdate(Remove, "remove");

            // act
            hybridRepository.AddOrUpdate(Add, "add");
            hybridRepository.AddOrUpdate(Update, "update_changed");
            hybridRepository.Remove(Remove);
            hybridRepository.Flush();

            var add = injectedRepository.Get(Add);
            var update = injectedRepository.Get(Update);
            var remove = injectedRepository.Get(Remove);

            // assert
            add.Should().Be("add");
            update.Should().Be("update_changed");
            remove.Should().BeNull();
        }

        [Fact]
        public void CanFlushWithInjectedDataAndNoLongRunningFlushing()
        {
            // arrange
            var injectedRepository = new MemoryRepository<string, string>();
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            injectedRepository.AddOrUpdate(Update, "update");
            injectedRepository.AddOrUpdate(Remove, "remove");

            // act
            hybridRepository.AddOrUpdate(Add, "add");
            hybridRepository.AddOrUpdate(Update, "update_changed");
            hybridRepository.Remove(Remove);
            hybridRepository.Flush();

            var add = injectedRepository.Get(Add);
            var update = injectedRepository.Get(Update);
            var remove = injectedRepository.Get(Remove);

            // assert
            add.Should().Be("add");
            update.Should().Be("update_changed");
            remove.Should().BeNull();
        }

        [Fact]
        public void BigNotSoSpecificHybridRepositoryTest()
        {
            var flush = new ManualResetEvent(false);
            var flushContinue = new ManualResetEvent(false);
            var flushEnd = new ManualResetEvent(false);
            var injectedRepository = new TestRepository(flush, flushContinue);
            var hybridRepository = new HybridRepository<string, string>(injectedRepository);

            injectedRepository.AddOrUpdate("A", "A");
            injectedRepository.AddOrUpdate("B", "B");

            new Thread(
                () =>
                {
                    hybridRepository.Flush();
                    flushEnd.Set();
                }).Start();
            
            
            flush.WaitOne();

            hybridRepository.AddOrUpdate("A", "A_changed");
            hybridRepository.Remove("B");
            hybridRepository.AddOrUpdate("C", "C");

            // assertions
            injectedRepository.Get("A").Should().Be("A", "ref1");
            injectedRepository.Get("B").Should().Be("B", "ref2");
            hybridRepository.Get("A").Should().Be("A_changed", "ref3");
            hybridRepository.Get("B").Should().BeNull("ref4");
            hybridRepository.Get("C").Should().Be("C", "ref5");

            flushContinue.Set();
            flushEnd.WaitOne();

            // assertions
            injectedRepository.Get("A").Should().Be("A", "ref6");
            injectedRepository.Get("B").Should().Be("B", "ref7");
            hybridRepository.Get("A").Should().Be("A_changed", "ref8");
            hybridRepository.Get("B").Should().BeNull("ref9");
            hybridRepository.Get("C").Should().Be("C", "ref10");

            var memoryRepository = new MemoryRepository<string, string>();
            hybridRepository = new HybridRepository<string, string>(injectedRepository);

            // NOTE (Cameron): Equivalent of event re-playing.
            hybridRepository.AddOrUpdate("A", "A_changed");
            hybridRepository.Remove("B");
            hybridRepository.AddOrUpdate("C", "C");

            // assertions
            injectedRepository.Get("A").Should().Be("A", "ref11");
            injectedRepository.Get("B").Should().Be("B", "ref12");
            hybridRepository.Get("A").Should().Be("A_changed", "ref13");
            hybridRepository.Get("B").Should().BeNull("ref14");
            hybridRepository.Get("C").Should().Be("C", "ref15");

            hybridRepository.AddOrUpdate("C", "C_changed");
            hybridRepository.AddOrUpdate("D", "D");

            // assertions
            injectedRepository.Get("A").Should().Be("A", "ref16");
            injectedRepository.Get("B").Should().Be("B", "ref17");
            hybridRepository.Get("A").Should().Be("A_changed", "ref18");
            hybridRepository.Get("B").Should().BeNull("ref19");
            hybridRepository.Get("C").Should().Be("C_changed", "ref20");
            hybridRepository.Get("D").Should().Be("D", "ref21");

            hybridRepository.Flush();
            flushContinue.Set();
            flushEnd.WaitOne();

            // assertions
            injectedRepository.Get("A").Should().Be("A_changed", "ref22");
            injectedRepository.Get("B").Should().BeNull("ref23");
            injectedRepository.Get("C").Should().Be("C_changed", "ref24");
            injectedRepository.Get("D").Should().Be("D", "ref25");

            flush.Dispose();
            flushContinue.Dispose();
            flushEnd.Dispose();
        }

        private class TestRepository : IRepository<string, string>
        {
            private readonly IRepository<string, string> memoryRepository = new MemoryRepository<string, string>();

            private readonly EventWaitHandle flush;
            private readonly EventWaitHandle flushContinue;

            public TestRepository(EventWaitHandle flush, EventWaitHandle flushContinue)
            {
                this.flush = flush;
                this.flushContinue = flushContinue;
            }

            public string Get(string identity)
            {
                return this.memoryRepository.Get(identity);
            }

            public void AddOrUpdate(string identity, string entity)
            {
                this.memoryRepository.AddOrUpdate(identity, entity);
            }

            public void Remove(string identity)
            {
                this.memoryRepository.Remove(identity);
            }

            public void Purge()
            {
                this.memoryRepository.Purge();
            }

            public void BulkUpdate(IEnumerable<KeyValuePair<string, string>> addOrUpdate, IEnumerable<string> remove)
            {
                this.flush.Set();

                if (this.flushContinue != null)
                {
                    this.flushContinue.WaitOne();
                }

                this.memoryRepository.BulkUpdate(addOrUpdate, remove);
            }
        }
    }
}
