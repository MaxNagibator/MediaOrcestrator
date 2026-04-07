# MediaOrcestrator

### Заливаешь видосы на 5 площадок руками? Серьёзно? В 2026?

[![Release](https://github.com/MaxNagibator/MediaOrcestrator/actions/workflows/release.yml/badge.svg)](https://github.com/MaxNagibator/MediaOrcestrator/actions/workflows/release.yml)
![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)
![WinForms](https://img.shields.io/badge/UI-WinForms%20💀-orange)
![LiteDB](https://img.shields.io/badge/DB-LiteDB-green)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Tests](https://img.shields.io/badge/tests-какие%20тесты%3F-red)

---

## Что это

Залил видео на ютуб. Теперь то же самое - на рутуб. Потом на вк. Потом в телегу. Заполнить описание, дождаться загрузки,
закрыть вкладку. Повторить. На каждой площадке свой логин, свой формат, свои приколы.

MediaOrcestrator берёт эту рутину на себя. Настраиваешь связи между площадками, жмёшь кнопку - сам качает, конвертит,
льёт и следит, чтобы метаданные не потерялись по дороге.

Написан на C#. WinForms. LiteDB в одном файле. Если хочется Electron с тремя гигами node_modules - это не сюда.

---

## Скриншоты

Скриншоты будут. Когда-нибудь. Наверное.
Такой шедевр не грех запустить и своими глазами увидеть.

<!-- TODO: вставить скриншоты -->
<!-- Главное окно с матрицей медиа по площадкам -->
<!-- Дерево связей синхронизации -->
<!-- Лог работы -->

---

## Что умеет

**Синхронизация**

Выстраиваешь цепочку: YouTube → HDD → RuTube → VK. Оркестратор сам разберётся в каком порядке что качать и куда лить.
Параллельно, не по одному - жись коротка.

**Медиа**

Метаданные подтягиваются автоматом - название, описание, превьюшка. Рутуб от VP9 нос воротит. Оркестратор проверит кодек
заранее, ffmpeg разрулит. Массовое переименование тоже есть, на случай если придумал более хайповое название.

**Инструменты**

yt-dlp, ffmpeg, deno - оркестратор сам проверяет версии через GitHub releases и обновляет. Не надо лазить по сайтам и
качать exe-шники ручками.

**Бэкапы**

LiteDB файл бэкапится при каждом запуске и потом каждые 6 часов. Потому что потерять базу со всеми связями - обидно, а
вспомнить потом что где лежало - нереально. Надёжнее страховки у Прапора.

**Автообновление**

Отдельный `Updater.exe` скачивает свежую версию, ждёт пока основной процесс закроется, делает бэкап, распаковывает,
запускает. Если что-то пошло не так - смотри `updater.log`.

---

## Площадки

| Площадка | Download | Upload | Авторизация                                                       |
|----------|:--------:|:------:|-------------------------------------------------------------------|
| YouTube  |    ✓     |   ✓    | OAuth 2.0 - гугл попросит доступ, один раз и навсегда             |
| RuTube   |    ✓     |   ✓    | Через yt-dlp. Кодек проверит перед заливкой, не волнуйся          |
| VK Video |    ✓     |   ✓    | Сессия + cookie. Есть challenge solver, если вк решит, что ты бот |
| Telegram |    ✓     |   ✓    | Телефон + 2FA через WTelegramClient                               |
| HDD      |    ✓     |   ✓    | Ну это диск. Какая авторизация                                    |

---

## Быстрый старт

Понадобится [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0). Без него никуда.

Собрать:

```bash
dotnet build MediaOrcestrator.sln
```

Запустить:

```bash
dotnet run --project MediaOrcestrator.Runner
```

Первый запуск спросит путь к плагинам и базе. По умолчанию `ModuleBuilds` и `MyData.db`. Жмякай далее.

Собрать релиз:

```bash
dotnet publish MediaOrcestrator.Runner -r win-x64 --self-contained false
dotnet publish MediaOrcestrator.Updater -r win-x64 --self-contained false
```

---

## Архитектура

```
MediaOrcestrator.sln
│
├── Domain            мозг: оркестрация, планирование, поиск плагинов
├── Runner            WinForms UI (да, WinForms, да, 2026, да, работает)
├── Modules           интерфейсы - контракт, по которому живут плагины
│
├── Youtube           OAuth + форк YoutubeExplode
├── Rutube            yt-dlp + кодек-валидация перед загрузкой
├── VkVideo           сессии, cookie, обходчик "я не робот, брат"
├── Telegram          WTelegramClient, телефон + 2FA
├── HardDiskDrive     просто файлы на диске. зато надёжно
│
├── Updater           автообновлятор, отдельный exe
└── Other/            форк YoutubeExplode - лежит прямо в репе
```

Плагины - это DLL-ки в папке `ModuleBuilds/`. При старте `InterfaceScanner` проходит по ним reflection-ом и находит
всех, кто реализует `ISourceType`. Новая площадка = новая DLL в папку. Без регистрации и SMS.

---

## Стек

| Что      | Чем                                            |
|----------|------------------------------------------------|
| Язык     | C# 12                                          |
| Рантайм  | .NET 8                                         |
| UI       | WinForms                                       |
| База     | LiteDB                                         |
| Логи     | Serilog (консоль + файл + RichTextBox в UI)    |
| YouTube  | Google.Apis.YouTube.v3 + YoutubeExplode (форк) |
| Telegram | WTelegramClient                                |
| Видео    | ffmpeg, yt-dlp                                 |
| Браузер  | Playwright (для OAuth)                         |
| CI/CD    | GitHub Actions                                 |

---

## Релизы

Пушишь тег `v*` или мержишь в master:

1. GitHub Actions собирает Runner и Updater через `dotnet publish`
2. Плагины подтягиваются из `ModuleBuilds/`
3. Всё пакуется в 7z
4. Создаётся GitHub Release

Версия в `Directory.Build.props`. Бампни перед релизом, иначе перезатрёшь предыдущий.

Скачать готовое без сборки: [GitHub Releases](https://github.com/MaxNagibator/MediaOrcestrator/releases).

---

## Чего не умеет

- Только Windows. Linux и Mac - как-нибудь потом. Или никогда
- Автосинхронизация по расписанию отключена. Только руками, через кнопку
- Тестов нет. Работает - не трогай

---

## FAQ

**Почему WinForms?**

Потому что работает. Следующий вопрос.

**А если всё сломается?**

Бэкапы каждые 6 часов + при запуске. Расслабься.

**Можно добавить свою площадку?**

DLL в папку - и вперёд. `ISourceType` тебе в помощь.

**А почему не микросервисы?**

Потому что это десктоп, а не собеседование.

---

*Собрано на коленке, работает как часы.*
