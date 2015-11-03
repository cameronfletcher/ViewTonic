// <copyright file="HybridRepository.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Persistence.Hybrid
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using ViewTonic.Sdk;

    public sealed class HybridRepository<TIdentity, TEntity> : IRepository<TIdentity, TEntity>, ISnapshotRepository
        where TEntity : class
    {
        private readonly Memory.MemoryRepository<TIdentity, TEntity> workingCache = new Memory.MemoryRepository<TIdentity, TEntity>();
        private readonly Memory.MemoryRepository<TIdentity, TEntity> temporaryCache = new Memory.MemoryRepository<TIdentity, TEntity>();
        private readonly ConcurrentSet<TIdentity> removedFromWorkingCache = new ConcurrentSet<TIdentity>();
        private readonly ConcurrentSet<TIdentity> removedFromTemporaryCache = new ConcurrentSet<TIdentity>();

        // NOTE (Cameron): There is the potential to introduce a second lock here for flushing the working cache but I'm not sure it's necessary.
        private readonly ReaderWriterLockSlim @lock = new ReaderWriterLockSlim();

        private readonly IRepository<TIdentity, TEntity> repository;

        private bool snapshotMode;

        public HybridRepository(IRepository<TIdentity, TEntity> repository)
        {
            Guard.Against.Null(() => repository);

            this.repository = repository;
        }

        public TEntity Get(TIdentity identity)
        {
            if (this.removedFromTemporaryCache.Contains(identity))
            {
                return null;
            }

            var entity = this.temporaryCache.Get(identity);
            if (entity != null)
            {
                return entity;
            }

            if (this.removedFromWorkingCache.Contains(identity))
            {
                return null;
            }

            return this.workingCache.Get(identity) ?? this.repository.Get(identity);
        }

        public void AddOrUpdate(TIdentity identity, TEntity entity)
        {
            if (this.snapshotMode)
            {
                this.@lock.EnterReadLock();

                try
                {
                    this.temporaryCache.AddOrUpdate(identity, entity);
                    this.removedFromTemporaryCache.Remove(identity);
                }
                finally
                {
                    this.@lock.ExitReadLock();
                }
            }
            else
            {
                this.workingCache.AddOrUpdate(identity, entity);
                this.removedFromWorkingCache.Remove(identity);
            }

        }

        public void Remove(TIdentity identity)
        {
            if (this.snapshotMode)
            {
                this.@lock.EnterReadLock();

                try
                {
                    this.removedFromTemporaryCache.Add(identity);
                    this.temporaryCache.Remove(identity);
                }
                finally
                {
                    this.@lock.ExitReadLock();
                }
            }
            else
            {
                this.removedFromWorkingCache.Add(identity);
                this.workingCache.Remove(identity);
            }
        }

        public void TakeSnapshot()
        {
            if (this.snapshotMode)
            {
                throw new InvalidOperationException("Already in snapshot mode.");
            }

            this.snapshotMode = true;
        }

        public void FlushSnapshot()
        {
            if (!this.snapshotMode)
            {
                throw new InvalidOperationException("No snapshot to flush! Call TakeSnapshot() first.");
            }

            // NOTE (Cameron): This is a potentially long-running operation...
            this.repository.BulkUpdate(this.workingCache.GetAll(), this.removedFromWorkingCache);

            this.workingCache.Purge();
            this.removedFromWorkingCache.Clear();

            this.@lock.EnterWriteLock();

            try
            {
                this.workingCache.BulkUpdate(this.temporaryCache.GetAll(), this.removedFromTemporaryCache);
                this.removedFromWorkingCache.UnionWith(this.removedFromTemporaryCache);

                this.temporaryCache.Purge();
                this.removedFromTemporaryCache.Clear();
            }
            finally
            {
                this.@lock.ExitWriteLock();
            }

            this.snapshotMode = false;
        }

        public void Purge()
        {
            this.temporaryCache.Purge();
            this.removedFromWorkingCache.Clear();

            this.workingCache.Purge();
            this.removedFromTemporaryCache.Clear();

            this.repository.Purge();
        }

        public void BulkUpdate(IEnumerable<KeyValuePair<TIdentity, TEntity>> addOrUpdate, IEnumerable<TIdentity> remove)
        {
            foreach (var item in addOrUpdate)
            {
                this.AddOrUpdate(item.Key, item.Value);
            }

            foreach (var identity in remove)
            {
                this.Remove(identity);
            }
        }
    }
}
