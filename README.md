<p align="center">
  <img src="docs/assets/logo.png" alt="FireLog Insight logo" width="380">
</p>

<h1 align="center">FireLog Insight</h1>

<p align="center">
  Windows firewall log analytics • WPF • .NET 8
</p>

<p align="center">
  <a href="https://github.com/<owner>/<repo>/releases/latest">
    <img alt="version" src="https://img.shields.io/github/v/release/<owner>/<repo>?label=version">
  </a>
  <a href="LICENSE">
    <img alt="license" src="https://img.shields.io/badge/license-MIT-blue">
  </a>
  <img alt=".NET" src="https://img.shields.io/badge/.NET-8.0-512BD4">
  <img alt="WPF" src="https://img.shields.io/badge/WPF-Desktop-5C2D91">
  <img alt="platform" src="https://img.shields.io/badge/Windows-10%2F11-informational">
  <a href="https://github.com/<owner>/<repo>/releases">
    <img alt="downloads" src="https://img.shields.io/github/downloads/<owner>/<repo>/total?label=downloads">
  </a>
</p>

<!-- Languages -->
<p align="center">
  <a href="docs/en/index.md"><img src="https://img.shields.io/badge/English-0A84FF?style=for-the-badge"></a>
  <a href="docs/ru/index.md"><img src="https://img.shields.io/badge/Русский-1F6FEB?style=for-the-badge"></a>
  <a href="docs/cs/index.md"><img src="https://img.shields.io/badge/Čeština-8E8CD8?style=for-the-badge"></a>
  <a href="docs/ua/index.md"><img src="https://img.shields.io/badge/Українська-FFD500?style=for-the-badge"></a>
</p>

<!-- Big CTAs -->
<p align="center">
  <a href="#installation">
    <img src="https://img.shields.io/badge/Get%20Started-%F0%9F%9A%80-4CAF50?style=for-the-badge" alt="Get Started">
  </a>
  <a href="docs/en/index.md">
    <img src="https://img.shields.io/badge/Read%20the%20Docs-%F0%9F%93%98-1976D2?style=for-the-badge" alt="Read the Docs">
  </a>
  <a href="https://github.com/<owner>/<repo>/releases/latest">
    <img src="https://img.shields.io/badge/Download-%F0%9F%93%A6-FF9800?style=for-the-badge" alt="Download">
  </a>
</p>

---

> **Welcome!** FireLog Insight is a lightweight desktop tool for parsing and visualizing **Windows Firewall** logs.  
> Import `pfirewall.log`, filter by Action/Protocol/IP/Port, explore charts (Pie & Protocol Distribution), export PNG/CSV.  
> Optional DB-backed login with **“Remember me”**. **Offline mode** if DB is not configured. Built-in **Serilog** logging.

---

## Table of contents

- [Features](#features)
- [Screenshots](#screenshots)
- [Installation](#installation)
- [Usage](#usage)
- [Configuration (DB, logging)](#configuration-db-logging)
- [Security & privacy](#security--privacy)
- [Troubleshooting](#troubleshooting)
- [Build from source](#build-from-source)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Links](#links)

## Features

- 🔍 Parse **Windows Firewall** log (`pfirewall.log`)
- 🎛️ Filters: **Action**, **Protocol**, **Source/Destination IP**, **Ports**, **Time**
- 📊 Charts: **Pie** (Allow/Drop), **Protocol distribution**
- 💾 Export: **PNG** (charts), **CSV** (data + stats)
- 📴 **Offline mode** (no DB → app still works)
- 🔐 Login with **BCrypt** password hash and **Remember me** token
- 🧾 **Serilog**: file + debug output
- 🌐 English UI, docs in **EN / RU / CS / UA**

## Screenshots

> Add your images to `docs/screenshots/` and update paths below.

<p align="center">
  <img src="docs/screenshots/main.png" alt="Main window" width="800"><br/>
  <em>Main view with filters</em>
</p>

<p align="center">
  <img src="docs/screenshots/charts.png" alt="Charts" width="800"><br/>
  <em>Pie / Protocol Distribution</em>
</p>

## Installation

### Option A — Portable (recommended for users)
1. Download the latest **Portable ZIP** from the [Releases](https://github.com/<owner>/<repo>/releases/latest) page.  
2. Unzip. Folder layout:
