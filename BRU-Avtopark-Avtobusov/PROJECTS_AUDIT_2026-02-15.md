# Аудит проектов репозитория (2026-02-15)

## Методика

Проверка выполнена статически (без сборки), так как в окружении отсутствует `dotnet` CLI. Основные источники:
- список проектов из `*.csproj`/`*.esproj`;
- параметры `TargetFramework`, `Nullable`, `ImplicitUsings`, зависимости и проектные ссылки;
- выборочная проверка solution и явных технических запахов.

## Приоритетные исправления (сделано в этом коммите)

1. Исправлен путь к frontend-проекту в solution с учетом регистра для Linux/macOS.
2. Удалены пустые заглушки `Class1.cs` в `TicketSalesApp.Core` и `TicketSalesApp.Services`.

---

## Анализ по каждому проекту

### 1) DynamicForms.Library.Avalonia
- **Статус:** `net8.0;net9.0`, `Nullable=enable`, `ImplicitUsings=enable`.
- **Что улучшить:** добавить smoke-тесты рендеринга/валидации форм в CI для обеих TFM.

### 2) DynamicForms.Library.Core
- **Статус:** `net8.0;net9.0`, хорошие базовые настройки.
- **Что улучшить:** добавить unit-тесты на сериализацию/десериализацию схем и edge-cases валидации.

### 3) DynamicForms.Library.WPF
- **Статус:** `net8.0-windows;net9.0-windows`.
- **Что улучшить:** предусмотреть `UseWPF`-специфичную проверку в CI на Windows runner.

### 4) SuperNova.Desktop
- **Статус:** `net9.0`, много пакетов.
- **Риск:** высокая плотность зависимостей => риск version drift.
- **Что улучшить:** централизовать версии пакетов через `Directory.Packages.props`.

### 5) SuperNova.Runtime.Tests
- **Статус:** тестовый проект, `TargetFramework=$(DotNetVersion)`.
- **Риск:** неявное значение `DotNetVersion` может ломать standalone-сборку/IDE.
- **Что улучшить:** задать fallback через `Directory.Build.props`.

### 6) SuperNova.Runtime
- **Статус:** `net9.0`, `ImplicitUsings` не задан.
- **Что улучшить:** унифицировать стиль (явно включить/выключить `ImplicitUsings` во всех проектах).

### 7) SuperNova.Standalone
- **Статус:** `TargetFramework=$(DotNetVersion)`.
- **Риск:** тот же, что у тестов (зависимость от внешнего MSBuild свойства).

### 8) SuperNova
- **Статус:** `net9.0`, крупный UI-проект, много пакетов.
- **Что улучшить:** ввести автоматическую проверку мертвых зависимостей и уязвимостей пакетов.

### 9) TicketSalesAPP.Mobile.UI.MAUI.Domain
- **Статус:** `net9.0`.
- **Что улучшить:** добавить строгие контракты DTO и отдельные тесты mapping/валидации.

### 10) TicketSalesAPP.Mobile.UI.MAUI.Infrastructure
- **Статус:** `net9.0`, есть ссылка на Domain.
- **Что улучшить:** покрыть retry/timeout сценарии тестами интеграции API-клиента.

### 11) TicketSalesAPP.Mobile.UI.MAUI
- **Статус:** `net9.0-android;net9.0-ios`.
- **Риск:** мобильный проект критичен к версиям SDK/workload.
- **Что улучшить:** добавить матрицу CI для android/ios с ранней проверкой workload.

### 12) TicketSalesApp.AdminServer.Tests
- **Статус:** есть тестовый контур, но мало тестовых файлов.
- **Что улучшить:** расширить покрытие критических endpoint'ов (auth, отчеты, справочники).

### 13) TicketSalesApp.AdminServer
- **Статус:** `net9.0`, много пакетов, серверный контур.
- **Риск:** в репозитории присутствуют SQLite-файлы и WAL/SHM артефакты.
- **Что улучшить:** исключить runtime-артефакты БД из VCS, хранить сиды/дампы отдельно.

### 14) TicketSalesApp.Core.Legacy
- **Статус:** `net40`.
- **Риск:** очень старый TFM, повышенные security/compatibility риски.
- **Что улучшить:** план миграции на минимум `net8.0` или изоляция в отдельную legacy-ветку.

### 15) TicketSalesApp.Core
- **Статус:** `net9.0`, но `Nullable` и `ImplicitUsings` не заданы.
- **Что исправлено:** удалена пустая заглушка `Class1.cs`.
- **Что улучшить:** включить `Nullable` и постепенно устранить предупреждения.

### 16) TicketSalesApp.Services
- **Статус:** `net9.0`, `Nullable=enable`, `ImplicitUsings=enable`.
- **Что исправлено:** удалена пустая заглушка `Class1.cs`.
- **Что улучшить:** проверить границы сервисов: вынести контракты (интерфейсы) в отдельный assembly при росте проекта.

### 17) TicketSalesApp.UI.Administration.Avalonia
- **Статус:** `net9.0`, `ImplicitUsings` не задан.
- **Что улучшить:** унифицировать настройки компилятора и добавить UI smoke-тесты.

### 18) TicketSalesApp.UI.Avalonia
- **Статус:** `net9.0`, настройки современные.
- **Что улучшить:** добавить baseline-снапшоты ключевых экранов в регресс-тестах UI.

### 19) TicketSalesApp.UI.LegacyForms.DX.Windows
- **Статус:** `net9.0-windows`, nullable/implicit не заданы.
- **Что улучшить:** включить nullable в режиме `warnings` и мигрировать поэтапно.

### 20) dynamic_forms_frontend_bru_avtopark (esproj)
- **Статус:** фронтенд-проект присутствует.
- **Что исправлено:** путь в `.sln` приведен к реальному регистру каталога для POSIX-систем.
- **Что улучшить:** добавить lockfile policy и проверку `npm audit`/`pnpm audit` в CI.

---

## Общие рекомендации по репозиторию

1. Ввести единые `Directory.Build.props` + `Directory.Packages.props`.
2. Добавить обязательный CI pipeline: restore/build/test + линтеры + SCA.
3. Пересмотреть политику хранения артефактов (`*.db`, `-wal`, `-shm`, `*.suo`, временные backup-файлы).
4. Укрепить тестирование серверного API и критичных бизнес-процессов (продажи, расписания, роли).
5. Зафиксировать roadmap по Legacy-компонентам (WinForms/.NET Framework 4.0).
