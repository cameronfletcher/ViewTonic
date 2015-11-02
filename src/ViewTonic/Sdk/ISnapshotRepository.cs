// <copyright file="ISnapshotRepository.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    public interface ISnapshotRepository
    {
        void TakeSnapshot();

        void FlushSnapshot();
    }
}
