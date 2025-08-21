# FireLog-Insight (English)

**Language:** [Čeština](../cs/index.md) • [Русский](../ru/index.md) • [Українська](../uk/index.md)

## Overview
Desktop tool (WPF, .NET) for Windows Firewall log analysis.
- Filters by IP, time, action/type
- Visuals: pie (allowed vs blocked), bar (top protocols)
- Suspicious activity heuristics (e.g., scans/bruteforce-like noise)
- CSV/TXT export; optional DB upload (PostgreSQL via Supabase Session Pooler for IPv4)
- Charts: drawn on WPF `Canvas` (PathGeometry/ArcSegment), PNG export via `RenderTargetBitmap`

## Build
```bash
dotnet restore
dotnet build -c Release
```
## Configuration
[!IMPORTANT]

-Do not commit real credentials. Use environment variables or a local file next to the executable.

-On IPv4 networks use the Session Pooler host; on IPv6 you may use a direct connection.

-Environment variables (recommended)
```
APP_DB_HOST=aws-0-eu-central-1.pooler.supabase.com
APP_DB_PORT=5432
APP_DB_USER=postgres.<instance-id>
APP_DB_PASSWORD=<secret>
APP_DB_NAME=postgres
APP_DB_SSLMODE=Require
```
Local file (fallback, not tracked): db_credentials.json
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
Connection string (Npgsql):
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```
[!CAUTION]
Desktop apps cannot fully hide secrets. Use least-privilege DB roles and rotate passwords if leaked.

## Team & Contributors
Bohdan — @Rincodev

Lead, development & release; architecture & security; DB connect/config; DB extract & export; integration & code merging; UI wiring (actions/handlers); charts; exports; stabilization & tests.

Štefan
UI/UX; XAML layouts & visual theme; navigation/menus; tables/grids.

Hanuš — @Menk1l

Architecture & data↔UI integration; filtering/search; PowerShell integration; local log processing; Security Alerts UI; Windows Firewall log parser; testing.

Lukáš — @Tykanek

Authentication (Login/Logout); control→logic wiring; IP/time filters; testing.

Contributors: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rinco) • @Just-Kurumi (Kurumi)

## License
Licensed under MIT — see LICENSE
```
MIT © 2025 Rinco (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
