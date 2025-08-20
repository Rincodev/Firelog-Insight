FireLog-Insight

 • English
 • Čeština
 • Русский
 • Українська

English
What it is

Desktop tool (WPF, .NET) for Windows Firewall log analysis:

filtering by IP, time, action/type

visuals: pie (allowed vs blocked), bar (top protocols)

detection of suspicious activity (e.g., scans/bruteforce heuristics)

export: CSV and TXT reports

optional upload to DB (PostgreSQL/Supabase)

Key features

Fast local parsing, resilient to comments/format quirks

Charts rendered on WPF Canvas (vector, PNG export)

IPv4-compatible DB connectivity via Supabase Session Pooler

Password hashing via bcrypt (for local auth module)

Build

.NET (WPF).

dotnet restore
dotnet build -c Release

Configuration (no secrets in repo)

Use env vars or a local file placed next to the executable (db_credentials.json, not tracked):

{
  "Host": "aws-0-eu-central-1.pooler.supabase.com",
  "Port": 5432,
  "User": "postgres.<instance-id>",
  "Password": "<set_via_env_or_local_file>",
  "Database": "postgres",
  "SslMode": "Require"
}


Recommended env vars:

APP_DB_HOST, APP_DB_PORT, APP_DB_USER, APP_DB_PASSWORD, APP_DB_NAME, APP_DB_SSLMODE


For IPv4 networks use Session Pooler host; for IPv6 you may use direct connection.

Security notes

Do not commit real credentials. Rotate any password that ever entered VCS history.

Use a limited DB role (least privilege) and RLS when applicable.

bcrypt for user passwords; never store plaintext.

Team & credits

Rinco (Team Lead, security & DB, rewrote DB connectivity layer, parsing/exports, charts, testing)

Štefan (UI/visuals, XAML, menus, data source picker)

Hanuš (architecture, data/UI wiring, filtering, PS integration, DB extraction, testing)

Lukáš (Security Alerts UI, auth menu, controls wiring, filters, testing)

Contributors:
@Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rinco) • @Just-Kurumi (Kurumi)

Roadmap (short)

Robust parsing (validate columns, skip bad lines, invariant dates)

Async DB I/O, timeouts, retry policy (transient)

DataGrid virtualization & paging for big logs

Central logging (Serilog/NLog), opt-in Npgsql tracing

Config loader priority: ENV → user config → prompt

License

MIT © 2025 Rinco (GitHub: @Rincodev, contact: jacenbo1226@gmail.com
)

Čeština
Co to je

Desktopová aplikace (WPF, .NET) pro analýzu logů Windows Firewallu:

filtrování dle IP, času, akce/typu

vizualizace: koláč (povoleno vs. blokováno), sloupce (top protokoly)

detekce podezřelé aktivity (heuristiky skenů/bruteforce)

export CSV a TXT reportů

volitelný upload do DB (PostgreSQL/Supabase)

Hlavní vlastnosti

Rychlé lokální parsování, tolerantní k formátu/komentářům

Grafy přes WPF Canvas (vektor, export PNG)

IPv4 připojení přes Supabase Session Pooler

Hesla uživatelů přes bcrypt

Build
dotnet restore
dotnet build -c Release

Konfigurace (bez tajných údajů v repu)

Lokální db_credentials.json (mimo VCS) nebo proměnné prostředí:

{ "Host":"aws-0-eu-central-1.pooler.supabase.com", "Port":5432, "User":"postgres.<instance-id>", "Password":"<ENV>", "Database":"postgres", "SslMode":"Require" }


Pro IPv4 sítě použij Session Pooler; pro IPv6 můžeš přímé připojení.

Bezpečnost

Skutečné přihlašovací údaje necommitovat; po úniku rotovat.

DB role s minimálními právy; RLS dle potřeby.

bcrypt; nikdy neukládat hesla v plaintextu.

Tým & zásluhy

Rinco (Teamlead, bezpečnost & DB, přepsání DB konektivity, parsování/exporty, grafy, testy)

Štefan (UI/XAML, menu, výběr zdroje dat)

Hanuš (architektura, propojení dat/UI, filtrování, PS integrace, DB extrakce, testy)

Lukáš (UI pro Security Alerts, auth menu, napojení ovládacích prvků, filtry, testy)

Přispěvatelé:
@Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rinco) • @Just-Kurumi (Kurumi)

Roadmap

Odolnější parsování, async DB I/O, logování, virtualizace Gridu

Licence

MIT © 2025 Rinco (GitHub: @Rincodev, contact: jacenbo1226@gmail.com
)

Русский
Что это

Desktop-приложение (WPF, .NET) для анализа логов Windows Firewall:

фильтры по IP/времени/типу

графики: круговая (allow/deny), столбцы (топ протоколов)

эвристики подозрительной активности

экспорт CSV/TXT

опционально — загрузка в БД (PostgreSQL/Supabase)

Особенности

Быстрый локальный парсер (устойчив к комментариям/пробелам)

Графика через WPF Canvas (PNG экспорт)

IPv4-совместимость через Supabase Session Pooler

Пароли — bcrypt

Сборка
dotnet restore
dotnet build -c Release

Конфигурация (без секретов в репозитории)

Локальный db_credentials.json (не коммитить) или ENV:

{ "Host":"aws-0-eu-central-1.pooler.supabase.com", "Port":5432, "User":"postgres.<instance-id>", "Password":"<ENV>", "Database":"postgres", "SslMode":"Require" }

Безопасность

Никогда не коммитить реальные пароли; скомпрометированный — ротировать.

Минимальные привилегии DB-роли; при необходимости RLS.

bcrypt; без хранения в открытом виде.

Команда и вклад

Rinco (тимлид; безопасность/БД; полная переработка логики подключения к БД; парсер/экспорты; графики; тестирование)

Штефан (UI/XAML; меню; выбор источника данных)

Гануш (архитектура; связка данных и UI; фильтрация; PowerShell интеграция; извлечение из БД; тесты)

Лукас (UI Security Alerts; логин/логаут; связь кнопок и функций; фильтры; тесты)

Контрибьюторы:
@Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rinco) • @Just-Kurumi (Kurumi)

План

Надёжный парсер, async DB, логирование, виртуализация таблиц

Лицензия

MIT © 2025 Rinco (GitHub: @Rincodev, contact: jacenbo1226@gmail.com
)

Українська
Що це

Desktop-застосунок (WPF, .NET) для аналізу логів Windows Firewall:

фільтри за IP/часом/типом

графіки: кругова (allow/deny), стовпці (топ протоколів)

евристики підозрілої активності

експорт CSV/TXT

опційно — завантаження в БД (PostgreSQL/Supabase)

Особливості

Швидкий локальний парсер, стійкий до коментарів/формату

Графіка через WPF Canvas (PNG)

IPv4-сумісність через Supabase Session Pooler

Паролі — bcrypt

Збірка
dotnet restore
dotnet build -c Release

Конфігурація (без секретів у репо)

Локальний db_credentials.json (не комітимо) або ENV:

{ "Host":"aws-0-eu-central-1.pooler.supabase.com", "Port":5432, "User":"postgres.<instance-id>", "Password":"<ENV>", "Database":"postgres", "SslMode":"Require" }

Безпека

Не зберігати паролі в репозиторії; скомпрометовані — ротувати.

Роль БД з мінімальними правами; RLS за потреби.

bcrypt; без зберігання у відкритому вигляді.

Команда та внесок

Rinco (тимлід; безпека/БД; повне переписування логіки підключення; парсер/експорти; графіки; тестування)

Štefan (UI/XAML; меню; вибір джерела даних)

Hanuš (архітектура; з’єднання даних і UI; фільтрація; PowerShell інтеграція; екстракція з БД; тести)

Lukáš (UI Security Alerts; логін/логаут; зв’язування кнопок; фільтри; тести)

Контриб’ютори:
@Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rinco) • @Just-Kurumi (Kurumi)

План

Надійний парсер, async DB, логування, віртуалізація таблиць

Ліцензія

MIT © 2025 Rinco (GitHub: @Rincodev, contact: jacenbo1226@gmail.com
)
