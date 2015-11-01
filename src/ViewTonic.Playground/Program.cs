namespace Playground
{
    using System.Collections.Generic;
    using ViewTonic;
    using ViewTonic.Persistence.Memory;

    internal class Program
    {
        public static void Main(string[] args)
        {
            var repository1 = new MemoryRepository<long, Data.Thing>();
            var repository2 = new MemoryRepository<long, Data.Thing>();
            var repository3 = new MemoryRepository<string, Data.Things>();

            var view1 = new Views.Thing(repository1);
            var view2 = new Views.Things(repository2, repository3);

            var eventDispatcher = EventDispatcher.Create(view1, view2);

            var events = new List<object>
            {
                { new Events.ThingCreated { Checkpoint = 1, Id = 1, Name = "A" } },
                { new Events.ThingCreated { Checkpoint = 2, Id = 2, Name = "B" } },
                { new Events.ThingCreated { Checkpoint = 3, Id = 3, Name = "C" } },
                { new Events.ThingUpdated { Checkpoint = 4, Id = 1, Name = "A_changed" } },
                { new Events.ThingDeleted { Checkpoint = 5, Id = 2 } },
            };

            foreach (var @event in events)
            {
                eventDispatcher.Dispatch(@event);
            }

        }
    }
}
