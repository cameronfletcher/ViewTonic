// <copyright file="IEventResolver.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    public interface IEventResolver
    {
        object GetEvent(long sequenceNumber);
    }
}
