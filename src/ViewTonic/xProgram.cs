
using ViewTonic.Persistence;
namespace ViewTonic
{
    class xProgram
    {
        public void Do()
        {
            var eventDispatcher = new EventDispatcher();
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
