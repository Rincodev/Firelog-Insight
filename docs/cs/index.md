# FireLog-Insight (Čeština)

**Jazyk:** [English](../en/index.md) • [Русский](../ru/index.md) • [Українська](../uk/index.md)

## Přehled
Desktopový nástroj (WPF, .NET) pro analýzu logů Windows Firewallu.
- Filtry podle IP, času, akce/typu
- Vizualizace: koláčový (povoleno vs zablokováno), sloupcový (nejčastější protokoly)
- Heuristiky podezřelé aktivity (např. skeny/brute-force-podobný šum)
- Export CSV/TXT; volitelný upload do DB (PostgreSQL přes Supabase Session Pooler pro IPv4)
- Grafy: kreslené na WPF `Canvas` (PathGeometry/ArcSegment), export PNG přes `RenderTargetBitmap`

## Build
```bash
dotnet restore
dotnet build -c Release
```

## Konfigurace
> [!IMPORTANT]
> - Neukládejte reálné přihlašovací údaje do repozitáře. Použijte proměnné prostředí nebo lokální soubor vedle spustitelného souboru.  
> - Na IPv4 sítích používejte host Session Pooleru; na IPv6 lze použít přímé připojení.

**Proměnné prostředí (doporučeno)**
```
APP_DB_HOST=aws-0-eu-central-1.pooler.supabase.com
APP_DB_PORT=5432
APP_DB_USER=postgres.<instance-id>
APP_DB_PASSWORD=<secret>
APP_DB_NAME=postgres
APP_DB_SSLMODE=Require
```

**Lokální soubor (záloha, mimo verzování):** `db_credentials.json`
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

**Připojovací řetězec (Npgsql):**
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```

> [!CAUTION]
> Desktopové aplikace nedokážou tajné údaje zcela skrýt. Používejte DB role s minimálními oprávněními a při úniku hesel je rotujte.

## Tým & Přispěvatelé
**Bohdan ——> @Rincodev**  
Vedoucí týmu, vývoj & release; architektura & bezpečnost; připojení/konfigurace DB; extrakce & export DB; integrace & slučování kódu; napojení UI (akce/handlery); grafy; exporty; stabilizace & testy.

**Štefan ——> @Just-Kurumi**  
UI/UX; XAML rozvržení & vizuální téma; navigace/menu; tabulky/mřížky.

**Hanuš ——> @Menk1l**  
Architektura & integrace data↔UI; filtrování/vyhledávání; integrace PowerShell; lokální zpracování logů; UI bezpečnostních upozornění; parser logů Windows Firewallu; testování.

**Lukáš ——> @Tykanek**  
UI/UX; Autentizace (Login/Logout); propojení ovládacích prvků→logika; IP/časové filtry; testování.

Přispěvatelé: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rincodev) • @Just-Kurumi (Kurumi)

## Licence
Licencováno pod MIT — viz [LICENSE](../../LICENSE)
```
MIT © 2025 Rincodev (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
```
