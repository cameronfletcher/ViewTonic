// <copyright file="MemoryRepositoryTests.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

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
