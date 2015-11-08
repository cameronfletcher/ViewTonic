// <copyright file="MemoryRepository.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Persistence.Memory
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;

    public class MemoryRepository<TIdentity, TEntity> : IRepository<TIdentity, TEntity>
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

        public virtual TEntity Get(TIdentity identity)
        {
            TEntity entity;
            return this.entities.TryGetValue(identity, out entity) ? entity : default(TEntity);
        }

        public virtual IEnumerable<KeyValuePair<TIdentity, TEntity>> GetAll()
        {
            return this.entities;
        }

        public virtual void AddOrUpdate(TIdentity identity, TEntity entity)
        {
            this.entities.AddOrUpdate(identity, entity, (i, e) => entity);
        }

        public virtual void Remove(TIdentity identity)
        {
            TEntity entity;
            this.entities.TryRemove(identity, out entity);
        }

        public virtual void Purge()
        {
            this.entities.Clear();
        }

        public virtual void BulkUpdate(IEnumerable<KeyValuePair<TIdentity, TEntity>> addOrUpdate, IEnumerable<TIdentity> remove)
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
