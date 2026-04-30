# Cooldown между прогонами `dotnet test` в BenchmarkRunner

## Контекст и мотивация

В `2026-04-30-ryuk-pauses-design.md` были добавлены две паузы по 10с в
`IntegresSqlContainerManager` и `RespawnContainerManager` — до и после
`StartAsync` PostgreSQL-контейнера. Они защищали от двух разных гонок:

- **Пауза ДО `StartAsync`** — защита от хвоста асинхронной зачистки
  Docker daemon (iptables NAT-правила, освобождение IP, hostport release)
  после контейнеров **предыдущего процесса** `dotnet test`.
- **Пауза ПОСЛЕ `StartAsync`** — защита от гонки **внутри текущего
  процесса**: `StartAsync` вернулся, но NAT-правила на хосте дописываются
  ещё несколько сотен миллисекунд. Первый коннект из теста может уйти
  до того, как правило вступило в силу, и поймать TCP RST.

Это работает, но архитектурно неаккуратно: первая пауза — это **гигиена
между процессами**, а живёт она в инфраструктуре теста. Каждый одиночный
запуск `dotnet test` (через `run-integresql.ps1`, IDE Test Explorer,
вручную) платит 20с холодного старта, хотя предыдущего процесса с
контейнерами не было — гигиена не нужна.

Цель: вынести **первую** паузу из тест-инфры в `BenchmarkRunner`. Вторую
оставить как есть (runner о ней ничего не знает).

## Что меняем

### `tools/BenchmarkRunner/Runner/TestRunner.cs`

Добавить параметр cooldown секунд — пауза перед запуском `dotnet test`,
кроме самого первого вызова в процессе. Примерно так:

```csharp
public class TestRunner
{
    private readonly TimeSpan _cooldown;
    private bool _firstRun = true;
    // ...

    public TestRunner(string repoRoot, TimeSpan timeout, TimeSpan cooldown)
    {
        // ...
        _cooldown = cooldown;
    }

    public BenchmarkResult Run(BenchmarkScenario scenario)
    {
        ApplyCooldown();
        // ... существующая логика ...
    }

    public BenchmarkResult Warmup(BenchmarkScenario scenario)
    {
        ApplyCooldown();
        // ... существующая логика ...
    }

    private void ApplyCooldown()
    {
        if (_firstRun)
        {
            _firstRun = false;
            return;
        }
        if (_cooldown > TimeSpan.Zero)
            Thread.Sleep(_cooldown);
    }
}
```

Cooldown применяется **только перед `Run` и `Warmup`** — то есть именно
перед запуском нового процесса `dotnet test`. Не применяется к
`runner.Build()` (там процесс другой и зачистки контейнеров нет).

Самый первый прогон (warmup IntegreSQL) обходится без cooldown — перед
ним нет «хвоста» от предыдущего dotnet test в том же запуске runner.
До warmup есть только начальная сборка проектов.

### `tools/BenchmarkRunner/Program.cs`

Добавить парсинг `--cooldown N` / `-c N` с дефолтом 8 секунд:

```csharp
int cooldownSeconds = 8;
// ... в цикле парсинга args:
if (args[i] is "--cooldown" or "-c" && int.TryParse(args[i + 1], out var c) && c >= 0)
    cooldownSeconds = c;

var runner = new TestRunner(repoRoot, TimeSpan.FromMinutes(TimeoutMinutes),
                            TimeSpan.FromSeconds(cooldownSeconds));
```

Вывод конфига дополняется: `Config: threads=12, scale=12, cooldown=8s, timeout=120m`.

### `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`

Убрать **первую** `await Task.Delay(TimeSpan.FromSeconds(10))` (та, что
ДО создания `network` и `pgContainer`). Вместе с ней убирается
сопровождающий 7-строчный комментарий про iptables/NAT/bridge — он
больше не релевантен на этом уровне (объяснение переезжает в runner).

**Вторая** `Task.Delay(10s)` после `integreSqlContainer.StartAsync()`
**остаётся как есть**, с её комментарием. Её обязанность — закрыть
in-process гонку между «контейнер стартовал» и «NAT-правила хоста готовы».
Runner про эту гонку ничего не знает и заменить её не может.

### `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs`

То же самое: убрать первую `Task.Delay(10s)` ДО `StartAsync`, вторую
оставить.

### `tests/FastIntegrationTests.Tests.Testcontainers/Infrastructure/Fixtures/ContainerFixture.cs`

Не трогаем. Этот фикстуроконтейнер пересоздаётся между тест-классами
**внутри одного процесса** — runner cooldown тут не помогает. In-process
паузы там нужны и должны остаться.

### `CLAUDE.md`

Добавить в секцию про BenchmarkRunner аргумент `--cooldown`:

| Аргумент | По умолчанию | Применяется в |
|---|---|---|
| `--scale N` / `-s N` | 12 | Сценарии 1 и 3 |
| `--threads N` / `-t N` | 8 | Сценарии 1 и 2 |
| `--cooldown N` / `-c N` | 8 | Все сценарии (пауза перед каждым `dotnet test`, кроме первого) |

И коротко в README.md в той же таблице, если там тоже есть аргументы.

## Дефолт 8 секунд — почему

Наблюдения:
- Реальная задержка cleanup в Docker на здоровом хосте: 1–3 секунды.
- WSL2 / Docker Desktop под нагрузкой: до 7–10 секунд (откуда и брался
  10с в исходной паузе).
- 8с — c запасом покрывает наблюдаемый верх диапазона WSL2, при этом
  добавляет к 56-точечному прогону максимум `8 × 55 = 440с ≈ 7.3 мин`.
  Часть из этих 55 переходов уже разделена `runner.Build()` (5–20с),
  но cooldown всё равно отрабатывает — это копеечная страховка.

На слабых машинах cooldown=8с — фактически бесплатный (диск всё равно
медленнее). На очень мощных (где cleanup за 1с) можно опустить до 3–5с
через `--cooldown 3`, если хочется минут сэкономить.

## Влияние на бенчмарк

- **Числа в отчёте не меняются**: cooldown стоит **до** `Stopwatch.Start`
  внутри `runner.Run`, измеряемое время прогона остаётся прежним.
- **Wall-clock прогона** удлиняется на ~7 минут при дефолте 8с —
  приемлемо.
- **Single `dotnet test` ускоряется на 10с** холодного старта (это
  чистый выигрыш для `run-integresql.ps1`, IDE-запусков, dev-итераций).

## Что не делаем

- Не трогаем `WaitForRyukToStop` в `TestRunner.cs` — он дёшев и
  ловит дешёвую часть окна; cooldown добавляется поверх него, не вместо.
- Не трогаем `ContainerFixture.cs` — там in-process паузы обязательны.
- Не пытаемся «умно» выбирать длину cooldown в зависимости от того,
  что было предыдущим прогоном. Простой константный sleep достаточен.
- Не убираем вторую паузу из инфры — у неё другая семантика, она
  продолжает быть нужна и в одиночных `dotnet test`.

## Файлы

| Файл | Изменение |
|---|---|
| `tools/BenchmarkRunner/Runner/TestRunner.cs` | Добавить `_cooldown`, `ApplyCooldown()` перед `Run`/`Warmup` |
| `tools/BenchmarkRunner/Program.cs` | Парсинг `--cooldown`/`-c`, передача в `TestRunner`, вывод в Config |
| `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` | Убрать первый `Task.Delay(10s)` и его комментарий |
| `tests/FastIntegrationTests.Tests.Respawn/Infrastructure/RespawnContainerManager.cs` | Убрать первый `Task.Delay(10s)` и его комментарий |
| `CLAUDE.md` | Добавить `--cooldown` в таблицу аргументов BenchmarkRunner |
| `README.md` | Добавить `--cooldown` в таблицу аргументов (если она там есть) |

## Проверка

- `dotnet build` всех тест-проектов и `tools/BenchmarkRunner` — без warning.
- `dotnet test --list-tests tests/FastIntegrationTests.Tests.IntegreSQL` —
  тесты обнаруживаются (контейнер не стартует на этой команде, но
  компиляция инфры проверяется).
- Полный прогон бенчмарка не запускаем — это в зоне пользователя.

## Открытые риски

- Если кто-то параллельно с бенчмарком запустит другой `docker run`
  на том же хосте, cooldown его не защитит. Это вне скоупа — в фикстуре
  и так не было защиты.
- Если в будущем добавят пятый подход с фикстурой типа Respawn (один
  контейнер на процесс, без in-process пересоздания) — нужно будет
  проверить, что в его менеджере **нет** первой паузы (а если есть —
  убрать). Сейчас затронутые файлы перечислены явно.
