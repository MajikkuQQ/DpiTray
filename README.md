# DpiTray

Удобный трей-лаунчер для Windows: запускает `winws` со готовыми стратегиями обхода DPI (YouTube, Discord, общие сайты), ставит WinDivert при первом запуске, сохраняет настройки и умеет автозагрузку.

## Быстрый старт

```bat
build.bat
```

Готовый файл: `dist\DpiTray.exe`

Запустите exe от администратора (UAC), ПКМ по иконке в трее:

- стратегии
- Старт / Стоп
- Автозапуск с Windows
- Выход

## Что делает build.bat

1. Ставит .NET 8 SDK при необходимости
2. Скачивает runtime (`winws`, `cygwin1.dll`, WinDivert, payload `*.bin`) в `payload\bin`
3. Собирает self-contained single-file `DpiTray.exe`
4. Кладёт exe + `bin` / `lists` / `strategies` в `dist\`

## Структура

```
DpiTray/
  build.bat
  scripts/fetch-runtime.ps1
  src/                     — исходники лаунчера
  payload/
    bin/                   — runtime (скачивается автоматически)
    lists/                 — списки доменов
    strategies/            — JSON-стратегии для winws
  dist/                    — готовая сборка
```

## Лицензия / благодарности

Лаунчер DpiTray — отдельный проект.
Движок обхода DPI использует `winws` / WinDivert и идеи экосистемы zapret.
