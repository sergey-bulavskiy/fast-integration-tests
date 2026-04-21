# FastIntegrationTests

Учебный проект: REST API магазина на .NET 8 с полным набором интеграционных тестов на реальной PostgreSQL.

Демонстрирует и сравнивает три подхода к изоляции тестов в паттерне **database-per-test**:

| Подход | Изоляция | Скорость сброса |
|--------|----------|-----------------|
| **IntegreSQL** | клон шаблонной БД на каждый тест | ~5 мс |
| **Respawn** | TRUNCATE CASCADE между тестами | ~1 мс |
| **Testcontainers** | EnsureDeleted + MigrateAsync на каждый тест | ~200 мс |

---

## Требования

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

---

## Быстрый старт

```bash
# 1. Восстановить инструменты EF CLI
dotnet tool restore

# 2. Поднять PostgreSQL
docker-compose up postgres -d

# 3. Создать appsettings.Development.json из шаблона
cp src/FastIntegrationTests.WebApi/appsettings.Development.json.example \
   src/FastIntegrationTests.WebApi/appsettings.Development.json
# Заполнить строку подключения:
# "PostgreSQL": "Host=localhost;Database=shop;Username=postgres;Password=postgres"

# 4. Применить миграции
dotnet ef database update \
  --project src/FastIntegrationTests.Infrastructure \
  --startup-project src/FastIntegrationTests.WebApi

# 5. Запустить сервис
dotnet run --project src/FastIntegrationTests.WebApi
```

После запуска Swagger UI доступен по адресу: `https://localhost:{port}/swagger`

---

## API

### Products

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/products` | Список всех товаров |
| GET | `/api/products/{id}` | Товар по ID |
| POST | `/api/products` | Создать товар |
| PUT | `/api/products/{id}` | Обновить товар |
| DELETE | `/api/products/{id}` | Удалить товар |

### Orders

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/orders` | Список всех заказов |
| GET | `/api/orders/{id}` | Заказ по ID (с позициями) |
| POST | `/api/orders` | Создать заказ |
| POST | `/api/orders/{id}/confirm` | Подтвердить заказ |
| POST | `/api/orders/{id}/ship` | Отправить заказ |
| POST | `/api/orders/{id}/complete` | Завершить заказ |
| POST | `/api/orders/{id}/cancel` | Отменить заказ |

#### Жизненный цикл заказа

```
New → Confirmed → Shipped → Completed
 └──────────────→ Cancelled
```

---

## Запуск тестов

Docker должен быть запущен. Контейнеры поднимаются автоматически через Testcontainers.

```bash
# Все 159 тестов (три подхода вместе)
dotnet test tests/FastIntegrationTests.Tests

# Один подход
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Respawn"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.Testcontainers"

# Отдельный тест-класс
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~ProductServiceTests"
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~OrdersApiTests"
```

### PowerShell скрипты (с замером времени)

```powershell
# По умолчанию: 5 повторов, 4 потока
.\run-integresql.ps1
.\run-respawn.ps1
.\run-testcontainers.ps1

# Переопределить параметры
.\run-integresql.ps1 -Repeat 20 -Threads 8
```

### Сравнение подходов

| | IntegreSQL | Respawn | Testcontainers |
|---|---|---|---|
| Контейнер | 1 на процесс | 1 на класс | 1 на класс |
| Миграции | 1 раз (весь процесс) | 1 раз на класс | 1 раз на класс |
| Сброс данных | удаление клона ~5 мс | TRUNCATE ~1 мс | EnsureDeleted ~200 мс |
| Параллелизм внутри класса | да | нет | да |

---

## Benchmark Runner

Инструмент для автоматического сравнительного бенчмарка трёх подходов. Прогоняет три сценария и генерирует HTML-отчёт с Chart.js графиками.

```bash
# Запуск (Docker должен быть запущен, займёт 15–30 мин)
dotnet run --project tools/BenchmarkRunner

# Открыть отчёт
start benchmark-results/report.html   # Windows
open benchmark-results/report.html    # macOS
```

### Сценарии бенчмарка

| Сценарий | Что варьируется |
|----------|-----------------|
| 1 — Влияние миграций | 16 / 66 / 116 миграций (TEST_REPEAT=10, потоков=4) |
| 2 — Масштаб тестов | TEST_REPEAT: 1, 5, 10, 20, 50 (16 миграций, потоков=4) |
| 3 — Параллелизм | потоков: 1, 2, 4, 8 (16 миграций, TEST_REPEAT=20) |

Фейковые миграции для сценария 1 генерируются и удаляются автоматически.

---

## Структура проекта

```
src/
├── FastIntegrationTests.Application/    # Домен: сущности, DTO, сервисы, исключения
├── FastIntegrationTests.Infrastructure/ # EF Core: DbContext, репозитории, миграции (16 шт.)
└── FastIntegrationTests.WebApi/         # ASP.NET Core: контроллеры, Program.cs

tests/
└── FastIntegrationTests.Tests/
    ├── Infrastructure/                  # Фикстуры, базовые классы для трёх подходов
    ├── IntegreSQL/                      # 53 теста: Products/, Orders/
    ├── Respawn/                         # 53 теста: Products/, Orders/
    └── Testcontainers/                  # 53 теста: Products/, Orders/

tools/
└── BenchmarkRunner/                     # Консольный инструмент бенчмарка
    ├── Models/                          # BenchmarkScenario, BenchmarkResult, BenchmarkReport
    ├── Runner/                          # TestRunner — запуск dotnet test через Process
    ├── Migrations/                      # MigrationManager — генерация фейковых миграций
    └── Report/                          # ReportGenerator + Chart.js HTML шаблон

benchmark-results/                       # Gitignored, создаётся при запуске бенчмарка
├── report.html
└── results.json
```
