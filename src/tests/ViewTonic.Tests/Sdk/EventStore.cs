// <copyright file="EventStore.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Sdk
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Globalization;
    using System.Linq;
    using System.Threading;

    public class EventStore : IEventResolver, ISequenceResolver
    {
        private readonly ConcurrentDictionary<long, Event> events = new ConcurrentDictionary<long, Event>();
        private readonly AutoResetEvent autoResetEvent = new AutoResetEvent(false);

        public EventStore WithEventCount(int eventCount)
        {
            this.HasEventCount(eventCount);
            return this;
        }

        public void HasEventCount(int eventCount)
        {
            Enumerable.Range(1, eventCount)
                .Select(sequenceNumber => Event.Number(sequenceNumber))
                .ToList()
                .ForEach(@event => this.events.TryAdd(@event.SequenceNumber, @event));
        }

        public void HasAdditionalEventCount(int eventCount)
        {
            Enumerable.Range(this.events.Count + 1, eventCount)
                .Select(sequenceNumber => Event.Number(sequenceNumber))
                .ToList()
                .ForEach(@event => this.events.TryAdd(@event.SequenceNumber, @event));
        }

        public void WaitForCall()
        {
            Trace.WriteLine("EventStore.WaitForCall()");
            this.autoResetEvent.WaitOne();
        }

        public IEventResolver EventResolver
        {
            get { return this; }
        }

        public ISequenceResolver SequenceResolver
        {
            get { return this; }
        }

        [DebuggerDisplay("{Value}")]
        public class Event : IEquatable<Event>
        {
            public static Event Number(int sequenceNumber)
            {
                return new Event
                {
                    SequenceNumber = sequenceNumber,
                    Value = string.Format(CultureInfo.InvariantCulture, "event{0}", sequenceNumber),
                };
            }

            public int SequenceNumber { get; set; }

            public string Value { get; set; }

            public override bool Equals(object obj)
            {
                return this.Equals((Event)obj);
            }

            public bool Equals(Event other)
            {
                return this.SequenceNumber == other.SequenceNumber && this.Value == other.Value;
            }

            public override int GetHashCode()
            {
                return unchecked(this.SequenceNumber.GetHashCode() * this.Value.GetHashCode());
            }
        }
    
        IEnumerable<object> IEventResolver.GetEventsFrom(long sequenceNumber)
        {
            Trace.WriteLine("IEventResolver.GetEventsFrom(long sequenceNumber)");

            foreach (var kvp in this.events.OrderBy(e => e.Key))
            {
                if (kvp.Key < sequenceNumber)
                {
                    continue;
                }

                Trace.WriteLine("IEventResolver.GetEventsFrom: yield return " + kvp.Key.ToString());
                yield return kvp.Value;
            }

            this.autoResetEvent.Set();
        }

        long ISequenceResolver.GetSequenceNumber(object eventPayload)
        {
            var @event = (EventStore.Event)eventPayload;
            return @event.SequenceNumber;
        }
    }
}
