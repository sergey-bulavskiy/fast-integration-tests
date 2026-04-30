# Опциональный tmpfs для PostgreSQL в IntegreSQL — переключатель для машин с медленным диском

## Симптом, который это лечит

Бенчмарк падает на точке `[37/56] IntegreSQL / scale / m=117 s=50 t=12`
на одной конкретной машине (HOME-PC), при этом на более слабой машине
пользователя та же точка проходит. Лог `last-failure (7).log` содержит
**2885 одинаковых ошибок** `Npgsql.NpgsqlException : Exception while
reading from stream` с inner `EndOfStreamException : Attempted to read
past the end of the stream`. Все ошибки — на стадии
`NpgsqlConnection.Open` внутри `WaitUntilDatabaseIsCreated` или первого
запроса EF.

## Почему это происходит

При `scale=50` за один прогон `dotnet test` создаётся **~9800
клонов БД** — каждый клон это `CREATE DATABASE ... TEMPLATE shop-default`
в PostgreSQL. Это операция уровня файловой системы: сервер физически
копирует data-файлы шаблонной БД в новую директорию `base/<oid>/`.
`fsync=off` и `synchronous_commit=off` на это **не влияют** — они
отключают только синхронность WAL, а не саму запись данных.

При нормальной дисковой подсистеме один клон занимает миллисекунды.
На машинах, где SSD под троттлингом (антивирус, WSL2 backing disk,
тепловой троттлинг, или просто медленный диск), синхронный
`CREATE DATABASE` стоит секунды. На таких таймингах:

1. IntegreSQL получает HTTP-запрос «дай клон» и ставит `CREATE` в очередь.
2. Тест на стороне xUnit получает строку подключения и пытается
   `NpgsqlConnection.Open` с дефолтным таймаутом 15 секунд.
3. PostgreSQL держит ACCESS EXCLUSIVE lock на `pg_database` пока
   `CREATE DATABASE` не завершит копирование файлов; параллельные
   `CREATE`/`DROP` ждут.
4. Истекает Npgsql Connection Timeout, kernel шлёт TCP RST, или
   сервер закрывает соединение по своему таймауту → клиент видит
   `EndOfStream`.

То есть симптом — это **массовая деградация под I/O-нагрузкой**, не
краш контейнера и не баг кода. На машине пользователя (более слабой
по CPU/RAM, но со здоровым диском) этого симптома нет.

## Решение

Дать возможность смонтировать `/var/lib/postgresql/data` как `tmpfs`
(in-memory файловая система Linux). При этом `CREATE DATABASE` становится
операцией над страницами page cache в RAM — ни одного `write()` на
физический диск.

Эффект:
- На пострадавшей машине s=50 проходит стабильно.
- На здоровых машинах — заметное общее ускорение CREATE/DROP DATABASE.
- Расход RAM при scale=50 и пуле в 48 клонов — ориентировочно
  ~700MB–1.5GB (точная цифра зависит от наполнения шаблонной БД).
- При остановке контейнера данные исчезают — это и нужно, тестовые БД
  эфемерные по определению.

## Как именно — комментарий, а не env-флаг

Вместо runtime-флага (env-переменной или `--use-tmpfs`) использовать
**закомментированную строку кода рядом с описательным комментарием**.
Аргументы:

- Это редкий ручной переключатель — раз в год кто-то отлаживает
  странную машину. Не runtime-решение.
- Меньше поверхности кода: не нужна валидация значений, нет ветвлений.
- Самодокументируется: проблема и её решение лежат рядом в одном файле.
- Если переключатель станет нужен где-то ещё, его легко поднять до
  явного флага позже. YAGNI.

## Что меняем

### `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs`

Перед существующим `var pgContainer = new PostgreSqlBuilder()...`
добавить блок-комментарий и закомментированную строку `WithTmpfsMount`
внутри билдера:

```csharp
// Если на твоей машине бенчмарк IntegreSQL падает массой Npgsql
// `EndOfStream` на больших scale (50+), а disk latency в момент
// падения уходит в полку — расскоментируй WithTmpfsMount ниже.
//
// При scale=50 один прогон делает ~9800 CREATE DATABASE TEMPLATE.
// Это синхронные filesystem-операции: fsync=off их не ускоряет
// (он про WAL, а не про data-файлы). На медленном/троттлящемся
// диске PostgreSQL держит ACCESS EXCLUSIVE lock на pg_database
// дольше Npgsql Connection Timeout (15с), параллельные коннекты
// ловят TCP RST на Open — это и есть "Attempted to read past
// the end of the stream".
//
// tmpfs кладёт data-файлы в RAM (через page cache, без write()
// на физический диск). CREATE DATABASE становится memcpy.
// Стоит ~1–1.5 GB RAM при scale=50; при остановке контейнера
// данные эфемерны — для тестов это безопасно.
//
// Подробнее: docs/superpowers/specs/2026-04-30-postgres-tmpfs-toggle-design.md
var pgContainer = new PostgreSqlBuilder()
    .WithImage("postgres:16-alpine")
    // .WithTmpfsMount("/var/lib/postgresql/data")
    .WithNetwork(network)
    .WithNetworkAliases("postgres")
    .WithCommand(...)
    .Build();
```

Размер tmpfs **не указываем** — пусть Docker управляет дефолтом
(половина RAM хоста). Указанный лимит даёт ложное чувство контроля,
а реальный риск (нехватка RAM) пользователь увидит сам через OOM
контейнера и сделает осознанный выбор.

### Скоуп

**Только IntegreSQL.** Не трогаем `RespawnContainerManager`,
`ContainerFixture`, `SharedContainerManager`. Аргументы:

- Падение узкое — на одной точке одного подхода. Trying to "fix all
  approaches consistently" расширяет скоуп без подтверждённой нужды.
- Расход RAM на четыре tmpfs-mounted PG ≈ ×4. Если это нужно где-то
  ещё, делается отдельной правкой по конкретному симптому.
- В IntegreSQL это особенно критично из-за CREATE DATABASE TEMPLATE
  на каждый тест; в Respawn основная нагрузка — DELETE по FK
  (write WAL, что и так в RAM при `synchronous_commit=off`); в
  Testcontainers/Shared CREATE DATABASE есть, но на меньших масштабах
  и на разных контейнерах — пиковая нагрузка распределена.

Если в будущем кто-то поймает аналогичный симптом на Testcontainers —
добавим аналогичный комментарий туда тогда же.

### Документация

В `CLAUDE.md` добавить короткую заметку в секцию про IntegreSQL:

> **Если бенчмарк падает на больших scale с массой `EndOfStream` от
> Npgsql:** твоя машина под disk-троттлингом. В
> `IntegresSqlContainerManager.cs` рядом с `pgContainer` закомментирован
> `WithTmpfsMount` — расскоментируй и попробуй.

В README.md ничего не добавлять — это операционный нюанс для
сопровождающего, не для пользователя репозитория.

## Что не делаем

- Не делаем env-флаг или CLI-аргумент.
- Не указываем `size=` для tmpfs.
- Не применяем ко всем 4 PG-контейнерам — только IntegreSQL.
- Не трогаем `BenchmarkRunner` — это правка чисто инфраструктуры.
- Не пытаемся менять механику бенчмарка (`max_connections`, размер
  пула IntegreSQL, и т.п.) — это всё workaround'ы вокруг I/O, а
  tmpfs убирает I/O в принципе.

## Файлы

| Файл | Изменение |
|---|---|
| `tests/FastIntegrationTests.Tests.Shared/Infrastructure/IntegreSQL/IntegresSqlContainerManager.cs` | Добавить блок-комментарий + закомментированный `.WithTmpfsMount("/var/lib/postgresql/data")` |
| `CLAUDE.md` | Короткая заметка в секции про IntegreSQL про переключатель |

## Проверка

- `dotnet build tests/FastIntegrationTests.Tests.IntegreSQL` — без warning.
- `dotnet build tests/FastIntegrationTests.Tests.NUnit.IntegreSQL` —
  тоже без warning (тот же `IntegresSqlContainerManager` через `Tests.Shared`).
- Корректность раскомментирования вручную проверяется на машине, где
  есть симптом — это уже зона пользователя, не покрывается автотестом.

## Открытые риски

- На очень слабых машинах с малым лимитом RAM Docker Desktop (по
  дефолту WSL2 берёт половину системной RAM, на 8GB-машине это 4GB)
  раскомментированный tmpfs может упереться. Это редкий кейс, ловится
  как OOM контейнера и понятно фиксится откатом одной строки.
- Шаблонная БД увеличивается при добавлении сидов или роста миграций —
  расход RAM пропорционально растёт. Сейчас seed пуст, миграций 117;
  если в будущем сид станет тяжёлым, прикинуть бюджет повторно.
