// <copyright file="RepositoryTests.cs" company="ViewTonic contributors">
//  Copyright (c) ViewTonic contributors. All rights reserved.
// </copyright>

namespace ViewTonic.Tests.Sdk
{
    using System;
    using FluentAssertions;
    using ViewTonic.Persistence;
    using ViewTonic.Persistence.Memory;
    using Xunit;

    public abstract class RepositoryTests
    {
        private readonly IRepository<string, string> repository;

        protected RepositoryTests(IRepository<string, string> repository)
        {
            this.repository = repository;
        }

        [Fact]
        public void CanAddAndRetrieveValue()
        {
            // arrange
            var identity = "value";
            var expected = "entity";

            // act
            repository.AddOrUpdate(identity, expected);
            var actual = repository.Get(identity);

            // assert
            actual.Should().Be(expected);
        }

        [Fact]
        public void CanUpdateValue()
        {
            // arrange
            var identity = "value";
            var expected = "entity";

            // act
            repository.AddOrUpdate(identity, "some other value");
            repository.AddOrUpdate(identity, expected);
            var actual = repository.Get(identity);

            // assert
            actual.Should().Be(expected);
        }

        [Fact]
        public void CanRemoveValue()
        {
            // arrange
            var identity = "value";

            // act
            repository.AddOrUpdate(identity, "entity");
            repository.Remove(identity);
            var actual = repository.Get(identity);

            // assert
            actual.Should().BeNull();
        }

        [Fact]
        public void DoesNotThrowOnGetMissingEntity()
        {
            // arrange
            string actual = null;

            // act
            Action action = () => actual = repository.Get("missing");

            // assert
            action.ShouldNotThrow();
            actual.Should().BeNull();
        }

        [Fact]
        public void DoesNotThrowOnRemoveMissingEntity()
        {
            // arrange
            var repository = new MemoryRepository<string, string>();

            // act
            Action action = () => repository.Remove("missing");

            // assert
            action.ShouldNotThrow();
        }
    }
}
