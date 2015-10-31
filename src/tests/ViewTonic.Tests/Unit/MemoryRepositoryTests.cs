namespace ViewTonic.Tests.Unit
{
    using ViewTonic.Persistence.Memory;
    using ViewTonic.Tests.Sdk;

    public class MemoryRepositoryTests : RepositoryTests
    {
        public MemoryRepositoryTests()
            : base(new MemoryRepository<string, string>())
        {
        }
    }
}
