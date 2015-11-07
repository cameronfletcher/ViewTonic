// <copyright file="Snapshot.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Runtime
{
    public class Snapshot
    {
        public string ViewName { get; set; }

        public long ProcessedSequenceNumber { get; set; }

        public long PersistedSequenceNumber { get; set; }

        public int EstimatedMemoryFootprint { get; set; }
    }
}
