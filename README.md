# FireLog-Insight

[![License](https://img.shields.io/badge/license-MIT-informational)](./LICENSE)
![Platform](https://img.shields.io/badge/platform-Windows%20%7C%20WPF-blue)
![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet&logoColor=white)
[![Build](https://github.com/<OWNER>/<REPO>/actions/workflows/build.yml/badge.svg)](https://github.com/<OWNER>/<REPO>/actions/workflows/build.yml)

**Language:** [English](#english) • [Čeština](#cestina) • [Русский](#russian) • [Українська](#ukrainian)

---

## Table of Contents
- [English](#english)
- [Čeština](#cestina)
- [Русский](#russian)
- [Українська](#ukrainian)
- [Build](#build)
- [Configuration](#configuration)
- [Security Notes](#security-notes)
- [Team & Contributors](#team--contributors)
- [Roadmap](#roadmap)
- [License](#license)

---

<a id="english"></a>
## English

**Desktop tool (WPF, .NET) for Windows Firewall log analysis.**  
Filters by IP, time, action/type. Visualizes allowed vs blocked and top protocols. Detects suspicious patterns. Exports CSV/TXT. Optional DB upload (PostgreSQL via Supabase Session Pooler for IPv4).

**Key features**
- Fast local parsing (tolerant to comments/format quirks)
- Charts on WPF `Canvas` with PNG export
- IPv4-compatible DB via **Supabase Session Pooler**
- Password hashing with **bcrypt**

---

<a id="cestina"></a>
## Čeština

**Desktopová aplikace (WPF, .NET) pro analýzu logů Windows Firewallu.**  
Filtrování dle IP, času a akce/typu. Vizualizace povolených vs blokovaných a top protokolů. Detekce podezřelé aktivity. Export CSV/TXT. Volitelný upload do DB (PostgreSQL přes Supabase Session Pooler pro IPv4).

**Hlavní vlastnosti**
- Rychlé lokální parsování (tolerantní k komentářům/formátu)
- Grafy na WPF `Canvas` s exportem PNG
- IPv4 připojení přes **Supabase Session Pooler**
- Hesla uživatelů přes **bcrypt**

---

<a id="russian"></a>
## Русский

**Desktop-приложение (WPF, .NET) для анализа логов Windows Firewall.**  
Фильтры по IP/времени/типу. Визуализация allow/deny и топ протоколов. Детект подозрительных паттернов. Экспорт CSV/TXT. Опциональная загрузка в БД (PostgreSQL через Supabase Session Pooler для IPv4).

**Особенности**
- Быстрый локальный парсер (устойчив к комментариям/формату)
- Графики на WPF `Canvas` с экспортом PNG
- IPv4-совместимость через **Supabase Session Pooler**
- Пароли — **bcrypt**

---

<a id="ukrainian"></a>
## Українська

**Desktop-застосунок (WPF, .NET) для аналізу логів Windows Firewall.**  
Фільтри за IP/часом/типом. Візуалізація allow/deny та топ протоколів. Виявлення підозрілих патернів. Експорт CSV/TXT. Опційне завантаження в БД (PostgreSQL через Supabase Session Pooler для IPv4).

**Особливості**
- Швидкий локальний парсер (стійкий до коментарів/формату)
- Графіки на WPF `Canvas` з експортом PNG
- IPv4-сумісність через **Supabase Session Pooler**
- Паролі — **bcrypt**

---

## Build
```bash
dotnet restore
dotnet build -c Release
