// <copyright file="ListExtensions.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Sdk
{
    using System.Collections.Generic;
    using System.Linq;
    using FluentAssertions;
    using ViewTonic.Sdk;

    public static class ListExtensions
    {
        public static void ShouldContainEvents(this IList<Event> events, int from, int to)
        {
            for (var sequenceNumber = from; sequenceNumber <= to; sequenceNumber++)
            {
                var @event = events[sequenceNumber];

                @event.Should().NotBeNull();
                @event.SequenceNumber.Should().Be(sequenceNumber);
                @event.Payload.Should().Be(EventStore.Event.Number(sequenceNumber));
            }
        }

        public static IList<Event> AlreadyHaving(this IList<Event> events, int eventCount)
        {
            Enumerable.Range(0, eventCount + 1).ToList().ForEach(index => events.Add(null));
            return events;
        }
    }
}
