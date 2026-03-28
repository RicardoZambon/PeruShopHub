using Xunit;

namespace PeruShopHub.IntegrationTests.Infrastructure;

/// <summary>
/// Collection definition that shares a single CustomWebApplicationFactory
/// (and thus a single set of TestContainers) across all integration tests.
/// </summary>
[CollectionDefinition("Integration")]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
