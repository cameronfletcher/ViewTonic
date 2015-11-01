namespace Playground.Views
{
    using Playground.Events;
    using ViewTonic;
    using ViewTonic.Persistence;

    public class Thing : View
    {
        private readonly IRepository<long, Data.Thing> repository;

        public Thing(IRepository<long, Data.Thing> repository)
        {
            this.repository = repository;
        }

        public void Consume(ThingCreated @event)
        {
            this.repository.AddOrUpdate(@event.Id, new Data.Thing { Name = @event.Name });
        }

        public void Consume(ThingUpdated @event)
        {
            this.repository.AddOrUpdate(@event.Id, new Data.Thing { Name = @event.Name });
        }

        public void Consume(ThingDeleted @event)
        {
            this.repository.Remove(@event.Id);

        }
    }
}
