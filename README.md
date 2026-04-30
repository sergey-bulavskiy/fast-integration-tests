# FastIntegrationTests

Учебный проект: REST API магазина на .NET 8 с полным набором интеграционных тестов на реальной PostgreSQL.

Демонстрирует и сравнивает три подхода к изоляции тестов в паттерне **database-per-test**:

| Подход | Изоляция | Время на тест* |
|--------|----------|----------------|
| **IntegreSQL** | клон шаблонной БД на каждый тест | ~50 мс |
| **Respawn** | DELETE по FK-порядку между тестами | ~20 мс |
| **Testcontainers** | новая БД + MigrateAsync на каждый тест | 240–1700 мс** |

\* Wall-clock на тест из реального бенчмарка (classScale=12, 2676 тестов, 8 потоков).
\** Зависит от числа миграций: 17 миграций → 240 мс, 117 миграций → 1700 мс.

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

### Categories

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/categories` | Список всех категорий |
| GET | `/api/categories/{id}` | Категория по ID |
| POST | `/api/categories` | Создать категорию |
| PUT | `/api/categories/{id}` | Обновить категорию |
| DELETE | `/api/categories/{id}` | Удалить категорию |

### Customers

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/customers` | Список всех покупателей |
| GET | `/api/customers/{id}` | Покупатель по ID |
| POST | `/api/customers` | Создать покупателя |
| PUT | `/api/customers/{id}` | Обновить покупателя |
| DELETE | `/api/customers/{id}` | Удалить покупателя |
| POST | `/api/customers/{id}/ban` | Заблокировать |
| POST | `/api/customers/{id}/activate` | Активировать |
| POST | `/api/customers/{id}/deactivate` | Деактивировать |

### Suppliers

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/suppliers` | Список всех поставщиков |
| GET | `/api/suppliers/{id}` | Поставщик по ID |
| POST | `/api/suppliers` | Создать поставщика |
| PUT | `/api/suppliers/{id}` | Обновить поставщика |
| DELETE | `/api/suppliers/{id}` | Удалить поставщика |
| POST | `/api/suppliers/{id}/activate` | Активировать |
| POST | `/api/suppliers/{id}/deactivate` | Деактивировать |

### Reviews

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/reviews` | Список всех отзывов |
| GET | `/api/reviews/{id}` | Отзыв по ID |
| POST | `/api/reviews` | Создать отзыв (рейтинг 1–5) |
| DELETE | `/api/reviews/{id}` | Удалить отзыв |
| POST | `/api/reviews/{id}/approve` | Одобрить (Pending → Approved) |
| POST | `/api/reviews/{id}/reject` | Отклонить (Pending → Rejected) |

### Discounts

| Метод | Endpoint | Описание |
|-------|----------|----------|
| GET | `/api/discounts` | Список всех скидок |
| GET | `/api/discounts/{id}` | Скидка по ID |
| POST | `/api/discounts` | Создать скидку (процент 1–100) |
| PUT | `/api/discounts/{id}` | Обновить скидку |
| DELETE | `/api/discounts/{id}` | Удалить скидку |
| POST | `/api/discounts/{id}/activate` | Активировать |
| POST | `/api/discounts/{id}/deactivate` | Деактивировать |

---

## Запуск тестов

Docker должен быть запущен. Контейнеры поднимаются автоматически через Testcontainers.

```bash
# Все тесты (три подхода вместе)
dotnet test

# Один подход
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL
dotnet test tests/FastIntegrationTests.Tests.Respawn
dotnet test tests/FastIntegrationTests.Tests.Testcontainers

# Отдельный тест-класс (примеры)
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~ProductServiceTests"
dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~OrdersApiContainerTests"
dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~CategoryServiceRespawnTests"
```

### PowerShell скрипты (с замером времени)

```powershell
# По умолчанию: 4 потока
.\run-integresql.ps1
.\run-respawn.ps1
.\run-testcontainers.ps1

# Переопределить параметры
.\run-integresql.ps1 -Threads 8
```

### Сравнение подходов

| | IntegreSQL | Respawn | Testcontainers |
|---|---|---|---|
| Контейнер | 1 на процесс (PG + IntegreSQL) | 1 на процесс | 1 на класс |
| Миграции | 1 раз (весь процесс) | 1 раз на класс | 1 раз на тест |
| Сброс между тестами | клон шаблона из пула ~50 мс | DELETE по FK-порядку ~20 мс | новая БД + MigrateAsync ~240–1700 мс |
| Параллелизм внутри класса | да | нет | да |

---

## Benchmark Runner

Инструмент для автоматического сравнительного бенчмарка трёх подходов. Прогоняет три сценария и генерирует HTML-отчёт с Chart.js графиками.

```bash
# Полный прогон с дефолтными параметрами (Docker должен быть запущен, ~1–2 часа)
dotnet run --project tools/BenchmarkRunner

# Переопределить дефолтные потоки и масштаб классов
dotnet run --project tools/BenchmarkRunner -- --threads 4 --scale 12

# Открыть отчёт (runner выводит точный путь после завершения)
start benchmark-results/report-20260425-143022.html   # Windows (пример)
open benchmark-results/report-20260425-143022.html    # macOS (пример)
```

| Аргумент | По умолчанию | Где применяется |
|----------|-------------|-----------------|
| `--scale N` / `-s N` | 12 | Сценарии 1 и 3 (масштаб классов) |
| `--threads N` / `-t N` | 8 | Сценарии 1 и 2 (где потоки не варьируются) |

### Сценарии бенчмарка

| Сценарий | Что варьируется | Что фиксируется |
|----------|-----------------|-----------------|
| 1 — Влияние миграций | 17 / 42 / 67 / 92 / 117 миграций | `--scale`, `--threads` |
| 2 — Масштаб тестов | ClassScale: 1, 5, 10, 20, 50 | `--threads`, 117 миграций |
| 3 — Параллелизм | потоков: 1, 2, 4, 8 | `--scale`, 117 миграций |

Перед Сценарием 1 выполняется warmup-прогон (результат не сохраняется). Benchmark-миграции скрываются и восстанавливаются автоматически в Сценарии 1.

---

## Структура проекта

```
src/
├── FastIntegrationTests.Application/    # Домен: сущности, DTO, сервисы, исключения
├── FastIntegrationTests.Infrastructure/ # EF Core: DbContext, репозитории, миграции (117 шт.: 17 рабочих + 100 benchmark)
└── FastIntegrationTests.WebApi/         # ASP.NET Core: контроллеры, Program.cs

tests/
├── FastIntegrationTests.Tests.Shared/   # Общая инфраструктура: TestWebApplicationFactory
├── FastIntegrationTests.Tests.IntegreSQL/   # ~195 тестов: Categories/, Customers/, Discounts/,
│                                            #              Orders/, Products/, Reviews/, Suppliers/
├── FastIntegrationTests.Tests.Respawn/      # ~195 тестов: те же 7 папок
└── FastIntegrationTests.Tests.Testcontainers/  # ~195 тестов: те же 7 папок

tools/
└── BenchmarkRunner/                     # Консольный инструмент бенчмарка
    ├── Models/                          # BenchmarkScenario, BenchmarkResult, BenchmarkReport
    ├── Runner/                          # TestRunner — запуск dotnet test через Process
    ├── Migrations/                      # MigrationManager — скрытие/восстановление benchmark-миграций для Сценария 1
    └── Report/                          # ReportGenerator + Chart.js HTML шаблон

benchmark-results/                       # Gitignored, создаётся при запуске бенчмарка
├── report-<timestamp>.html              # Точный путь runner выводит после завершения
└── results-<timestamp>.json
```
