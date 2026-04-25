# Split Test Projects — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Разнести `FastIntegrationTests.Tests` на 4 проекта: `Tests.Shared`, `Tests.IntegreSQL`, `Tests.Respawn`, `Tests.Testcontainers`.

**Architecture:** Сначала создаём Tests.Shared и перемещаем туда общий код. Затем последовательно создаём три approach-проекта — каждый раз перемещая инфраструктуру и тест-классы, проверяя сборку. В конце удаляем старый проект и обновляем tooling. Namespace-ы остаются неизменными — только физическое расположение файлов меняется.

**Tech Stack:** .NET 8, xUnit 2.9.3, Testcontainers, Respawn, MccSoft.IntegreSql.EF, PowerShell

---

## File Map

### Создать

- `tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj`
- `tests/FastIntegrationTests.Tests.Shared/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj`
- `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.IntegreSQL/xunit.runner.json`
- `tests/FastIntegrationTests.Tests.Respawn/FastIntegrationTests.Tests.Respawn.csproj`
- `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Respawn/xunit.runner.json`
- `tests/FastIntegrationTests.Tests.Testcontainers/FastIntegrationTests.Tests.Testcontainers.csproj`
- `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs`
- `tests/FastIntegrationTests.Tests.Testcontainers/xunit.runner.json`

### Переместить (git mv)

**→ Tests.Shared:**
- `tests/FastIntegrationTests.Tests/Infrastructure/Base/TestRepeat.cs`
- `tests/FastIntegrationTests.Tests/Infrastructure/WebApp/TestWebApplicationFactory.cs`

**→ Tests.IntegreSQL/Infrastructure:**
- `Infrastructure/Base/AppServiceTestBase.cs`
- `Infrastructure/Base/ComponentTestBase.cs`
- `Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`
- `Infrastructure/IntegreSQL/IntegresSqlDefaults.cs`
- `Infrastructure/IntegreSQL/IntegresSqlState.cs`

**→ Tests.IntegreSQL/ (тест-классы):**
- `IntegreSQL/Categories/` → `Categories/`
- `IntegreSQL/Customers/` → `Customers/`
- `IntegreSQL/Discounts/` → `Discounts/`
- `IntegreSQL/Orders/` → `Orders/`
- `IntegreSQL/Products/` → `Products/`
- `IntegreSQL/Reviews/` → `Reviews/`
- `IntegreSQL/Suppliers/` → `Suppliers/`

**→ Tests.Respawn/Infrastructure:**
- `Infrastructure/Base/RespawnServiceTestBase.cs`
- `Infrastructure/Base/RespawnApiTestBase.cs`
- `Infrastructure/Fixtures/RespawnFixture.cs`
- `Infrastructure/Fixtures/RespawnApiFixture.cs`

**→ Tests.Respawn/ (тест-классы):**
- `Respawn/Categories/` → `Categories/`
- `Respawn/Customers/` → `Customers/`
- `Respawn/Discounts/` → `Discounts/`
- `Respawn/Orders/` → `Orders/`
- `Respawn/Products/` → `Products/`
- `Respawn/Reviews/` → `Reviews/`
- `Respawn/Suppliers/` → `Suppliers/`

**→ Tests.Testcontainers/Infrastructure:**
- `Infrastructure/Base/ServiceTestBase.cs`
- `Infrastructure/Base/ApiTestBase.cs`
- `Infrastructure/Base/ContainerServiceTestBase.cs`
- `Infrastructure/Base/ContainerApiTestBase.cs`
- `Infrastructure/Factories/TestDbFactory.cs`
- `Infrastructure/Fixtures/ContainerFixture.cs`

**→ Tests.Testcontainers/ (тест-классы):**
- `Testcontainers/Categories/` → `Categories/`
- `Testcontainers/Customers/` → `Customers/`
- `Testcontainers/Discounts/` → `Discounts/`
- `Testcontainers/Orders/` → `Orders/`
- `Testcontainers/Products/` → `Products/`
- `Testcontainers/Reviews/` → `Reviews/`
- `Testcontainers/Suppliers/` → `Suppliers/`

### Удалить

- `tests/FastIntegrationTests.Tests/` (целиком, после опустошения)

### Изменить

- `FastIntegrationTests.slnx` — 4 раза (добавить Shared, три approach-проекта; удалить старый)
- `run-integresql.ps1`
- `run-respawn.ps1`
- `run-testcontainers.ps1`
- `CLAUDE.md`
- `tools/BenchmarkRunner/Runner/TestRunner.cs`

---

## Task 1: Tests.Shared

**Files:**
- Create: `tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj`
- Create: `tests/FastIntegrationTests.Tests.Shared/GlobalUsings.cs`
- Move: `Infrastructure/Base/TestRepeat.cs` → Shared
- Move: `Infrastructure/WebApp/TestWebApplicationFactory.cs` → Shared
- Modify: `FastIntegrationTests.slnx`
- Modify: `tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj`

- [ ] Создать директорию и csproj:

```bash
mkdir -p tests/FastIntegrationTests.Tests.Shared/Infrastructure/Base
mkdir -p tests/FastIntegrationTests.Tests.Shared/Infrastructure/WebApp
```

```xml
<!-- tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj -->
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="xunit" Version="2.9.3" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" Version="8.0.15" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FastIntegrationTests.Application\FastIntegrationTests.Application.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.Infrastructure\FastIntegrationTests.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.WebApi\FastIntegrationTests.WebApi.csproj" />
  </ItemGroup>

</Project>
```

- [ ] Создать `tests/FastIntegrationTests.Tests.Shared/GlobalUsings.cs`:

```csharp
global using FastIntegrationTests.Infrastructure.Data;
global using Microsoft.EntityFrameworkCore;
global using Xunit;
```

- [ ] Переместить файлы в Shared:

```bash
git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/TestRepeat.cs" \
       "tests/FastIntegrationTests.Tests.Shared/Infrastructure/Base/TestRepeat.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/WebApp/TestWebApplicationFactory.cs" \
       "tests/FastIntegrationTests.Tests.Shared/Infrastructure/WebApp/TestWebApplicationFactory.cs"
```

- [ ] Добавить Shared в `FastIntegrationTests.slnx`:

```xml
<!-- Добавить в <Folder Name="/tests/"> -->
<Project Path="tests/FastIntegrationTests.Tests.Shared/FastIntegrationTests.Tests.Shared.csproj" />
```

- [ ] Добавить project reference на Shared в `tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj`:

```xml
<!-- Добавить в существующий <ItemGroup> с ProjectReference -->
<ProjectReference Include="..\FastIntegrationTests.Tests.Shared\FastIntegrationTests.Tests.Shared.csproj" />
```

- [ ] Проверить сборку:

```bash
dotnet build --nologo -v q
```

Ожидается: `Build succeeded.`

- [ ] Commit:

```bash
git add tests/FastIntegrationTests.Tests.Shared/
git add FastIntegrationTests.slnx
git add tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj
git commit -m "refactor(tests): извлечь Tests.Shared из монолитного проекта"
```

---

## Task 2: Tests.IntegreSQL

**Files:**
- Create: `tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj`
- Create: `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs`
- Create: `tests/FastIntegrationTests.Tests.IntegreSQL/xunit.runner.json`
- Move: 5 инфра-файлов IntegreSQL
- Move: 7 директорий с тест-классами (IntegreSQL/*)
- Modify: `FastIntegrationTests.slnx`

- [ ] Создать директории:

```bash
mkdir -p tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base
mkdir -p tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL
```

- [ ] Создать `tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="MccSoft.IntegreSql.EF" Version="0.12.2" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FastIntegrationTests.Application\FastIntegrationTests.Application.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.Infrastructure\FastIntegrationTests.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.WebApi\FastIntegrationTests.WebApi.csproj" />
    <ProjectReference Include="..\FastIntegrationTests.Tests.Shared\FastIntegrationTests.Tests.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="xunit.runner.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] Создать `tests/FastIntegrationTests.Tests.IntegreSQL/GlobalUsings.cs`:

```csharp
global using System.Net;
global using System.Net.Http.Json;
global using FastIntegrationTests.Application.DTOs;
global using FastIntegrationTests.Application.Entities;
global using FastIntegrationTests.Application.Enums;
global using FastIntegrationTests.Application.Exceptions;
global using FastIntegrationTests.Application.Interfaces;
global using FastIntegrationTests.Application.Services;
global using FastIntegrationTests.Infrastructure.Data;
global using FastIntegrationTests.Infrastructure.Repositories;
global using FastIntegrationTests.Tests.Infrastructure.Base;
global using FastIntegrationTests.Tests.Infrastructure.WebApp;
global using Microsoft.EntityFrameworkCore;
global using Xunit;
```

- [ ] Создать `tests/FastIntegrationTests.Tests.IntegreSQL/xunit.runner.json`:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 12
}
```

- [ ] Переместить инфраструктуру:

```bash
git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/AppServiceTestBase.cs" \
       "tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/AppServiceTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/ComponentTestBase.cs" \
       "tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/Base/ComponentTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs" \
       "tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/IntegresSqlDefaults.cs" \
       "tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlDefaults.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/IntegreSQL/IntegresSqlState.cs" \
       "tests/FastIntegrationTests.Tests.IntegreSQL/Infrastructure/IntegreSQL/IntegresSqlState.cs"
```

- [ ] Переместить тест-классы (7 директорий):

```bash
for entity in Categories Customers Discounts Orders Products Reviews Suppliers; do
  git mv "tests/FastIntegrationTests.Tests/IntegreSQL/$entity" \
         "tests/FastIntegrationTests.Tests.IntegreSQL/$entity"
done
```

- [ ] Добавить в `FastIntegrationTests.slnx`:

```xml
<Project Path="tests/FastIntegrationTests.Tests.IntegreSQL/FastIntegrationTests.Tests.IntegreSQL.csproj" />
```

- [ ] Проверить сборку нового проекта:

```bash
dotnet build tests/FastIntegrationTests.Tests.IntegreSQL --nologo -v q
```

Ожидается: `Build succeeded.`

- [ ] Commit:

```bash
git add tests/FastIntegrationTests.Tests.IntegreSQL/
git add FastIntegrationTests.slnx
git add tests/FastIntegrationTests.Tests/
git commit -m "refactor(tests): создать Tests.IntegreSQL, перенести инфраструктуру и тесты"
```

---

## Task 3: Tests.Respawn

**Files:**
- Create: `tests/FastIntegrationTests.Tests.Respawn/FastIntegrationTests.Tests.Respawn.csproj`
- Create: `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs`
- Create: `tests/FastIntegrationTests.Tests.Respawn/xunit.runner.json`
- Move: 4 инфра-файла Respawn
- Move: 7 директорий с тест-классами (Respawn/*)

- [ ] Создать директории:

```bash
mkdir -p tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Base
mkdir -p tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures
```

- [ ] Создать `tests/FastIntegrationTests.Tests.Respawn/FastIntegrationTests.Tests.Respawn.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Respawn" Version="7.0.0" />
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FastIntegrationTests.Application\FastIntegrationTests.Application.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.Infrastructure\FastIntegrationTests.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.WebApi\FastIntegrationTests.WebApi.csproj" />
    <ProjectReference Include="..\FastIntegrationTests.Tests.Shared\FastIntegrationTests.Tests.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="xunit.runner.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] Создать `tests/FastIntegrationTests.Tests.Respawn/GlobalUsings.cs`:

```csharp
global using System.Net;
global using System.Net.Http.Json;
global using FastIntegrationTests.Application.DTOs;
global using FastIntegrationTests.Application.Entities;
global using FastIntegrationTests.Application.Enums;
global using FastIntegrationTests.Application.Exceptions;
global using FastIntegrationTests.Application.Interfaces;
global using FastIntegrationTests.Application.Services;
global using FastIntegrationTests.Infrastructure.Data;
global using FastIntegrationTests.Infrastructure.Repositories;
global using FastIntegrationTests.Tests.Infrastructure.Base;
global using FastIntegrationTests.Tests.Infrastructure.Fixtures;
global using FastIntegrationTests.Tests.Infrastructure.WebApp;
global using Microsoft.EntityFrameworkCore;
global using Xunit;
```

- [ ] Создать `tests/FastIntegrationTests.Tests.Respawn/xunit.runner.json`:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4
}
```

- [ ] Переместить инфраструктуру:

```bash
git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/RespawnServiceTestBase.cs" \
       "tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Base/RespawnServiceTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/RespawnApiTestBase.cs" \
       "tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Base/RespawnApiTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Fixtures/RespawnFixture.cs" \
       "tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnFixture.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Fixtures/RespawnApiFixture.cs" \
       "tests/FastIntegrationTests.Tests.Respawn/Infrastructure/Fixtures/RespawnApiFixture.cs"
```

- [ ] Переместить тест-классы (7 директорий):

```bash
for entity in Categories Customers Discounts Orders Products Reviews Suppliers; do
  git mv "tests/FastIntegrationTests.Tests/Respawn/$entity" \
         "tests/FastIntegrationTests.Tests.Respawn/$entity"
done
```

- [ ] Добавить в `FastIntegrationTests.slnx`:

```xml
<Project Path="tests/FastIntegrationTests.Tests.Respawn/FastIntegrationTests.Tests.Respawn.csproj" />
```

- [ ] Проверить сборку:

```bash
dotnet build tests/FastIntegrationTests.Tests.Respawn --nologo -v q
```

Ожидается: `Build succeeded.`

- [ ] Commit:

```bash
git add tests/FastIntegrationTests.Tests.Respawn/
git add FastIntegrationTests.slnx
git add tests/FastIntegrationTests.Tests/
git commit -m "refactor(tests): создать Tests.Respawn, перенести инфраструктуру и тесты"
```

---

## Task 4: Tests.Testcontainers

**Files:**
- Create: `tests/FastIntegrationTests.Tests.Testcontainers/FastIntegrationTests.Tests.Testcontainers.csproj`
- Create: `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs`
- Create: `tests/FastIntegrationTests.Tests.Testcontainers/xunit.runner.json`
- Move: 6 инфра-файлов Testcontainers
- Move: 7 директорий с тест-классами (Testcontainers/*)

- [ ] Создать директории:

```bash
mkdir -p tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base
mkdir -p tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories
mkdir -p tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures
```

- [ ] Создать `tests/FastIntegrationTests.Tests.Testcontainers/FastIntegrationTests.Tests.Testcontainers.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Testcontainers.PostgreSql" Version="4.4.0" />
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="coverlet.collector" Version="6.0.4">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\FastIntegrationTests.Application\FastIntegrationTests.Application.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.Infrastructure\FastIntegrationTests.Infrastructure.csproj" />
    <ProjectReference Include="..\..\src\FastIntegrationTests.WebApi\FastIntegrationTests.WebApi.csproj" />
    <ProjectReference Include="..\FastIntegrationTests.Tests.Shared\FastIntegrationTests.Tests.Shared.csproj" />
  </ItemGroup>

  <ItemGroup>
    <None Update="xunit.runner.json">
      <CopyToOutputDirectory>Always</CopyToOutputDirectory>
    </None>
  </ItemGroup>

</Project>
```

- [ ] Создать `tests/FastIntegrationTests.Tests.Testcontainers/GlobalUsings.cs`:

```csharp
global using System.Net;
global using System.Net.Http.Json;
global using FastIntegrationTests.Application.DTOs;
global using FastIntegrationTests.Application.Entities;
global using FastIntegrationTests.Application.Enums;
global using FastIntegrationTests.Application.Exceptions;
global using FastIntegrationTests.Application.Interfaces;
global using FastIntegrationTests.Application.Services;
global using FastIntegrationTests.Infrastructure.Data;
global using FastIntegrationTests.Infrastructure.Repositories;
global using FastIntegrationTests.Tests.Infrastructure.Base;
global using FastIntegrationTests.Tests.Infrastructure.Factories;
global using FastIntegrationTests.Tests.Infrastructure.Fixtures;
global using FastIntegrationTests.Tests.Infrastructure.WebApp;
global using Microsoft.EntityFrameworkCore;
global using Xunit;
```

- [ ] Создать `tests/FastIntegrationTests.Tests.Testcontainers/xunit.runner.json`:

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 8
}
```

- [ ] Переместить инфраструктуру:

```bash
git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/ServiceTestBase.cs" \
       "tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ServiceTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/ApiTestBase.cs" \
       "tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ApiTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/ContainerServiceTestBase.cs" \
       "tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ContainerServiceTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Base/ContainerApiTestBase.cs" \
       "tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Base/ContainerApiTestBase.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Factories/TestDbFactory.cs" \
       "tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Factories/TestDbFactory.cs"

git mv "tests/FastIntegrationTests.Tests/Infrastructure/Fixtures/ContainerFixture.cs" \
       "tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs"
```

- [ ] Переместить тест-классы (7 директорий):

```bash
for entity in Categories Customers Discounts Orders Products Reviews Suppliers; do
  git mv "tests/FastIntegrationTests.Tests/Testcontainers/$entity" \
         "tests/FastIntegrationTests.Tests.Testcontainers/$entity"
done
```

- [ ] Добавить в `FastIntegrationTests.slnx`:

```xml
<Project Path="tests/FastIntegrationTests.Tests.Testcontainers/FastIntegrationTests.Tests.Testcontainers.csproj" />
```

- [ ] Проверить сборку:

```bash
dotnet build tests/FastIntegrationTests.Tests.Testcontainers --nologo -v q
```

Ожидается: `Build succeeded.`

- [ ] Commit:

```bash
git add tests/FastIntegrationTests.Tests.Testcontainers/
git add FastIntegrationTests.slnx
git add tests/FastIntegrationTests.Tests/
git commit -m "refactor(tests): создать Tests.Testcontainers, перенести инфраструктуру и тесты"
```

---

## Task 5: Удалить FastIntegrationTests.Tests

**Files:**
- Delete: `tests/FastIntegrationTests.Tests/` (целиком)
- Modify: `FastIntegrationTests.slnx`

- [ ] Убедиться, что старый проект пустой (осталось только Infrastructure/Base/ без файлов и корневые файлы):

```bash
find tests/FastIntegrationTests.Tests -name "*.cs" | sort
```

Ожидается: только `GlobalUsings.cs` (если ещё не удалён).

- [ ] Удалить директорию из git:

```bash
git rm -r tests/FastIntegrationTests.Tests/
```

- [ ] Убрать из `FastIntegrationTests.slnx` строку:

```xml
<!-- Удалить: -->
<Project Path="tests/FastIntegrationTests.Tests/FastIntegrationTests.Tests.csproj" />
```

- [ ] Проверить сборку всего solution:

```bash
dotnet build --nologo -v q
```

Ожидается: `Build succeeded.`

- [ ] Commit:

```bash
git add FastIntegrationTests.slnx
git commit -m "refactor(tests): удалить монолитный FastIntegrationTests.Tests"
```

---

## Task 6: Обновить tooling

**Files:**
- Modify: `run-integresql.ps1`
- Modify: `run-respawn.ps1`
- Modify: `run-testcontainers.ps1`
- Modify: `CLAUDE.md`
- Modify: `tools/BenchmarkRunner/Runner/TestRunner.cs`

- [ ] Обновить `run-integresql.ps1`:

```powershell
param(
    [int]$Repeat = 5,
    [int]$Threads = 4
)

$env:TEST_REPEAT = $Repeat
$start = Get-Date

Write-Host "IntegreSQL | repeat=$Repeat | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
```

- [ ] Обновить `run-respawn.ps1`:

```powershell
param(
    [int]$Repeat = 5,
    [int]$Threads = 4
)

$env:TEST_REPEAT = $Repeat
$start = Get-Date

Write-Host "Respawn | repeat=$Repeat | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.Respawn `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
```

- [ ] Обновить `run-testcontainers.ps1`:

```powershell
param(
    [int]$Repeat = 5,
    [int]$Threads = 4
)

$env:TEST_REPEAT = $Repeat
$start = Get-Date

Write-Host "Testcontainers | repeat=$Repeat | threads=$Threads"
dotnet test tests/FastIntegrationTests.Tests.Testcontainers `
    -- xUnit.MaxParallelThreads=$Threads

$elapsed = (Get-Date) - $start
Write-Host "`nВремя выполнения: $($elapsed.ToString('mm\:ss\.fff'))"
```

- [ ] Обновить `tools/BenchmarkRunner/Runner/TestRunner.cs` — метод `Build()`:

```csharp
/// <summary>Собирает все тестовые проекты. Вызывается перед первым Run и после изменения миграций.</summary>
public void Build()
{
    var projects = new[]
    {
        "tests/FastIntegrationTests.Tests.IntegreSQL",
        "tests/FastIntegrationTests.Tests.Respawn",
        "tests/FastIntegrationTests.Tests.Testcontainers",
    };

    foreach (var project in projects)
    {
        Console.Write($"\n[BUILD] {project} ... ");
        var (output, code) = RunCapture("dotnet", $"build {project} --nologo -v minimal");
        if (code != 0)
        {
            Console.WriteLine("FAIL");
            var buildScenario = new BenchmarkScenario("build", "build", 0, 0, 0);
            LogFailure(buildScenario, output);
            throw new Exception($"Build failed: {project} (exit code {code})");
        }
        Console.WriteLine("OK");
    }
}
```

- [ ] Обновить `tools/BenchmarkRunner/Runner/TestRunner.cs` — метод `RunTest()`:

Заменить:
```csharp
private (double Elapsed, bool Success, string Output) RunTest(BenchmarkScenario scenario)
{
    var filter = $"FullyQualifiedName~Tests.{scenario.Approach}";
    var args =
        $"test tests/FastIntegrationTests.Tests" +
        $" --filter \"{filter}\"" +
        $" --no-build" +
        $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";
```

На:
```csharp
private (double Elapsed, bool Success, string Output) RunTest(BenchmarkScenario scenario)
{
    var args =
        $"test tests/FastIntegrationTests.Tests.{scenario.Approach}" +
        $" --no-build" +
        $" -- xUnit.MaxParallelThreads={scenario.MaxParallelThreads}";
```

- [ ] Обновить CLAUDE.md — раздел «Интеграционные тесты»:

Заменить блок команд запуска подходов:

```bash
# Запустить один подход
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL
dotnet test tests/FastIntegrationTests.Tests.Respawn
dotnet test tests/FastIntegrationTests.Tests.Testcontainers

# С повторами — сравнение производительности (bash)
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests.IntegreSQL
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests.Respawn
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests.Testcontainers

# PowerShell
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests.IntegreSQL
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests.Respawn
$env:TEST_REPEAT=19; dotnet test tests/FastIntegrationTests.Tests.Testcontainers

# Переопределить количество потоков прямо из CLI
TEST_REPEAT=19 dotnet test tests/FastIntegrationTests.Tests.IntegreSQL -- xUnit.MaxParallelThreads=8

# Запустить тесты отдельного класса
dotnet test tests/FastIntegrationTests.Tests.IntegreSQL --filter "FullyQualifiedName~ProductServiceCrTests"
dotnet test tests/FastIntegrationTests.Tests.Respawn --filter "FullyQualifiedName~OrdersApiUdRespawnTests"
dotnet test tests/FastIntegrationTests.Tests.Testcontainers --filter "FullyQualifiedName~CategoryServiceUdContainerTests"
```

Убрать старые команды с `--filter "FullyQualifiedName~Tests.IntegreSQL"` и аналогичные.

- [ ] Проверить сборку BenchmarkRunner:

```bash
dotnet build tools/BenchmarkRunner --nologo -v q
```

Ожидается: `Build succeeded.`

- [ ] Commit:

```bash
git add run-integresql.ps1 run-respawn.ps1 run-testcontainers.ps1
git add CLAUDE.md
git add tools/BenchmarkRunner/Runner/TestRunner.cs
git commit -m "refactor(tests): обновить скрипты, CLAUDE.md и BenchmarkRunner под новые проекты"
```
