// <copyright file="Event.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Diagnostics;

    // TODO (Cameron): Consider struct?
    [DebuggerDisplay("{SequenceNumber}: {Payload}")]
    public class Event
    {
        public long SequenceNumber { get; set; }

        public object Payload { get; set; }
    }
}
