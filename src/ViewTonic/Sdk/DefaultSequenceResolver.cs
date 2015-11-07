// <copyright file="DefaultSequenceResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;

    public sealed class DefaultSequenceResolver : ISequenceResolver
    {
        private readonly Func<object, long> sequenceResolver;

        public DefaultSequenceResolver(string eventPropertyName)
        {
            throw new NotImplementedException();
        }
        
        public DefaultSequenceResolver(Func<object, long> sequenceResolver)
        {
            Guard.Against.Null(() => sequenceResolver);

            this.sequenceResolver = sequenceResolver;
        }

        public long GetSequenceNumber(object eventPayload)
        {
            return this.sequenceResolver.Invoke(eventPayload);
        }
    }
}
