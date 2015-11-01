namespace Playground.Views
{
    using System.Collections.Generic;
    using Playground.Events;
    using ViewTonic;
    using ViewTonic.Persistence;

    public class Things : View
    {
        private const string Root = "root";

        private readonly IRepository<long, Data.Thing> thingRepository;
        private readonly IRepository<string, Data.Things> thingsRepository;

        public Things(IRepository<long, Data.Thing> thingRepository, IRepository<string, Data.Things> thingsRepository)
        {
            this.thingRepository = thingRepository;
            this.thingsRepository = thingsRepository;
        }

        public void Consume(ThingCreated @event)
        {
            var things = this.thingsRepository.Get(Root);
            if (things == null)
            {
                things = new Data.Things { Names = new List<string>() };
            }

            things.Names.Add(@event.Name);

            this.thingsRepository.AddOrUpdate(Root, things);
            this.thingRepository.AddOrUpdate(@event.Id, new Data.Thing { Name = @event.Name });
        }

        public void Consume(ThingUpdated @event)
        {
            var thing = this.thingRepository.Get(@event.Id);
            var things = this.thingsRepository.Get(Root);

            things.Names.Remove(thing.Name);
            things.Names.Add(@event.Name);
            thing.Name = @event.Name;
            
            this.thingsRepository.AddOrUpdate(Root, things);
            this.thingRepository.AddOrUpdate(@event.Id, thing);
        }

        public void Consume(ThingDeleted @event)
        {
            var thing = this.thingRepository.Get(@event.Id);
            var things = this.thingsRepository.Get(Root);

            things.Names.Remove(thing.Name);

            this.thingsRepository.AddOrUpdate(Root, things);
            this.thingRepository.Remove(@event.Id);

        }
    }
}
