// <copyright file="EventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic
{
    using ViewTonic.Sdk;

    public sealed class EventDispatcher
    {
        public static IEventDispatcher Create(params View[] views)
        {
            var eventDispatcher = new DefaultEventDispatcher(views);

            return eventDispatcher;
        }
    }
}
