# Диагностика: "address already in use" при запуске Respawn/Testcontainers на мощной машине

## Симптом

Бенчмарк стабильно падает на первом же прогоне Respawn в Сценарии 1 (после warmup)
на мощной машине, но проходит без ошибок на слабой.

Точка падения: `Respawn / migrations / m=17 s=12 t=4`

## Ошибка из `benchmark-results/last-failure.log`

```
Docker.DotNet.DockerApiException : Docker API responded with status code=InternalServerError,
response={"message":"failed to set up container networking: driver failed programming external
connectivity on endpoint hopeful_faraday (...): failed to bind host port for
0.0.0.0::172.17.0.6:5432/tcp: address already in use"}
```

Падает в `RespawnFixture.InitializeAsync()` → `_container.StartAsync()`.

## Первопричина

### Механизм

`RespawnFixture` и `ContainerFixture` создают контейнеры без кастомной сети — они
присоединяются к дефолтному bridge `docker0` (подсеть `172.17.0.0/16`). Docker раздаёт IP
последовательно и переиспользует освободившиеся адреса.

Гонка:
1. Фикстура A запускается → контейнер получает `172.17.0.6`
2. Фикстура A останавливается → контейнер удалён, но iptables DNAT-правило
   `0.0.0.0:HOSTPORT → 172.17.0.6:5432` ещё не убрано (асинхронная очистка в ядре)
3. Фикстура B запускается сразу — Docker снова выдаёт `172.17.0.6`
4. Docker пытается добавить правило `0.0.0.0:HOSTPORT2 → 172.17.0.6:5432` → конфликт

### Почему на мощной машине, а не на слабой

При `scale=12` создаётся 396 фикстур (33 базовых класса × 12). На мощной машине
тест-классы с миграциями и тестами выполняются быстро — фикстуры сменяют друг друга
быстрее, чем ядро успевает убрать iptables-правила.

На слабой машине между остановкой одного контейнера и стартом следующего проходит
достаточно времени — конфликт не возникает.

### Почему warmup проходит

Warmup запускается без scale-классов — 33 Respawn-фикстуры вместо 396.
Меньший оборот фикстур → iptables успевает очиститься.

### Почему IntegreSQL не затронут

`IntegresSqlContainerManager` создаёт **одну** пару контейнеров на весь процесс
(static Lazy). Контейнеры не пересоздаются между тест-классами → IP не переиспользуется.
Кастомная сеть там нужна для другого: чтобы IntegreSQL-сервер мог достучаться до
PostgreSQL по алиасу `postgres` внутри Docker-сети.

## Предлагаемый фикс

Добавить изолированную Docker-сеть на каждую фикстуру в `RespawnFixture` и `ContainerFixture`.

```csharp
// InitializeAsync
_network = new NetworkBuilder().Build();
await _network.CreateAsync();

_container = new PostgreSqlBuilder()
    .WithNetwork(_network)
    ...

// DisposeAsync
await _container.DisposeAsync();
await _network.DisposeAsync();
```

Каждый контейнер получает IP в своей подсети (`172.20.0.2`, `172.21.0.2`, ...) — IP не
пересекаются. При `network.DisposeAsync()` Docker убирает все iptables-правила сети
атомарно. ConnectionString остаётся прежним (`localhost:RANDOM_HOST_PORT`) — хост-порт
не зависит от сети.

## Потенциальный риск фикса

Docker по умолчанию выделяет `/16` подсети из пула `172.17–172.31.x` (~30 штук).
При `MaxParallelThreads=4` одновременно живут не более 4 сетей, поэтому пул не
исчерпывается. Но если Docker не успевает вернуть подсеть в пул до следующего запроса —
возможна ошибка `could not find an available, non-overlapping IPv4 address pool`.

Это нужно проверить запуском `--test-respawn` с высоким scale.
