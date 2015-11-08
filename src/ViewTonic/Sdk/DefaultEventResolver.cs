// <copyright file="DefaultEventResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Generic;

    public sealed class DefaultEventResolver : IEventResolver
    {
        private readonly Func<long, IEnumerable<object>> eventResolver;

        public DefaultEventResolver(Func<long, IEnumerable<object>> eventResolver)
        {
            Guard.Against.Null(() => eventResolver);

            this.eventResolver = eventResolver;
        }

        public IEnumerable<object> GetEventsFrom(long sequenceNumber)
        {
            return this.eventResolver.Invoke(sequenceNumber);
        }
    }
}
