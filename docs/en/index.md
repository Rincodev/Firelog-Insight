# FireLog-Insight (English)

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
Do not commit real credentials. Use env vars or a local file next to the executable.

Env (recommended):

```APP_DB_HOST=aws-0-eu-central-1.pooler.supabase.com
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
## Security Notes
Secrets: never commit; rotate if leaked; .gitignore keeps db_credentials.json out of VCS.

DB: use a least-privilege role (not postgres); RLS if multi-tenant.

TLS: SslMode=Require (or VerifyFull if you validate CN/CA).

Passwords: bcrypt (no plaintext storage).

## Team & Contributors
Rinco — Team lead; security & DB; rewrote DB connectivity (migrated from hardcoded school DB); parsing & exports; charts; testing.

Štefan — UI/visuals; XAML; menus; data source picker.

Hanuš — Architecture; data↔UI wiring; filtering; PowerShell integration; DB extract; testing.

Lukáš — Security Alerts UI; auth menu; controls wiring; filters; testing.

Contributors: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rinco) • @Just-Kurumi (Kurumi)

## Roadmap
Robust parser (column validation, skip bad lines, invariant dates)

Async DB I/O; timeouts; transient retry

DataGrid virtualization & paging for large logs

Central logging (Serilog/NLog), optional Npgsql tracing

First-run config prompt (writes user-scope config)

Unit tests for parser/export

DPI-aware PNG export, better legends

## License
```
MIT © 2025 Rinco (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
