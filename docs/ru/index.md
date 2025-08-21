
# FireLog-Insight (Русский)

**Язык:** [Čeština](../cs/index.md) • [Русский](../ru/index.md) • [Українська](../uk/index.md)

## Обзор
Настольное приложение (WPF, .NET) для анализа журналов брандмауэра Windows.
- Фильтры по IP, времени, действию/типу
- Визуализации: круговая (разрешено vs заблокировано), столбчатая (топ протоколов)
- Эвристики подозрительной активности (например, сканы/«brute-force»-подобный шум)
- Экспорт CSV/TXT; опциональная загрузка в БД (PostgreSQL через Supabase Session Pooler для IPv4)
- Графики: рисуются на WPF `Canvas` (PathGeometry/ArcSegment), экспорт PNG через `RenderTargetBitmap`

## Сборка
```bash
dotnet restore
dotnet build -c Release
```

## Конфигурация
> [!IMPORTANT]
> - Не коммитьте реальные секреты. Используйте переменные окружения или локальный файл рядом с исполняемым.  
> - В IPv4-сетях используйте хост Session Pooler; в IPv6 можно прямое подключение.

**Переменные окружения (рекомендуется)**
```
APP_DB_HOST=aws-0-eu-central-1.pooler.supabase.com
APP_DB_PORT=5432
APP_DB_USER=postgres.<instance-id>
APP_DB_PASSWORD=<secret>
APP_DB_NAME=postgres
APP_DB_SSLMODE=Require
```

**Локальный файл (фолбэк, вне контроля версий):** `db_credentials.json`
```
{
  "Host": "aws-0-eu-central-1.pooler.supabase.com",
  "Port": 5432,
  "User": "postgres.<instance-id>",
  "Password": "<SET_VIA_ENV_OR_LOCAL_FILE>",
  "Database": "postgres",
  "SslMode": "Require"
}
```

**Строка подключения (Npgsql):**
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```

> [!CAUTION]
> Настольные приложения не могут полностью скрыть секреты. Используйте роли БД с минимальными правами и меняйте пароли при утечке.

## Команда & Участники
**Bohdan ——> @Rincodev**  
Тимлид, разработка и релиз; архитектура и безопасность; подключение/конфиг БД; извлечение и экспорт БД; интеграция и слияние кода; связывание UI (действия/обработчики); графики; экспорты; стабилизация и тесты.

**Štefan ——> @Just-Kurumi**  
UI/UX; XAML-макеты и визуальная тема; навигация/меню; таблицы/гриды.

**Hanuš ——> @Menk1l**  
Архитектура и интеграция данные↔UI; фильтрация/поиск; интеграция PowerShell; локальная обработка логов; интерфейс Security Alerts; парсер логов брандмауэра Windows; тестирование.

**Lukáš ——> @Tykanek**  
Аутентификация (Login/Logout); связка контролов→логика; IP/временные фильтры; тестирование.

Участники: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rincodev) • @Just-Kurumi (Kurumi)

## Лицензия
Распространяется по MIT — см. [LICENSE](../../LICENSE)
```
MIT © 2025 Rincodev (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
```
