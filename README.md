# Информер

Трей-приложение для приёма уведомлений по HTTP (JSON) из внешних систем (например, драйвера
1С), их отображения во всплывающих тостах и хранения истории с фильтрацией по отправителю.

## Стек

- **.NET 6** (net6.0) — работает на Windows 7+ и Linux
- **Avalonia UI 11** — кроссплатформенный интерфейс + системный трей (`TrayIcon`)
- **ASP.NET Core Minimal API (Kestrel)**, встроенный в тот же процесс, что и UI
- **EF Core 6 + SQLite** — провайдер подключается в одном месте (`Program.cs`), поэтому
  замена на SQL Server/PostgreSQL — это правка одной строки `UseSqlite(...)`

## Структура решения

```
Informer.sln
├── Informer.Core   — сущности (Notification, ApiKeyEntity, AppSettingsEntity), DTO, NotificationBus
├── Informer.Data   — InformerDbContext, миграции, design-time factory
├── Informer.Api    — middleware (API key, rate limit), минимальные API эндпоинты
└── Informer.App    — Avalonia UI (трей, тосты, история, настройки) + точка входа Program.cs,
                       который поднимает Kestrel и UI-loop в одном процессе
```

## Как открыть и собрать в Visual Studio 2022

1. Установи workload **".NET desktop development"** (для WinExe) — стандартно уже есть.
   Расширение **"Avalonia for Visual Studio"** из Marketplace нужно только для XAML-превью,
   на сборку не влияет.
2. Открой `Informer.sln`.
3. Правой кнопкой на `Informer.App` → **Set as Startup Project**.
4. NuGet-пакеты подтянутся автоматически при первой сборке (interactive restore).
   Если нет — ПКМ на решении → **Restore NuGet Packages**.

## Применение миграций (перед первым запуском)

Открой **Tools → NuGet Package Manager → Package Manager Console**, выбери
**Default project: Informer.Data**, затем:

```powershell
Add-Migration InitialCreate -Project Informer.Data -StartupProject Informer.App
Update-Database -Project Informer.Data -StartupProject Informer.App
```

(Миграция в репозитории ещё не сгенерирована — папка `Informer.Data/Migrations` пуста,
её нужно один раз создать локально, поскольку в песочнице, где писался код, нет .NET SDK
для запуска `dotnet ef`.)

Дальнейшие миграции при изменении сущностей — та же команда `Add-Migration <Имя>` +
`Update-Database`. При запуске `Program.cs` вызывает `db.Database.Migrate()` автоматически,
так что после `Update-Database` один раз (для генерации файла миграции) на других машинах
достаточно просто запустить exe — база создастся/обновится сама.

## Запуск

`F5` в VS2022. Приложение появится в системном трее (иконка-заглушка лежит в
`Informer.App/Assets/tray-icon.ico` — замени на свою). Правый клик по иконке:
**История уведомлений**, **Настройки**, **Выход**.

По умолчанию Kestrel слушает `http://127.0.0.1:5005` (только локально — см.
`Program.cs`/`appsettings.json`, при необходимости приёма из локальной сети поменяй
`UseUrls` на `http://0.0.0.0:5005` и не отключай API-key/rate-limit).

## Формат входящего запроса

```
POST http://127.0.0.1:5005/api/notify
Content-Type: application/json
X-Api-Key: <ключ, если включено требование ключа в настройках>

{
  "header": "1C:Session:MainBase:ivanov",
  "description": "Новый документ проведён",
  "ApiKey": "не используется для авторизации, оставлено для совместимости формата",
  "ResponseBody": {
    "any": "произвольная структура"
  }
}
```

`header` сохраняется как есть и используется только для отображения/фильтрации по
отправителям в истории — он не является заранее согласованным идентификатором и не
участвует в авторизации (см. пункт "Вопросы" в исходном ТЗ). Авторизация — только через
заголовок `X-Api-Key`, сверяемый с активными ключами из `Настройки → API-ключи`.

Быстрый тест через curl:

```bash
curl -X POST http://127.0.0.1:5005/api/notify \
  -H "Content-Type: application/json" \
  -H "X-Api-Key: <ключ_из_окна_настроек>" \
  -d '{"header":"TestSender","description":"Привет из curl","ResponseBody":{"x":1}}'
```

## Прочие эндпоинты (используются UI, но доступны и извне для отладки/интеграций)

- `GET  /api/history?sender=...&fromUtc=...&toUtc=...&page=1&pageSize=100`
- `GET  /api/history/senders`
- `POST /api/history/{id}/read`
- `GET  /api/settings`
- `PUT  /api/settings`
- `GET  /api/apikeys`
- `POST /api/apikeys`
- `DELETE /api/apikeys/{id}`

## Известные ограничения текущего скелета (что доделать перед реальным продакшеном)

- Миграции не сгенерированы (см. выше) — обязательно сделать перед первым запуском.
- `RateLimitMiddleware` хранит счётчики в статическом `ConcurrentDictionary` в памяти
  процесса — для одного трей-инстанса этого достаточно, но при большом числе уникальных
  IP стоит добавить периодическую очистку старых записей.
- История/настройки сейчас читаются UI напрямую через `InformerDbContext` (в одном
  процессе с Kestrel), а не по HTTP — это быстрее и проще, но если ты захочешь вынести
  UI в отдельный процесс от API в будущем, эти ViewModel нужно переключить на `HttpClient`
  к уже готовым `/api/history*` и `/api/settings*` эндпоинтам.
- Иконка трея — плейсхолдер, замени `Assets/tray-icon.ico` на свою (ICO с размерами
  16/32/48/256).
- Автозапуск при старте Windows/Linux не реализован — обычно решается ярлыком в
  `shell:startup` (Windows) или `.desktop`-файлом автозапуска / systemd user unit (Linux).
- Публикация (`dotnet publish`) под self-contained для Windows/Linux не настроена в
  csproj — при необходимости добавь `RuntimeIdentifiers`/`SelfContained` в
  `Informer.App.csproj`.
