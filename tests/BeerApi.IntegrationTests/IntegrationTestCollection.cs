namespace BeerApi.IntegrationTests;

[CollectionDefinition(nameof(IntegrationTestCollection))]
public class IntegrationTestCollection : ICollectionFixture<CustomWebApplicationFactory>
{
}
