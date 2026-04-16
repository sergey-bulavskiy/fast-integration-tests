# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Команды разработки

```bash
# Сборка всего решения
dotnet build

# Запуск сервиса
dotnet run --project src/FastIntegrationTests.WebApi

# Восстановить локальные инструменты (нужно один раз после клонирования)
dotnet tool restore

# Запустить PostgreSQL локально
docker-compose up postgres -d

# Запустить MSSQL локально
docker-compose up mssql -d

# Добавить новую миграцию
dotnet ef migrations add <НазваниеМиграции> \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi \
  --output-dir Migrations

# Применить миграции
dotnet ef database update \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi
```

## Интеграционные тесты

```bash
# Запустить все 53 теста (требует запущенный Docker)
dotnet test tests/FastIntegrationTests.Tests

# Запустить с подробным выводом
dotnet test tests/FastIntegrationTests.Tests --verbosity normal

# Запустить отдельную коллекцию
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=ProductsService"
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=ProductsApi"
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=OrdersService"
dotnet test tests/FastIntegrationTests.Tests --filter "Collection=OrdersApi"
```

### Как работают тесты

- **Требование:** Docker должен быть запущен. Testcontainers автоматически поднимает контейнер PostgreSQL или MSSQL.
- **Провайдер БД** определяется из `tests/FastIntegrationTests.Tests/appsettings.json` — по умолчанию `PostgreSQL`. Менять вручную не нужно: Testcontainers сам запустит нужный контейнер.
- **Изоляция:** каждый тест создаёт отдельную базу `test_{guid}`, применяет миграции через EF Core, удаляет базу после завершения.
- **Параллелизм:** 4 xUnit-коллекции выполняются параллельно (`maxParallelThreads = 4` в `xunit.runner.json`).
- **Инфраструктура скрыта:** тесты работают только через `IProductService` / `IOrderService` (сервисный уровень) или `HttpClient` (HTTP-уровень). Создание и удаление базы данных происходит в базовых классах `ServiceTestBase` / `ApiTestBase`.

## Архитектура

Трёхслойная архитектура:

- **Application** — доменные сущности (`Entities/`), перечисления (`Enums/`), DTO (`DTOs/`), интерфейсы репозиториев и сервисов (`Interfaces/`), сервисы бизнес-логики (`Services/`), доменные исключения (`Exceptions/`). Не зависит от EF Core и конкретной СУБД.
- **Infrastructure** — реализация репозиториев через EF Core (`Repositories/`), `ShopDbContext` с конфигурациями (`Data/`), extension-методы регистрации DI (`Extensions/ServiceCollectionExtensions.cs`).
- **WebApi** — контроллеры (`Controllers/`), `Program.cs` с DI-конфигурацией, глобальная обработка ошибок (`Middleware/GlobalExceptionHandler.cs`).
- **Tests** (`tests/FastIntegrationTests.Tests/`) — интеграционные тесты. Инфраструктура тестов в `Infrastructure/` (фикстуры, фабрики, базовые классы). Тест-классы в `Products/` и `Orders/`.

## Переключение провайдера БД

Файл `appsettings.Development.json` не хранится в репозитории. После клонирования скопировать шаблон:
```bash
cp src/FastIntegrationTests.WebApi/appsettings.Development.json.example src/FastIntegrationTests.WebApi/appsettings.Development.json
```
Затем в скопированном файле изменить `"DatabaseProvider"` на `"PostgreSQL"` или `"MSSQL"` и заполнить строки подключения. Оба docker-сервиса объявлены в `docker-compose.yml`.

## Соглашения

- Документация и комментарии на русском языке.
- Все публичные классы и методы — с XML-документацией (`/// <summary>`).
- Все async-методы с `CancellationToken ct` параметром обязательно документируются тегом `<param name="ct">Токен отмены операции.</param>`.
- Коммит после каждого логического шага.

## Архитектурные ограничения

- **Application не зависит от EF Core.** Проект `FastIntegrationTests.Application` не содержит ни одного `<PackageReference>` на EF Core или провайдеры БД. Добавление таких зависимостей — нарушение архитектуры.
- **Миграции сгенерированы под PostgreSQL.** Файлы в `src/FastIntegrationTests.Infrastructure/Migrations/` содержат Npgsql-специфичные аннотации (`NpgsqlValueGenerationStrategy`) и типы (`timestamp with time zone`). При необходимости поддержки MSSQL как основного провайдера миграции нужно пересоздавать — это известное ограничение учебного проекта (зафиксировано в комментарии `Program.cs`).
