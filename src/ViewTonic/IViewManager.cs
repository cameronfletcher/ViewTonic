// <copyright file="IViewManager.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using System;

    public interface IViewManager : IDisposable
    {
        void QueueForDispatch(object @event);

        void TriggerEventReplaying();
    }
}
