# FireLog Insight — Документація (UA)

**Мова:** [Čeština](../cs/index.md) • [Русский](../ru/index.md) • [English](../en/index.md)

---

## Що таке FireLog Insight?
**FireLog Insight** — легкий **WPF (.NET 8)** настільний застосунок для парсингу та візуалізації логів **Брандмауера Windows**.

- Завантажує `pfirewall.log` (або зразок у `assets/demo`).
- Фільтрація за **Action** (Allow/Drop), **IP**, **Port** та **Time**.
- Сортування за **Action** (Allow/Drop), **Protocol**, **IP**, **Port** та **Time**.
- Візуалізація: **Pie** (Allowed vs Blocked) і **Protocol Distribution** (стовпчаста).
- Експорт відфільтрованих даних у **CSV** і поточної діаграми у **PNG**.
- Необов’язковий режим **PostgreSQL** (через **Npgsql**) з "Remember me".
- **Офлайн-режим**, якщо БД не налаштована.
- Структуроване логування через **Serilog** (файл + Debug).

---

## Швидкий старт
1) **Отримати збірку**
   
• Портативний реліз: [Download the latest build](https://github.com/Rincodev/FireLog-Insight/releases/latest) і розпакуйте.

• Із вихідних кодів:
```bash
 dotnet restore
 dotnet build -c Release
```

2) **(Необов’язково) Налаштувати базу даних** — див. **[Configuration](#configuration)**.

> [!CAUTION]
> Якщо пропустити цей крок, застосунок запуститься в **офлайн-режимі**.

3) **Запуск застосунку**  
• Портативна збірка: запустіть `Start FireLog.cmd` (залишає DLL у теці `app/`).  
• Або запустіть `FireLog.exe` напряму.

4) **Готово — завантажуйте логи та користуйтеся FireLog Insight!**

> [!IMPORTANT]
> Якщо ви щойно увімкнули логування Брандмауера Windows, нові записи можуть з’явитися не відразу. Дайте системі кілька хвилин звичайної мережевої активності (або перезавантажте комп’ютер), потім спробуйте завантажити лог знову.

---

## Configuration
> [!TIP]
> **Не** комітьте реальні облікові дані.
> Надавайте перевагу **змінним середовища**.
>
> **Змінні середовища — не файли**: їх потрібно задавати в ОС або у скрипті запуску.
>
> **Портативна збірка:** відредагуйте `Start FireLog.cmd`, щоб задати змінні середовища перед запуском застосунку.
>
> **АБО**
>
> розмістіть `db_credentials.json` поряд із `FireLog.exe` у теці `app/`
>
> Локальний `db_credentials.json` поруч із виконуваним файлом підтримується як запасний варіант.

### Змінні середовища (рекомендується для CI/ops)
- Задайте їх на рівні користувача/машини у Windows **або** відредагуйте скрипт запуску:
  ```bat
  @echo off
  set APP_DB_HOST=your-host
  set APP_DB_PORT=5432
  set APP_DB_USER=firelog_app
  set APP_DB_PASSWORD=***secret***
  set APP_DB_NAME=firelog
  set APP_DB_SSLMODE=Require
  start "" "FireLog.exe"
  ```

### Локальний файл (fallback, не відстежується):
- Помістіть `db_credentials.json` поруч із `FireLog.exe` (портативна: `app\db_credentials.json`).

```json
{
  "Host": "aws-0-eu-central-1.pooler.supabase.com",
  "Port": 5432,
  "User": "postgres.<instance-id>",
  "Password": "<SET_VIA_ENV_OR_LOCAL_FILE>",
  "Database": "postgres",
  "SslMode": "Require"
}
```

### Рядок підключення (Npgsql)
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```

> [!CAUTION]
> Настільні застосунки не можуть повністю приховати секрети. Використовуйте ролі з мінімальними привілеями та ротуруйте паролі у разі можливої витікання.

### Remember me
- Під час увімкнення створюється користувацький `remember_token` у БД і локальний `credentials.json` поряд із виконуваним файлом.  
- Під час **Logout** токен у БД очищується, локальний файл видаляється.

---

## Посібник з інтерфейсу
(Галерея скріншотів: `docs/screenshots/`)

<p align="center">
  <img src="../screenshots/ui_overview.png" alt="FireLog Insight — огляд інтерфейсу" width="720">
</p>

### Швидка навігація [Верхня область](#ui-top) • [Таблиця](#ui-table) • [Права панель (статистика та графіки)](#ui-right) • [Security Alerts](#ui-alerts) • [Нижні дії](#ui-bottom)

---

<a id="ui-top"></a>
### Верхня область — фільтри та сесія
<p>
  <img src="../screenshots/ui_top_area.png" alt="Верхня область — фільтри, Data Source, блок користувача, apply/reset" width="800">
</p>

- **Filters**
  - **Time range** — *From* / *To*.
  - **Action** — *All* / *Allow* / *Drop* (також співставляється “Block”).
  - **IP contains** — підрядковий пошук за **Source IP** і **Destination IP**.
  - **Port contains** — підрядковий пошук за **Source Port** і **Destination Port**.
  - **Apply Filters** / **Reset Filters** — застосувати або скинути набір фільтрів.

- **Data Source** — *Local* або *Database*.  
  Якщо БД не налаштована/недоступна, застосунок лишається в **Local** (одноразово буде показано невелику підказку).

- **User box + Logout**
  - У блоці користувача відображається поточна ідентичність (наприклад, **User: Offline** або ім’я увійшлого користувача).
  - **Logout** очищує активну сесію; **кнопки “Login” тут немає**.

---

<a id="ui-table"></a>
### Таблиця — список подій (центр)
<p>
  <img src="../screenshots/ui_table.png" alt="Таблиця подій — сортувальні заголовки, змінювана ширина стовпців" width="600">
</p>

- Стовпці: **Time**, **Action**, **Protocol**, **Source IP/Port**, **Destination IP/Port**.
- Таблиця завжди відображає **активні фільтри**.

> [!TIP]
> Клік по заголовку стовпця — **сортування** за зростанням/спаданням (наприклад, за **Protocol**, **Time**, **Port**, **IP**).
> 
> **Змінюйте ширину** стовпців перетягуванням меж заголовків (як у Excel).
> 
> Подвійний клік по межі заголовка — авто-підбір ширини (якщо підтримується темою ОС).

---

<a id="ui-right"></a>
### Права панель (статистика та графіки)
<p>
  <img src="../screenshots/ui_right_panel.png" alt="Права панель — вибір графіка, лічильники, дії" width="380">
</p>

- **Chart selector** — `Pie Chart` / `Protocol Distribution`.  
- **Pie** — Allowed vs Blocked.  
- **Protocol Distribution** — стовпчаста діаграма розподілу протоколів.  
- **Totals** — швидкі лічильники **Total**, **Allowed**, **Blocked**.  

**Actions**
- **Enable Logs** — помічник для увімкнення логування Брандмауера Windows (якщо доступно).
- **Extract Data** — витягує логи з **поточного джерела** (*Local* або *Database*).
- **Upload to DB** — надсилає **поточні завантажені записи** в налаштовану БД PostgreSQL.

> [!IMPORTANT]
> Діаграми та лічильники рахуються з **поточного відфільтрованого** набору записів.

---

<a id="ui-alerts"></a>
### Security Alerts
<p>
  <img src="../screenshots/ui_alerts.png" alt="Security Alerts — підозрілі порти та нетиповий вихідний трафік" width="450">
</p>

- Евристики, що підсвічують **підозрілі порти** та **нетиповий вихідний трафік**.  
- Використовуйте як орієнтири для ручної перевірки; це не заміна IDS/IPS.

---

<a id="ui-bottom"></a>
### Нижні дії
<p>
  <img src="../screenshots/ui_bottom_actions.png" alt="Нижні дії — завантаження демо, експорт звітів (CSV/TXT), експорт діаграми" width="360">
</p>

- **Load Demo Data** — завантажує `assets/demo/pfirewall_demo.log` одним кліком (зручно для перевірки парсингу/фільтрів/графіків).
- **Export Reports** — експорт таблиці з урахуванням фільтрів у **CSV** та **TXT**.
- **Export Chart** — зберігає **поточну діаграму** як **PNG**.

---

## Логування (Serilog)
- Журнали пишуться у `logs/firelog-.log` (щоденне обертання, UTF-8) і в Debug-sink під час розробки.
- Конфігурація старту знаходиться у `App.xaml.cs`:

```csharp
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .Enrich.FromLogContext()
    .WriteTo.Debug()
    .WriteTo.File(
        path: Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "firelog-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 7,
        encoding: Encoding.UTF8)
    .CreateLogger();
```

---

## FAQ
**Чи потрібна БД?**  
Ні. Без налаштування БД застосунок працює в **офлайн-режимі**.

**Де зберігаються облікові дані?**  
Із **змінних середовища** або локального `db_credentials.json`. При "Remember me" створюється локальний `credentials.json` і токен у БД; обидва очищуються під час **Logout**.

**Чому режим Database повертається в Local?**  
Якщо конфігурації БД немає або вона некоректна, застосунок показує підказку і повертається в **Local**.

**Яку БД підтримано?**  
**PostgreSQL** через **Npgsql**. Очікується таблиця `users` з полями `id`, `username`, `password_hash`, `remember_token`.

---

## Команда та учасники

**Bohdan ——> @Rincodev** 

Тімлід, розробка і реліз; архітектура та безпека; підключення/конфіг БД; витяг/експорт у БД; інтеграція та злиття коду; зв’язка UI (дії/обробники); діаграми; експорти; стабілізація і тести. 

**Štefan ——> @Just-Kurumi** 

UI/UX; XAML-макети і візуальна тема; навігація/меню; таблиці/сітки. 

**Hanuš ——> @Menk1l** 

Архітектура і інтеграція дані↔UI; фільтрація/пошук; інтеграція PowerShell; локальна обробка логів; UI Security Alerts; парсер логів Брандмауера Windows; тестування. 

**Lukáš ——> @Tykanek** 

UI/UX; Автентифікація (Login/Logout); зв’язка контролів з логікою; фільтри IP/часу; тестування. 

Учасники: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rincodev) • @Just-Kurumi (Kurumi)

---

## Ліцензія 
Ліцензія MIT — див. [LICENSE](../../LICENSE)

MIT © 2025 Rincodev (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
