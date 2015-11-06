// <copyright file="DefaultEventResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;

    public sealed class DefaultEventResolver : IEventResolver
    {
        private readonly Func<long, object> eventResolver;

        public DefaultEventResolver(Func<long, object> eventResolver)
        {
            Guard.Against.Null(() => eventResolver);

            this.eventResolver = eventResolver;
        }

        public object GetEvent(long sequenceNumber)
        {
            return this.eventResolver.Invoke(sequenceNumber);
        }
    }
}
