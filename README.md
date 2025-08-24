# FireLog-Insight

[![License](https://img.shields.io/badge/license-MIT-informational)](./LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20WPF-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
[![Build](https://github.com/<OWNER>/<REPO>/actions/workflows/build.yml/badge.svg)](https://github.com/<OWNER>/<REPO>/actions/workflows/build.yml)

**Language hub:** [English](docs/en/index.md) • [Čeština](docs/cs/index.md) • [Русский](docs/ru/index.md) • [Українська](docs/uk/index.md)

---

> Start here: choose your language above.  
> Full docs live under `/docs/<lang>/index.md`.
# FireLog Insight

> Lightweight Windows app to parse, explore and export Windows Firewall logs. Clean UI, filters, charts, CSV/PDF export.

[![.NET](https://img.shields.io/badge/.NET-8.0-blue)](#)
[![Platform](https://img.shields.io/badge/Platform-Windows%2010/11%20x64-informational)](#)
[![License](https://img.shields.io/badge/License-MIT-success)](LICENSE)

---

## ✨ Features

- Parse large Windows Firewall logs (`pfirewall.log`)
- Fast filtering: action, protocol, IPs, ports, time
- Charts: **Pie** (Allow/Drop) and **Protocol distribution**
- Export **CSV** and chart **PNG**
- Optional **“Remember me”** (secure token)
- Built-in logging (Serilog)

<p align="center">
  <img src="docs/screenshots/main-window.png" width="720" alt="Main window"/>
</p>

---

## 🧩 How it works

- **Input**: standard firewall log (e.g. `C:\Windows\System32\LogFiles\Firewall\pfirewall.log`) or your demo in `assets/demo/pfirewall_demo.log`.
- **Parser** builds an in-memory model, UI applies filters and renders charts.
- **Export**: CSV rows + summary stats; chart to PNG.

---

## 🚀 Getting started

### Option A — Portable build (recommended for users)
1. Download the latest ZIP from **Releases**.
2. Unzip → `FireLog_Portable/`.
3. Copy `config/db_credentials.sample.json` → `app/db_credentials.json` and fill in:
   ```json
   {
     "Host": "db.example.com",
     "Port": 5432,
     "Database": "firelog",
     "Username": "firelog_app",
     "Password": "CHANGE_ME",
     "SslMode": "Require"
   }

