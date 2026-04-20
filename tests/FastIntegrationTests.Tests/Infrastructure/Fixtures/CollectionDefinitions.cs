namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>Коллекция тестов сервисного уровня для Products через Testcontainers.</summary>
[CollectionDefinition("ProductsServiceContainer")]
public class ProductsServiceContainerCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция тестов HTTP-уровня для Products через Testcontainers.</summary>
[CollectionDefinition("ProductsApiContainer")]
public class ProductsApiContainerCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция тестов сервисного уровня для Orders через Testcontainers.</summary>
[CollectionDefinition("OrdersServiceContainer")]
public class OrdersServiceContainerCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция тестов HTTP-уровня для Orders через Testcontainers.</summary>
[CollectionDefinition("OrdersApiContainer")]
public class OrdersApiContainerCollection : ICollectionFixture<ContainerFixture> { }
