namespace ViewTonic
{
    using System.Collections.Generic;
    using ViewTonic.Persistence;
    using ViewTonic.Sdk;

    class xProgram
    {
        public void Do()
        {
            var sequenceResolver = new DefaultSequenceResolver();
            var buffer = new Sdk.OrderedBuffer(0);
            var eventDispatcher = new OrderedEventDispatcher(sequenceResolver, buffer);

            buffer.Add(1, "x");

            eventDispatcher.Dispatch(new AnEvent { SequenceNumber = 2, Value = "Two" });
            eventDispatcher.Dispatch(new AnEvent { SequenceNumber = 1, Value = "One" });
        }
    }

    public class MyView : View
    {
        public MyView(IRepository<string, Data> repository)
        {

        }

        public void Consume(AnEvent @event)
        {

        }
    }

    public class AnEvent
    {
        public long SequenceNumber { get; set; }

        public string Value { get; set; }
    }

    public class Data
    {

    }
}
