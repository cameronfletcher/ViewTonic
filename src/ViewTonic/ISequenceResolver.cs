// <copyright file="ISequenceResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    public interface ISequenceResolver
    {
        long GetSequenceNumber(object message);
    }
}
