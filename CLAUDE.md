# PriceTracker API

Учебный пет-проект: товары и история цен. Minimal API на .NET 10 + PostgreSQL 17 + Dapper (без EF Core).
Контекст: готовлюсь к собеседованиям, слабое место — SQL руками и индексы. Код пишу сам, ты ревьюишь и объясняешь.

## Цели проекта

1. Отточить написание SQL-запросов через Dapper (JOIN, GROUP BY/HAVING, оконные функции, пагинация, транзакции, EXPLAIN).
2. Использовать современные практики .NET 10.
3. Использовать оптимальные решения, а не «как проще»: индексы под запросы, keyset вместо глубокого OFFSET, один источник правды для данных.
4. Довести репозиторий до чёткого финала (критерии ниже).

## Как со мной работать

- Я пишу код сам. Ты показываешь ошибки, объясняешь причины и даёшь поправки, а не переписываешь всё целиком.
- **SQL за меня не пиши.** Задачи ставь словами: что вернуть, какие граничные случаи, какие ловушки, какой метод Dapper подходит. Готовый SQL — только если я прошу об этом явно по конкретному запросу. Ревью моего SQL (ошибки, планы, индексы) — всегда пожалуйста.
- Если вижу антипаттерн, называй его и объясняй, как это спросят на собеседовании.
- Задачи на SQL (sql-ex / LeetCode) я решаю без ИИ, здесь это не нарушаем.
- Перечень задач по эндпоинтам (без SQL) лежит в `docs/query-tasks.md`. Статус работ ведём в git и issues, а не в этом файле.

## Стек и соглашения

- .NET 10, `dotnet new web`, пакеты: `Dapper`, `Npgsql.DependencyInjection`, `Microsoft.AspNetCore.OpenApi`.
- `NpgsqlDataSource` через DI, своей фабрики соединений нет.
- `DefaultTypeMap.MatchNamesWithUnderscores = true` (`changed_at` → `ChangedAt`).
- Только параметры `@Name` через анонимный объект или модель. Никакой склейки строк.
- Во все вызовы Dapper передаётся `CancellationToken` через `CommandDefinition`. В транзакции `transaction` тоже передаётся явно в каждый `CommandDefinition`.
- SQL в `const string` или raw string literal (`"""`), колонки в `snake_case`.
- Модели, которые материализует Dapper: классы со свойствами, не позиционные record (Dapper и несовпадение типов конструктора). Запросные DTO (`Models/Request`) могут быть record.
- `timestamptz` ↔ `DateTimeOffset` (`ToUniversalTime()`, Npgsql не принимает ненулевое смещение); `DateTime` только с `Kind = Utc`.
- Время берём из `TimeProvider`, а не из `DateTimeOffset.UtcNow`.
- Конфигурация, без которой приложение не работает, валит старт (`?? throw`), без подстановки заглушек.
- Ловим только понятные ошибки: `PostgresException` с `23503` и `23505`. Остальное в глобальный обработчик → 500. Смысл кода зависит от операции:
  - вставка цены: `23503` → 404 (товара нет), `23505` → 409 (цена на эту дату уже есть);
  - удаление товара: `23503` → 409 (есть история цен).
  - При `23505` проверять имя ограничения (`ConstraintName`), чтобы второе unique-ограничение в будущем не маскировалось под этот конфликт.
- Результат операций репозитория — `Result<T>` с `Error(ErrorType, message)`. `Result<T>` неизменяемый (без публичных сеттеров).
- Структура без лишних слоёв: никаких MediatR и чистой архитектуры.

```
PriceTracker.Api/                  (корень репозитория)
├── Program.cs
├── appsettings.json               (строка подключения Default = null, задаётся user-secrets / env)
├── db/init.sql
├── docs/                          (query-tasks.md, потом queries.md)
├── Endpoints/ProductEndpoints.cs
├── Data/ProductRepository.cs, PriceChangeRepository.cs
└── Models/
    ├── Product.cs, PriceChange.cs
    ├── Core/ (BaseModel, Result, Error)
    ├── Enums/ (ErrorType)
    └── Request/ (CreateProductRequest, PriceChangeRequest)
```

Ещё нет: `docker-compose.yml`, тестов, CI, README, `requests.http` (пункты DoD ниже).

## Запуск (локально)

- Строка подключения: `ConnectionStrings:Default` через user-secrets или переменную окружения `ConnectionStrings__Default`. В `appsettings.json` она `null`, приложение без неё не стартует (так и задумано).
- База поднимается из `db/init.sql`; после любой правки схемы `docker compose down -v` (когда compose появится).

## Принятые решения

- **Цена хранится только в `price_changes`.** `Product.Price` убрали (дубль данных). Текущая цена = цена на момент «сейчас».
- **Цена строго больше нуля.** Проверка в API и check-ограничение в схеме (сейчас в `init.sql` допускается ноль — привести в соответствие).
- **Id — `Guid.CreateVersion7()`**, генерируется в приложении (в Postgres 17 нет `uuidv7()`, `gen_random_uuid()` даёт случайный v4 и фрагментирует индекс). Колонка `id uuid primary key` без default. Поэтому `POST /products` не использует `RETURNING id`: id известен до вставки.
- Известное отступление: в seed id товаров фиксированные (для ручных запросов), а id в `price_changes` — v4 из `gen_random_uuid()`. Допустимо только в seed.
- `BaseModel.Id` — `init`, не меняется после создания. Инициализатор срабатывает и при чтении из БД (лишний UUIDv7 на строку, потом затирается Dapper'ом) — осознанная цена за простоту.
- `PriceChange.EffectiveFrom` — «цена действует с этого момента», можно планировать на будущее.
- Схема: `unique (product_id, effective_from)` (он же создаёт составной индекс; отдельный индекс с теми же колонками — дубль, не нужен) и внешний ключ на `products`.
- `init.sql` вместо миграций; после правки `docker compose down -v`.
- Сравнение uuid (курсор keyset) только в SQL: Postgres сравнивает байты, что совпадает с порядком UUIDv7; `Guid.CompareTo` в .NET даёт другой порядок. Внутри одной миллисекунды порядок v7 случайный, но стабильный, для keyset этого достаточно.

## Модели (текущие, как в коде)

```csharp
public abstract class BaseModel { public Guid Id { get; init; } = Guid.CreateVersion7(); }
public class Product : BaseModel { public required string Name { get; init; } }
public class PriceChange : BaseModel
{
    public required Guid ProductId { get; init; }
    public required DateTimeOffset EffectiveFrom { get; init; }
    public required decimal Price { get; init; }
}
```

## Эндпоинты

Источник: пункты 1–7 из исходного списка; 8–15 восстановлены по плану (оригинальный список из 15 потерян). Описания задач без SQL — в `docs/query-tasks.md`.

Базовый CRUD:
1. `GET /products/{id}`
2. `POST /products` — id генерируется в приложении, ответ `201` с Location
3. `POST /products/{id}/prices` — 404 при отсутствии товара, 409 при дубле даты (id товара из маршрута, не из тела)
4. `GET /products/{id}/price?at=` — цена на дату, по умолчанию сейчас (главный запрос)
5. `GET /products/{id}/prices` — вся история

Пагинация и фильтры:
6. `GET /products?search=&page=&pageSize=` — `ILIKE`, `LIMIT/OFFSET`, total через оконный `count(*)`, текущая цена товара
7. `GET /products/feed?after=&limit=` — keyset-пагинация по UUIDv7

Восстановленные (заменить оригиналами, если найдутся):
8. `PUT /products/{id}` — переименование
9. `DELETE /products/{id}` — 409, если есть история цен (или удаление вместе с ценами)
10. `DELETE /products/{id}/prices/{priceId}`
11. `PUT /products/{id}/prices/schedule` — атомарная замена будущих цен в явной транзакции (блокировка строки товара)
12. `GET /products/{id}/prices/stats?from=&to=` — min/max/avg/count за период
13. `GET /products/{id}/prices/changes` — изменение цены относительно предыдущей через `LAG`
14. `GET /reports/volatility?from=&to=&minChanges=` — `GROUP BY` / `HAVING`
15. `GET /reports/current-prices` — последняя цена по каждому товару (три способа, сравнить планы)

Плюс `GET /health`.

Главный запрос (мой вариант A из чата):

```sql
SELECT price FROM price_changes
WHERE product_id = @ProductId AND effective_from <= @At
ORDER BY effective_from DESC
LIMIT 1;
```

Ожидание в `EXPLAIN ANALYZE`: `Index Scan` без `Sort`. Известная проблема варианта A: `null` не различает «товара нет» и «цены ещё нет» (см. задачу 4 в `docs/query-tasks.md`).

## Финал проекта (Definition of Done)

1. Реализованы все эндпоинты: CRUD, цена на дату, история, offset- и keyset-пагинация, минимум 2 отчёта (агрегаты + оконная функция).
2. Хотя бы одна операция в явной транзакции (`BeginTransactionAsync`), с объяснением, зачем она нужна (задача 11).
3. Ошибки: `ProblemDetails` + глобальный `IExceptionHandler`; 404/409/400 отдаются осмысленно, без утечки деталей базы. `Result<T>` из репозитория мапится в `TypedResults` (201/404/409), а не отдаётся в ответ как есть.
4. Валидация входных данных (цена > 0, размер страницы ограничен, дата корректна).
5. Интеграционные тесты на Testcontainers (реальный Postgres): минимум по тесту на каждый эндпоинт, включая сценарии 404 и 409.
6. `docs/queries.md`: ключевые SQL-запросы, вывод `EXPLAIN ANALYZE` до и после индекса, 2–3 абзаца «почему так». Для «до» нужна копия таблицы без индексов и сгенерированные данные: на 11 строках Postgres выберет Seq Scan.
7. `docker compose up` поднимает базу и API с нуля, `/health` проверяет подключение к базе.
8. CI (GitHub Actions): build + test.
9. README: что за проект, как запустить, список эндпоинтов с примерами, принятые решения (почему Dapper, UUIDv7, keyset, нет `Product.Price`).
10. Файл `requests.http` с примерами запросов.

Готово, когда все пункты закрыты и репозиторий можно показать на собеседовании без оговорок.

## Современные практики .NET 10, которые стоит применить

- Primary constructors (уже в репозитории), `required`/`init`, raw string literals для SQL.
- `TypedResults` и `MapGroup` в эндпоинтах, `AddOpenApi`/`MapOpenApi`.
- `AddProblemDetails` + `IExceptionHandler`.
- `AddHealthChecks` с проверкой Npgsql.
- Валидация настроек через `IOptions` + `ValidateOnStart`.
- `TimeProvider` вместо `DateTime.UtcNow`/`DateTimeOffset.UtcNow` (тесты без привязки к часам).
- Централизованные пакеты версий (`Directory.Packages.props`), если проект вырастет до нескольких csproj.
