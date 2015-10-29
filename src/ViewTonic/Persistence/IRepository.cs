// <copyright file="IRepository.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

using System.Collections.Generic;
namespace ViewTonic.Persistence
{
    public interface IRepository<TIdentity, TEntity>
        where TEntity : class
    {
        TEntity Get(TIdentity identity);

        void AddOrUpdate(TIdentity identity, TEntity entity);

        void Remove(TIdentity identity);

        void Purge();

        void BulkUpdate(IEnumerable<KeyValuePair<TIdentity, TEntity>> addOrUpdate, IEnumerable<TIdentity> remove);
    }
}
