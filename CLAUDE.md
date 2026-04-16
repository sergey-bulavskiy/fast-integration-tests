# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Команды разработки

```bash
# Сборка
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

## Архитектура

Трёхслойная архитектура:

- **Application** — доменные сущности (`Entities/`), перечисления (`Enums/`), DTO (`DTOs/`), интерфейсы репозиториев и сервисов (`Interfaces/`), сервисы бизнес-логики (`Services/`), доменные исключения (`Exceptions/`). Не зависит от EF Core и конкретной СУБД.
- **Infrastructure** — реализация репозиториев через EF Core (`Repositories/`), `ShopDbContext` с конфигурациями (`Data/`), extension-методы регистрации DI (`Extensions/ServiceCollectionExtensions.cs`).
- **WebApi** — контроллеры (`Controllers/`), `Program.cs` с DI-конфигурацией, глобальная обработка ошибок (`Middleware/GlobalExceptionHandler.cs`).

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
