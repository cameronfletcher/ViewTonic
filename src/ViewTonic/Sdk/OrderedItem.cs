// <copyright file="OrderedItem.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    // TODO (Cameron): Consider struct?
    public class OrderedItem
    {
        public long SequenceNumber { get; set; }

        public object Value { get; set; }
    }
}
