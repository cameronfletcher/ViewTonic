// <copyright file="DefaultEventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Collections.Generic;

    public sealed class DefaultEventDispatcher : IEventDispatcher
    {
        private readonly List<View> views;

        public DefaultEventDispatcher(IEnumerable<View> views)
        {
            Guard.Against.NullOrEmptyOrNullElements(() => views);

            this.views = new List<View>(views);
        }

        public void Dispatch(object @event)
        {
            foreach (var view in views)
            {
                view.Apply(@event);
            }
        }
    }
}
