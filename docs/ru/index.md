# FireLog-Insight (Русский)

**Язык:** [English](../en/index.md) • [Čeština](../cs/index.md) • [Українська](../uk/index.md)

## Обзор
Desktop-приложение (WPF, .NET) для анализа логов Windows Firewall.
- Фильтры по IP, времени и типу действия
- Визуализация: круговая (allow/deny), столбчатая (топ протоколов)
- Эвристики подозрительной активности (сканы/bruteforce-подобный шум)
- Экспорт CSV/TXT; опциональная загрузка в БД (PostgreSQL через Supabase Session Pooler для IPv4)
- Графики: рисуются на WPF `Canvas` (PathGeometry/ArcSegment), экспорт PNG через `RenderTargetBitmap`

## Сборка
```bash
dotnet restore
dotnet build -c Release
```
## Конфигурация
[!ВАЖНО]

-Не коммить настоящие креды. Используйте переменные окружения или локальный файл рядом с исполняемым.

-В сетях IPv4 используйте хост Session Pooler; для IPv6 можно прямое подключение.

-Переменные окружения (рекомендуется)
```
APP_DB_HOST=aws-0-eu-central-1.pooler.supabase.com
APP_DB_PORT=5432
APP_DB_USER=postgres.<instance-id>
APP_DB_PASSWORD=<secret>
APP_DB_NAME=postgres
APP_DB_SSLMODE=Require
```
-Локальный файл (fallback, вне репозитория): db_credentials.json
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
Строка подключения (Npgsql):
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```
[!ОСТОРОЖНО]
Desktop-приложения не могут полностью скрыть секреты. Используйте роли БД с минимальными правами и ротируйте пароли при утечке.

## Команда и вклад
Bohdan ——> @Rincodev

Руководство, разработка и релиз; архитектура и безопасность; подключение/конфигурация БД; извлечение и экспорт данных БД; интеграция и слияние кода; связывание UI с логикой; графики и экспорт; стабилизация и тесты.

Štefan ——> @Just-Kurumi 

UI/UX; XAML-макеты и визуальная тема; навигация/меню; таблицы/гриды.

Hanuš ——> @Menk1l

Архитектура и связка данных с UI; фильтрация/поиск; интеграция PowerShell; локальная обработка логов; Security Alerts UI; парсер логов Windows Firewall; тестирование.

Lukáš ——> @Tykanek

Аутентификация (Login/Logout); привязка действий к контролам; фильтры по IP/времени; тестирование.

Contributors: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rincodev) • @Just-Kurumi (Kurumi)

## Лицензия
Licensed under MIT — see [LICENSE](../../LICENSE)
```
MIT © 2025 Rincodev (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
