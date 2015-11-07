// <copyright file="IEventResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System.Collections.Generic;

    public interface IEventResolver
    {
        IEnumerable<object> GetEventsFrom(long sequenceNumber);
    }
}
