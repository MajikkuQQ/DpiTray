# DpiTray

Трей-лаунчер для Windows: запускает `winws` с узкими hostlist-стратегиями (меньше пинга и поломок интернета).

## Запуск

```bat
build.bat
dist\START.bat
```

В `START.bat` одно меню: запуск / починить Discord / логи.  
В трее по умолчанию: **Рекомендуемая**.

## Стратегии

| Стратегия | Назначение |
|-----------|------------|
| **YouTube + Discord** | основная: рабочие профили YT и Discord в одной стратегии |
| Только YouTube | минимальный профиль |
| Discord FIX | только Discord (badseq) |
| Расширенная | много сайтов РФ-throttle (+можно вместе с TG); без voice UDP Discord |

## Telegram Proxy

В трее: **Telegram Proxy (TgWsProxy)** — официальный бинарь Flowseal с проверкой SHA256.  
Не смешивается с winws; в Telegram: MTProto `127.0.0.1:1443`.

## Безопасность

См. [SECURITY.md](SECURITY.md): только официальные релизы, без сторонних «сборок» с RAT.

## Почему меньше лагает

- только hostlist (без `ipset-all` на весь интернет)
- `--dpi-desync-cutoff` — desync только на рукопожатии
- меньше repeats
- без широкого `cloudflare.com` / game-filter

## Списки

- `lists/list-google.txt` — YouTube
- `lists/list-discord.txt` — Discord / CDN
- `lists/list-apps.txt` — StatLocker, Deadlock API, Deadlock Mod Manager, GameBanana
- `lists/list-extended.txt` — расширенный hostlist для стратегии «Расширенная»

Домены можно дописать в нужный `.txt` (для расширенной — в `list-extended.txt`).

## Discord update failed

1. `dist\СБРОС_DISCORD_CACHE.bat`
2. стратегия **Только Discord** → Старт
3. перезапуск Discord

Runtime: `C:\ProgramData\DpiTray`
