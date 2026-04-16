using Xunit;

namespace FastIntegrationTests.Tests.Infrastructure.Fixtures;

/// <summary>Коллекция для сервисных тестов продуктов.</summary>
[CollectionDefinition("ProductsService")]
public class ProductsServiceCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция для HTTP-тестов продуктов.</summary>
[CollectionDefinition("ProductsApi")]
public class ProductsApiCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция для сервисных тестов заказов.</summary>
[CollectionDefinition("OrdersService")]
public class OrdersServiceCollection : ICollectionFixture<ContainerFixture> { }

/// <summary>Коллекция для HTTP-тестов заказов.</summary>
[CollectionDefinition("OrdersApi")]
public class OrdersApiCollection : ICollectionFixture<ContainerFixture> { }
