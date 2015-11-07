// <copyright file="IEventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    public interface IEventDispatcher
    {
        void Dispatch(object @event);
    }
}
