// <copyright file="MemoryRepository.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Persistence.Memory
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public sealed class MemoryRepository<TIdentity, TEntity> : IRepository<TIdentity, TEntity>
        where TEntity : class
    {
        private readonly ConcurrentDictionary<TIdentity, TEntity> entities;

        public MemoryRepository()
            : this(EqualityComparer<TIdentity>.Default)
        {
        }

        public MemoryRepository(IEqualityComparer<TIdentity> equalityComparer)
        {
            Guard.Against.Null(() => equalityComparer);

            this.entities = new ConcurrentDictionary<TIdentity, TEntity>(equalityComparer);
        }

        public TEntity Get(TIdentity identity)
        {
            TEntity entity;
            return this.entities.TryGetValue(identity, out entity) ? entity : default(TEntity);
        }

        public IEnumerable<KeyValuePair<TIdentity, TEntity>> GetAll()
        {
            return this.entities;
        }

        public void AddOrUpdate(TIdentity identity, TEntity entity)
        {
            this.entities.AddOrUpdate(identity, entity, (i, e) => entity);
        }

        public void Remove(TIdentity identity)
        {
            TEntity entity;
            this.entities.TryRemove(identity, out entity);
        }

        public void Purge()
        {
            this.entities.Clear();
        }

        public void BulkUpdate(IEnumerable<KeyValuePair<TIdentity, TEntity>> addOrUpdate, IEnumerable<TIdentity> remove)
        {
            foreach (var item in addOrUpdate)
            {
                this.entities.AddOrUpdate(item.Key, item.Value, (i, e) => item.Value);
            }

            TEntity entity = null;
            foreach (var identity in remove)
            {
                this.entities.TryRemove(identity, out entity);
            }
        }
    }
}
