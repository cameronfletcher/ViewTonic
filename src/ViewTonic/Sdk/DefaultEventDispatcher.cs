// <copyright file="DefaultEventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Collections.Generic;

    public sealed class DefaultEventDispatcher : IEventDispatcher
    {
        private readonly List<View> views = new List<View>();

        public DefaultEventDispatcher()
        {
        }

        public void Dispatch(object @event)
        {
            // efficiently dispatch to all views
        }
    }
}
