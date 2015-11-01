// <copyright file="OrderedEventDispatcher.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Sdk
{
    using System.Threading;
    using ViewTonic.Sdk;

    public sealed class OrderedEventDispatcher : IEventDispatcher
    {
        private readonly CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private readonly ISequenceResolver sequenceResolver;
        private readonly IOrderedBuffer buffer;
        private readonly IEventDispatcher innerEventDispatcher;

        private bool isDisposed;

        public OrderedEventDispatcher(ISequenceResolver sequenceResolver, IOrderedBuffer buffer, IEventDispatcher innerEventDispatcher)
        {
            Guard.Against.Null(() => sequenceResolver);
            Guard.Against.Null(() => buffer);
            Guard.Against.Null(() => sequenceResolver);

            this.sequenceResolver = sequenceResolver;
            this.buffer = buffer;
            this.innerEventDispatcher = innerEventDispatcher;

            new Thread(this.Run).Start();
        }

        public void Dispatch(object @event)
        {
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
            this.cancellationTokenSource.Dispose();
        }

        private void Run()
        {
            while (true)
            {
                var item = this.buffer.Take(cancellationTokenSource.Token);

                // NOTE (Cameron): The items here are coming out of the buffer in the correct order.
                this.innerEventDispatcher.Dispatch(item.Value);
            }
        }
    }
}
