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
# Запустить все тесты (требует запущенный Docker)
dotnet test tests/FastIntegrationTests.Tests

# Запустить один подход (bash / Git Bash)
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Testcontainers"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Respawn"

# С повторами — сравнение производительности (bash)
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL"
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Testcontainers"
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Respawn"

# PowerShell
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL"
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Testcontainers"
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Respawn"

# Только накладные расходы инфраструктуры (пустые тесты, измеряют InitializeAsync)
TEST_REPEAT=100 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Overhead"

# Переопределить количество потоков прямо из CLI
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL" -- xUnit.MaxParallelThreads=8

# Запустить тесты отдельного класса
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductServiceTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductsApiTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~OrderServiceTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~OrdersApiTests"
```

### Как работают тесты

- **Требование:** Docker должен быть запущен. Testcontainers автоматически поднимает контейнеры PostgreSQL и IntegreSQL.
- **Изоляция:** каждый тест получает клон шаблонной БД через IntegreSQL (~5 мс), применяет бизнес-операции, возвращает базу после завершения.
- **Параллелизм:** тест-классы выполняются параллельно (`maxParallelThreads = 4` в `xunit.runner.json`).
- **Инфраструктура скрыта:** тесты работают только через `IProductService` / `IOrderService` (сервисный уровень) или `HttpClient` (HTTP-уровень). Создание и удаление базы данных происходит в базовых классах `AppServiceTestBase` / `ComponentTestBase` (IntegreSQL) и `ContainerServiceTestBase` / `ContainerApiTestBase` (Testcontainers).

## Архитектура

Трёхслойная архитектура:

- **Application** — доменные сущности (`Entities/`), перечисления (`Enums/`), DTO (`DTOs/`), интерфейсы репозиториев и сервисов (`Interfaces/`), сервисы бизнес-логики (`Services/`), доменные исключения (`Exceptions/`). Не зависит от EF Core и конкретной СУБД.
- **Infrastructure** — реализация репозиториев через EF Core (`Repositories/`), `ShopDbContext` с конфигурациями (`Data/`), extension-методы регистрации DI (`Extensions/ServiceCollectionExtensions.cs`).
- **WebApi** — контроллеры (`Controllers/`), `Program.cs` с DI-конфигурацией, глобальная обработка ошибок (`Middleware/GlobalExceptionHandler.cs`).
- **Tests** (`tests/FastIntegrationTests.Tests/`) — интеграционные тесты. Инфраструктура тестов в `Infrastructure/` (фикстуры, фабрики, базовые классы). Тест-классы в `Products/` и `Orders/`.

## Локальная разработка

Файл `appsettings.Development.json` не хранится в репозитории. После клонирования скопировать шаблон:
```bash
cp src/FastIntegrationTests.WebApi/appsettings.Development.json.example src/FastIntegrationTests.WebApi/appsettings.Development.json
```
Затем заполнить строку подключения PostgreSQL.

## Соглашения

- Документация и комментарии на русском языке.
- Все публичные классы и методы — с XML-документацией (`/// <summary>`).
- Все async-методы с `CancellationToken ct` параметром обязательно документируются тегом `<param name="ct">Токен отмены операции.</param>`.
- Коммит после каждого логического шага.

## Архитектурные ограничения

- **Application не зависит от EF Core.** Проект `FastIntegrationTests.Application` не содержит ни одного `<PackageReference>` на EF Core или провайдеры БД. Добавление таких зависимостей — нарушение архитектуры.
