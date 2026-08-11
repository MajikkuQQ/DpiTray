# DpiTray

Небольшой трей-лаунчер для Windows: помогает открывать привычные сервисы, когда сеть их режет или сильно тормозит.

Запускает `winws` с узкими списками доменов — без «лечения» всего интернета сразу. Так меньше шансов сломать игры и обычный пинг.

## Быстрый старт

```bat
build.bat
dist\START.bat
```

Или скачай готовый релиз, распакуй и запусти `START.bat` **от администратора**.

В трее выбери стратегию и нажми **Старт (zapret + TG)**.

## Стратегии

| Стратегия | Зачем |
|-----------|--------|
| **YouTube + Discord** | основная — оба сервиса в одном профиле |
| Только YouTube | если нужен только ролик |
| Discord | если глючит именно Discord |
| Расширенная | много сайтов, которые в РФ часто тормозят (без voice UDP Discord) |

Свои домены можно дописать в файлы в папке `lists`.

## Telegram

В трее есть пункт **Telegram (TgWsProxy)**. По умолчанию он стартует вместе с zapret.

В Telegram: **Настройки → Данные и память → Прокси** → MTProto `127.0.0.1:1443` (secret смотри в окне TgWsProxy).

## Если Discord пишет update failed

1. В `START.bat` выбери пункт про Discord / кэш  
2. Стратегия **Discord** → Старт  
3. Перезапусти Discord

Рабочие файлы лежат в `C:\ProgramData\DpiTray`.

## Безопасность

Качай DpiTray из [релизов этого репозитория](https://github.com/MajikkuQQ/DpiTray/releases).  
Подробности — в [SECURITY.md](SECURITY.md).

## Благодарности

DpiTray — просто удобная обёртка. Спасибо людям, без чьих проектов этого бы не было:

- **[bol-van](https://github.com/bol-van)** — автор [zapret](https://github.com/bol-van/zapret) (`winws` и идеи стратегий)
- **[basil00](https://github.com/basil00)** — автор [WinDivert](https://github.com/basil00/WinDivert)
- **[Flowseal](https://github.com/Flowseal)** — автор [zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) и [tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy)

Если DpiTray помог — поддержите и их.
