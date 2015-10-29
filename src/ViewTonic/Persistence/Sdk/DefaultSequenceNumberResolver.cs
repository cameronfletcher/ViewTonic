// <copyright file="DefaultSequenceNumberResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    public class DefaultSequenceNumberResolver : ISequenceNumberResolver
    {
        public long GetSequenceNumber<T>(T @event)
        {
            throw new System.NotImplementedException();
        }
    }
}
