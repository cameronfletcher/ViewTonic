// <copyright file="OrderedEventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Threading.Tasks;

    public class OrderedEventDispatcher : IEventDispatcher, IDisposable
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        private readonly IEventDispatcher innerEventDispatcher;
        private readonly ISequenceResolver sequenceResolver;
        private readonly IOrderedBuffer buffer;

        private bool isDisposed;

        public OrderedEventDispatcher(IEventDispatcher innerEventDispatcher, ISequenceResolver sequenceResolver, IOrderedBuffer buffer)
        {
            Guard.Against.Null(() => innerEventDispatcher);
            Guard.Against.Null(() => sequenceResolver);
            Guard.Against.Null(() => buffer);

            this.innerEventDispatcher = innerEventDispatcher;
            this.sequenceResolver = sequenceResolver;
            this.buffer = buffer;

            var task = new Task(() => this.Run(this.cancellationTokenSource.Token));
            task.ContinueWith(t => this.cancellationTokenSource.Dispose(), TaskContinuationOptions.ExecuteSynchronously);
            task.Start();
        }

        public void Dispatch(object @event)
        {
            if (this.isDisposed)
            {
                throw new ObjectDisposedException(this.GetType().Name);
            }

            var sequenceNumber = this.sequenceResolver.GetSequenceNumber(@event);
            this.buffer.Add(sequenceNumber, @event);
        }

        public void Dispose()
        {
            if (this.isDisposed)
            {
                return;
            }

            this.cancellationTokenSource.Cancel();
            this.isDisposed = true;
        }

        private void Run(CancellationToken token)
        {
            while (true)
            {
                OrderedItem item = null;
                try
                {
                    item = this.buffer.Take(token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }

                // NOTE (Cameron): The items here are coming out of the buffer in the correct order.
                this.innerEventDispatcher.Dispatch(item.Value);
            }
        }
    }
}
