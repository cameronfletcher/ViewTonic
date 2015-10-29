namespace ViewTonic.Tests
{
    using ViewTonic.Persistence.Memory;

    public class MemoryRepositoryTests : BasicRepositoryTests
    {
        public MemoryRepositoryTests()
            : base(new MemoryRepository<string, string>())
        {
        }
    }
}
