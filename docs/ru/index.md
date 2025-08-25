# FireLog Insight — Документация (RU)

**Язык:** [Čeština](../cs/index.md) • [English](../en/index.md) • [Українська](../ua/index.md)

---

## Что такое FireLog Insight?
**FireLog Insight** — лёгкое настольное приложение на **WPF (.NET 8)** для парсинга и визуализации логов **Windows Firewall**.

- Загружайте `pfirewall.log` (или демо‑файл из `assets/demo`).
- Фильтруйте по **действию** (Allow/Drop), **IP**, **порту** и **времени**.
- Сортируйте по **действию**, **протоколу**, **IP**, **порту** и **времени**.
- Визуализируйте: **Pie** (Allowed vs Blocked) и **Protocol Distribution** (столбчатая).
- Экспортируйте отфильтрованные данные в **CSV**/**TXT** и текущую диаграмму в **PNG**.
- Опционально: режим **PostgreSQL** (через **Npgsql**) и “Remember me”.
- **Offline‑режим**, если БД не настроена.
- Структурированное логирование через **Serilog** (файл + Debug‑sink).

---

## Быстрый старт
1) **Получите сборку**  
• Portable‑релиз: [скачайте последнюю версию](https://github.com/Rincodev/FireLog-Insight/releases/latest) и распакуйте.  
• Из исходников:
```bash
dotnet restore
dotnet build -c Release
```

2) **(Опционально) Настройте базу данных** — см. **[Конфигурация](#конфигурация)**.

> [!CAUTION]
> Если пропустить этот шаг, приложение запустится в **Offline‑режиме**.

3) **Запустите приложение**  
• Portable: запустите `Start FireLog.cmd` (библиотеки остаются внутри папки `app/`).  
• Либо запустите `FireLog.exe` напрямую.

4) **Можно работать — загрузите логи и пользуйтесь FireLog Insight!**

> [!IMPORTANT]
> Если вы только что включили логирование Windows Firewall, новые записи могут появиться не сразу. Дайте системе несколько минут обычной сетевой активности (или перезагрузите компьютер), а затем попробуйте загрузить лог снова.

---

## Конфигурация
> [!TIP]
> Не коммитьте реальные секреты. Предпочитайте **переменные окружения**.  
> **Переменные окружения — это не файлы**: их задают в ОС или в стартовом скрипте.  
> **Portable‑сборка:** отредактируйте `Start FireLog.cmd`, чтобы задать переменные перед запуском.  
> **ИЛИ** положите `db_credentials.json` рядом с `FireLog.exe` в папку `app/`.  
> Локальный файл рядом с исполняемым поддерживается как резервный вариант.

### Переменные окружения (рекомендуется для CI/ops)
Задайте их на уровне пользователя/машины в Windows **или** добавьте в лончер:
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

### Локальный файл (fallback, не в VCS)
Положите `db_credentials.json` рядом с `FireLog.exe` (portable: `app\db_credentials.json`).
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

### Строка подключения (Npgsql)
```
Host={Host};Port={Port};Username={User};Password={Password};Database={Database};
SslMode={SslMode};Timeout=15;Command Timeout=30;Keepalive=60
```

> [!CAUTION]
> Десктоп‑приложения не могут полностью скрыть секреты. Используйте наименьшие привилегии и периодически ротируйте пароли.

### Remember me
- При включении создаётся `remember_token` в БД и локальный `credentials.json` рядом с исполняемым файлом.  
- При **Logout** токен удаляется из БД, локальный файл стирается.

---

## Руководство по интерфейсу
(Галерея скриншотов: `docs/screenshots/`)

### 1) Верхняя панель
- **Фильтры:** диапазон времени (From/To), **Action** (All/Allow/Drop), поля **IP содержит** и **Port содержит** (поиск по подстроке в источнике/назначении).  
- **Data Source** — *Local* или *Database*. Если БД не настроена, принудительно *Local* с подсказкой один раз.
- **Apply Filters / Reset Filters** — применить/сбросить фильтры.
- **Logout** — выход из PostgreSQL‑режима; над кнопкой отображается текущий пользователь.

### 2) Правая панель — Статистика и диаграммы
- **Статистика:** всего событий, Allowed, Blocked — по текущей выборке (с учётом фильтров).
- **Диаграммы:** переключатель между *Pie* (Allowed vs Blocked) и *Protocol Distribution* (по протоколам).
- **Enable Logs** — включение сбора логов Windows Firewall (если применяется в вашей конфигурации).
- **Extract Data** — извлекает данные из локального лога или БД.
- **Upload to DB** — выгружает текущие данные в базу.

> [!WARNING]
> Все диаграммы строятся по **текущей отфильтрованной** выборке.

### 3) Правая нижняя область — Security Alerts
- Индикаторы и сообщения о потенциально подозрительной активности.

### 4) Нижняя панель
- **Load demo data** — загрузка демо‑лога для проверки парсинга/фильтров/диаграмм.
- **Export reports** — экспорт таблицы в **CSV**/**TXT** с применёнными фильтрами.
- **Export chart** — экспорт текущей диаграммы в **PNG**.

### 5) Таблица
- Колонки: Timestamp, Action, Protocol, Source IP/Port, Destination IP/Port, User ID.
- Строки отражают активные фильтры.

> [!TIP]
> Нажмите на заголовок столбца для **сортировки** по возрастанию/убыванию (например, по Protocol, Time, Port, IP).  
> **Меняйте ширину колонок**, перетаскивая границы заголовков (как в Excel).  
> Двойной щелчок по границе может авто‑подогнать ширину (если поддерживается темой/стилем системы).

---

## Логирование (Serilog)
- Логи пишутся в `logs/firelog-.log` (помесячный/подневной роллинг, UTF‑8) и в Debug‑sink при разработке.
- Инициализация — в `App.xaml.cs`:

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
**Нужна ли база данных?**  
Нет. Без конфигурации БД приложение работает в **Offline‑режиме**.

**Где хранится конфигурация?**  
Через **переменные окружения** или локальный `db_credentials.json`. “Remember me” пишет локальный `credentials.json` и токен в БД; оба очищаются при **Logout**.

**Почему режим Database откатывается в Local?**  
Если конфигурация БД отсутствует или некорректна, приложение покажет подсказку и вернётся в **Local**.

**Какая БД поддерживается?**  
**PostgreSQL** через **Npgsql**. Ожидается таблица `users` с полями `id`, `username`, `password_hash`, `remember_token`.

---

## Команда и контрибьюторы
**Bohdan ——> @Rincodev**  
Лид, разработка и релиз; архитектура и безопасность; подключение/конфигурация БД; извлечение/экспорт данных; интеграция и слияние кода; связывание UI и логики; диаграммы; экспорт; стабилизация и тесты.

**Štefan ——> @Just-Kurumi**  
UI/UX; XAML‑макеты и визуальная тема; навигация/меню; таблицы/сетки.

**Hanuš ——> @Menk1l**  
Архитектура и связка данных с UI; фильтрация/поиск; интеграция PowerShell; локальная обработка логов; Security Alerts UI; парсер Windows Firewall; тестирование.

**Lukáš ——> @Tykanek**  
UI/UX; аутентификация (Login/Logout); связывание контролов и логики; фильтры по IP/времени; тестирование.

Контрибьюторы: @Menk1l (Hanuš Hart) • @Tykanek (Lukáš Elbl) • @Rincodev (Rincodev) • @Just-Kurumi (Kurumi)

---

## Лицензия
MIT — см. [LICENSE](../../LICENSE)

MIT © 2025 Rincodev (GitHub: @Rincodev, contact: jacenbo1226@gmail.com)
