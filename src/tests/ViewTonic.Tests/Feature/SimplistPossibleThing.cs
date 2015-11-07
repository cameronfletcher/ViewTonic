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

    public class SimplistPossibleThing
    {
        [Scenario]
        public void Example01(
            IRepository<string, SomeEvent> repository,
            View view,
            IViewManager viewManager,
            SomeEvent @event)
        {
            "Given a view repository"._(() => repository = new MemoryRepository<string, SomeEvent>());
            "and a view"._(() => view = new SomeView(repository));
            "and a view manager"._(ctx => viewManager = ViewManager.ForViews(view).Create().Using(ctx));
            "and an event"._(() => @event = new SomeEvent { Id = "test" });
            "when that event is dispatched"._(() => viewManager.QueueForDispatch(@event));
            "and the operation is given time to process"._(() => Thread.Sleep(1000));
            "then the repository contains the denormalized event"._(() => repository.Get(@event.Id).Should().Be(@event));
        }

        public class SomeView : View
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

        public class SomeEvent
        {
            public string Id { get; set; }
        }
    }
}
