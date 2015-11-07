// <copyright file="ISnapshotManager.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    public interface ISnapshotManager
    {
        void Dispatch(Event @event);
    }
}
