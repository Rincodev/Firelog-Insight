# FireLog-Insight (Українська)

**Мова:**  [English](../en/index.md) • [Čeština](../cs/index.md) • [Русский](../ru/index.md) 

## Огляд
Настільний застосунок (WPF, .NET) для аналізу журналів брандмауера Windows.
- Фільтри за IP, часом, дією/типом
- Візуалізації: секторна діаграма (дозволено vs заблоковано), стовпчикова (топ протоколів)
- Евристики підозрілої активності (напр., сканування/«brute-force»-подібний шум)
- Експорт CSV/TXT; опційне завантаження до БД (PostgreSQL через Supabase Session Pooler для IPv4)
- Графіки: малюються на WPF `Canvas` (PathGeometry/ArcSegment), експорт PNG через `RenderTargetBitmap`

## Збірка
```bash
dotnet restore
dotnet build -c Release
```

## Конфігурація
> [!IMPORTANT]
> - Не комітьте реальні облікові дані до репозиторію. Використовуйте змінні середовища або локальний файл поруч із виконуваним файлом.  
> - В IPv4-мережах використовуйте хост Session Pooler; в IPv6 можна пряме підключення.

**Змінні середовища (рекомендовано)**
```
APP_DB_HOST=aws-0-eu-central-1.pooler.supabase.com
APP_DB_PORT=5432
APP_DB_USER=postgres.<instance-id>
APP_DB_PASSWORD=<secret>
APP_DB_NAME=postgres
APP_DB_SSLMODE=Require
```

**Локальний файл (резервний варіант, поза контролем версій):** `db_credentials.json`
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

**Рядок підключення (Npgsql):**
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```

> [!CAUTION]
> Настільні застосунки не можуть повністю приховати секрети. Використовуйте ролі БД з мінімальними правами та змінюйте паролі у разі витоку.

## Команда та Учасники
**Bohdan ——> @Rincodev**  
Лід-команди, розробка та реліз; архітектура та безпека; підключення/конфіг БД; екстракція та експорт БД; інтеграція та злиття коду; зв’язування UI (дії/обробники); графіки; експорти; стабілізація та тести.

**Štefan ——> @Just-Kurumi**  
UI/UX; XAML-макети та візуальна тема; навігація/меню; таблиці/ґриди.

**Hanuš ——> @Menk1l**  
Архітектура та інтеграція дані↔UI; фільтрування/пошук; інтеграція PowerShell; локальна обробка логів; інтерфейс Security Alerts; парсер журналів брандмауера Windows; тестування.

**Lukáš ——> @Tykanek**  
UI/UX; Автентифікація (Login/Logout); зв’язування контролів→логіка; IP/часові фільтри; тестування.

Учасники: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rincodev) • @Just-Kurumi (Kurumi)

## Ліцензія
Ліцензовано за MIT — див. [LICENSE](../../LICENSE)
```
MIT © 2025 Rincodev (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
```
