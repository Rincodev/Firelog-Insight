# FireLog Insight — Documentation (EN)

**Language:** [Čeština](../cs/index.md) • [Русский](../ru/index.md) • [Українська](../ua/index.md)

---

## What is FireLog Insight?
**FireLog Insight** is a lightweight **WPF (.NET 8)** desktop tool for parsing and visualizing **Windows Firewall** logs.

- Load `pfirewall.log` (or the sample in `assets/demo`).
- Filter by **Action** (Allow/Drop), **IP**, **Port**, and **Time**.
- Sort by **Action** (Allow/Drop), **Protocol**, **IP**, **Port**, and **Time**.
- Visualize with **Pie** (Allowed vs Blocked) and **Protocol Distribution** (bar).
- Export filtered data to **CSV** and the current chart to **PNG**.
- Optional **PostgreSQL** mode (via **Npgsql**) with "Remember me".
- **Offline mode** when no DB is configured.
- Structured logging via **Serilog** (file + debug sinks).


---


## Quick start
1) **Get a build**
   
• Portable release: [Download the latest build](https://github.com/Rincodev/FireLog-Insight/releases/latest) and unzip.

• From source:
```bash
 dotnet restore
 dotnet build -c Release
```

2) **(Optional) Configure database** — see **[Configuration](#configuration)**.

> [!CAUTION]
> If you skip this, the app starts in **Offline mode**.

3) **Run the app**  
• Portable: run `Start FireLog.cmd` (keeps DLLs inside the `app/` folder).  
• Or run `FireLog.exe` directly.

4) **You’re ready to go — load your logs and enjoy FireLog Insight!**

>[!IMPORTANT]
>If you’ve just enabled Windows Firewall logging, new entries may not appear immediately. Give it a few minutes of normal network activity (or restart the machine) and then try loading the log again.


---


## Configuration
> [!WARNING]
> **Portable build:** place `db_credentials.json` next to `FireLog.exe` in the `app/` folder **OR** edit `Start FireLog.cmd` to set environment variables before launching the app.

> **Environment variables are not files** — they must be set in the OS or in your launcher script. 

> Do **not** commit real credentials. Prefer **environment variables**. A local `db_credentials.json` next to the executable is supported as a fallback.

### Environment variables (recommended for CI/ops)
- Set them at the user/machine level in Windows, **or** edit the launcher:
  ```bat
  @echo off
  set APP_DB_HOST=your-host
  set APP_DB_PORT=5432
  set APP_DB_USER=firelog_app
  set APP_DB_PASSWORD=***secret***
  set APP_DB_NAME=firelog
  set APP_DB_SSLMODE=Require
  start "" "FireLog.exe"

### Local file (fallback, not tracked): 
- Put `db_credentials.json` next to `FireLog.exe` (portable: `app\db_credentials.json`).
- 
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

### Connection string (Npgsql)
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```

> [!CAUTION]
> Desktop apps cannot fully hide secrets. Use least‑privilege roles and rotate passwords if they might be exposed.

### Remember me
- When checked, the app creates a per‑user `remember_token` in the DB and writes a local `credentials.json` next to the executable.  
- On **Logout**, the token is cleared from DB and the local file is deleted.

---

## User interface guide
(Screenshot gallery: `docs/screenshots/`)

### 1) Top bar / actions
- **Open Log** — choose `pfirewall.log` to load entries.
- **Export CSV** — saves the **current filtered table** to CSV.
- **Export Chart (PNG)** — saves the **visible chart** (pie/bar) to PNG.
- **Data Source** — *Local* vs *Database*. If DB is not configured, the app forces *Local* and shows a tip once.
- **Login / Logout** — PostgreSQL authentication; supports "Remember me".

### 2) Filters panel
- **Time range** — From / To.
- **Action** — All / Allow / Drop (also matches “Block”).
- **IP contains** — substring match against Source or Destination IP.
- **Port contains** — substring match against Source or Destination Port.
- **Apply / Clear** — run or reset filters.


### 3) Table
- Columns: Timestamp, Action, Protocol, Source IP/Port, Destination IP/Port, User ID.
- Rows reflect the active filters.
  
> [!TIP]
> Click any column header to **sort** ascending/descending (e.g., by Protocol, Time, Port, IP).
> **Resize columns** by dragging the header borders (Excel-style).
> Double-click a header border to auto-fit width (if enabled by your system theme).


### 4) Charts
- **Pie** — Allowed vs Blocked.
- **Protocol Distribution** — bar chart of protocol counts.
- Switch charts using the chart selector.
  
> [!CAUTION]
> All charts are computed from the **currently filtered** entries.


### 5) Export
- **CSV** — exports all rows **after filters** are applied.
- **TXT** — exports all rows **after filters** in plain text format.
- **PNG** — exports the current chart as an image.


### 6) Demo data
- Sample firewall log: `assets/demo/pfirewall_demo.log`.
- You can load the demo log via the **Load Demo** button in the app to verify parsing, filters, and charts without touching real logs.


---

## Logging (Serilog)
- Logs go to `logs/firelog-.log` (rolling daily, UTF‑8) and to the Debug sink during development.
- Startup configuration lives in `App.xaml.cs`:

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

## Demo data
- Sample firewall log: `assets/demo/pfirewall_demo.log`.
- Use it to verify parsing, filters, and charts without touching real logs.

---

## FAQ
**Is DB required?**  
No. Without DB config the app runs in **Offline** mode.

**Where are credentials stored?**  
From **env vars** or local `db_credentials.json`. "Remember me" writes a local `credentials.json` and a token in the DB; both are cleared on **Logout**.

**Why does Database mode revert to Local?**  
If DB config is missing or invalid, the app shows a tip and reverts to **Local**.

**Which DB is supported?**  
**PostgreSQL** via **Npgsql**. The expected `users` table contains `id`, `username`, `password_hash`, `remember_token`.

---

## License
Licensed under **MIT** — see [LICENSE](../../LICENSE).

---

## Credits
- **Bohdan — @Rincodev** — development & release; architecture & security; DB integration; UI wiring; charts; exports; stabilization & tests.
- **Štefan — @Just-Kurumi** — UI/UX; XAML layouts & theme; navigation/menus; grids.
- **Hanuš — @Menk1l** — architecture & data↔UI integration; filtering; PowerShell; log parser; Security Alerts; testing.
- **Lukáš — @Tykanek** — UI/UX; authentication; filters; testing.

Contributors: @Menk1l • @Tykanek • @Rincodev • @Just-Kurumi

