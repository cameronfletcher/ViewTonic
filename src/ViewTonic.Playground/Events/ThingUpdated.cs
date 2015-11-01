namespace Playground.Events
{
    public class ThingUpdated
    {
        public long Checkpoint { get; set; }

        public int Id { get; set; }

        public string Name { get; set; }
    }
}
