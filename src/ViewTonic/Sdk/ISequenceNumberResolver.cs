// <copyright file="ISequenceNumberResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    public interface ISequenceNumberResolver
    {
        long GetSequenceNumber<T>(T @event);
    }
}
