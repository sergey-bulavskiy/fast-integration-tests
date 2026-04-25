# Разбивка тестов на отдельные проекты — Design Spec

**Дата:** 2026-04-25  
**Цель:** Разнести единый `FastIntegrationTests.Tests` на четыре проекта — по одному на подход плюс общий Shared — чтобы каждый подход запускался отдельной командой без `--filter`.

---

## Контекст

Сейчас все три подхода (IntegreSQL, Respawn, Testcontainers) живут в одном проекте `tests/FastIntegrationTests.Tests`. Запуск конкретного подхода требует `--filter "FullyQualifiedName~Tests.IntegreSQL"`. После разбивки достаточно `dotnet test tests/FastIntegrationTests.Tests.IntegreSQL`.

Это первый из двух рефакторингов. Второй (абстрактные базовые классы для тест-методов) — отдельный spec и план, выполняется после.

---

## Раздел 1 — Структура проектов

### Tests.Shared (class library, не test project)

```
tests/FastIntegrationTests.Tests.Shared/
  Infrastructure/
    WebApp/
      TestWebApplicationFactory.cs
    TestRepeat.cs
  GlobalUsings.cs
  FastIntegrationTests.Tests.Shared.csproj
```

Используется всеми тремя подходами. Не содержит тест-классов и не является test runner-ом. Каждый из трёх approach-проектов может дополнительно иметь свой `GlobalUsings.cs` с подход-специфичными using-ами (например, `using MccSoft.IntegreSql.EF` в Tests.IntegreSQL).

### Tests.IntegreSQL (test project)

```
tests/FastIntegrationTests.Tests.IntegreSQL/
  Infrastructure/
    Base/
      AppServiceTestBase.cs
      ComponentTestBase.cs
    IntegreSQL/
      IntegresSqlContainerManager.cs
      IntegresSqlDefaults.cs
      IntegresSqlState.cs
  Categories/
  Customers/
  Discounts/
  Orders/
  Products/
  Reviews/
  Suppliers/
  xunit.runner.json
  FastIntegrationTests.Tests.IntegreSQL.csproj
```

### Tests.Respawn (test project)

```
tests/FastIntegrationTests.Tests.Respawn/
  Infrastructure/
    Base/
      RespawnServiceTestBase.cs
      RespawnApiTestBase.cs
    Fixtures/
      RespawnFixture.cs
      RespawnApiFixture.cs
  Categories/
  Customers/
  Discounts/
  Orders/
  Products/
  Reviews/
  Suppliers/
  xunit.runner.json
  FastIntegrationTests.Tests.Respawn.csproj
```

### Tests.Testcontainers (test project)

```
tests/FastIntegrationTests.Tests.Testcontainers/
  Infrastructure/
    Base/
      ServiceTestBase.cs
      ApiTestBase.cs
      ContainerServiceTestBase.cs
      ContainerApiTestBase.cs
    Factories/
      TestDbFactory.cs
    Fixtures/
      ContainerFixture.cs
  Categories/
  Customers/
  Discounts/
  Orders/
  Products/
  Reviews/
  Suppliers/
  xunit.runner.json
  FastIntegrationTests.Tests.Testcontainers.csproj
```

**Ссылки:** каждый из трёх test project-ов → `Tests.Shared` + `Application` + `Infrastructure` + `WebApi`.

---

## Раздел 2 — NuGet-зависимости

Версии берутся из текущего `FastIntegrationTests.Tests.csproj` без изменений.

### Tests.Shared

```xml
<PackageReference Include="xunit" />
<PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
```

### Tests.IntegreSQL

```xml
<PackageReference Include="MccSoft.IntegreSql.EF" />
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="coverlet.collector" />
```

### Tests.Respawn

```xml
<PackageReference Include="Respawn" />
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="coverlet.collector" />
```

### Tests.Testcontainers

```xml
<PackageReference Include="Testcontainers.PostgreSql" />
<PackageReference Include="Microsoft.NET.Test.Sdk" />
<PackageReference Include="xunit.runner.visualstudio" />
<PackageReference Include="coverlet.collector" />
```

---

## Раздел 3 — xunit.runner.json на проект

### Tests.IntegreSQL
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 12
}
```

### Tests.Respawn
Тесты внутри класса последовательные (общая БД), классы параллельны между собой:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

### Tests.Testcontainers
Каждый тест создаёт свою БД, классы параллельны:
```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 8
}
```

Значения для Respawn и Testcontainers — стартовые, тюнятся через `-Repeat` и `-Threads` в скриптах.

---

## Раздел 4 — Скрипты и CLAUDE.md

### PowerShell-скрипты

`run-integresql.ps1`, `run-respawn.ps1`, `run-testcontainers.ps1` — обновить путь к проекту. Флаг `--filter` больше не нужен.

```powershell
# Было:
dotnet test tests/FastIntegrationTests.Tests --filter "FullyQualifiedName~Tests.IntegreSQL" ...

# Станет:
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL ...
```

### CLAUDE.md

Обновить раздел «Интеграционные тесты»:

```bash
# Запустить один подход
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL
dotnet test tests/FastIntegrationTests.Tests.Respawn
dotnet test tests/FastIntegrationTests.Tests.Testcontainers

# Все подходы сразу
dotnet test

# С повторами
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests.IntegreSQL
```

Убрать примеры с `--filter "FullyQualifiedName~Tests.*"` — они становятся неактуальными.

### BenchmarkRunner

В `tools/BenchmarkRunner/Runner/TestRunner.cs` обновить пути к проектам для каждого из трёх подходов.

### Solution-файл

Убрать `FastIntegrationTests.Tests`, добавить четыре новых проекта в папку `/tests/`.

---

## Раздел 5 — Порядок миграции

Каждый шаг заканчивается коммитом. Сборка (`dotnet build`) проверяется после каждого шага 1–5.

1. **Создать Tests.Shared** — перенести `TestWebApplicationFactory`, `TestRepeat`, `GlobalUsings`. Добавить project reference из старого `FastIntegrationTests.Tests` на `Tests.Shared`. Проверить `dotnet build`.

2. **Создать Tests.IntegreSQL** — перенести IntegreSQL-инфраструктуру (5 файлов) и 28 тест-классов. Добавить в solution. Проверить `dotnet build`. Удалить IntegreSQL-файлы из старого проекта.

3. **Создать Tests.Respawn** — перенести Respawn-инфраструктуру (4 файла) и 28 тест-классов. Проверить `dotnet build`. Удалить из старого проекта.

4. **Создать Tests.Testcontainers** — перенести Testcontainers-инфраструктуру (6 файлов) и 28 тест-классов. Проверить `dotnet build`. Удалить из старого проекта.

5. **Удалить FastIntegrationTests.Tests** — к этому моменту он пустой. Убрать из solution.

6. **Обновить** PowerShell-скрипты, CLAUDE.md, BenchmarkRunner — отдельный коммит.
