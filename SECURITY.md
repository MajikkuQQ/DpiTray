# Безопасность

## Откуда лучше брать DpiTray

Скачивай только из официальных релизов этого репозитория:  
https://github.com/MajikkuQQ/DpiTray/releases

Исходники лаунчера открыты — можно собрать сам через `build.bat`.

## Что скачивается при сборке

- `winws` / WinDivert — через `scripts/fetch-runtime.ps1` из публичного репозитория с бинарниками
- TgWsProxy — только с официального релиза [Flowseal/tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy/releases), с проверкой SHA256 (`scripts/fetch-tgproxy.ps1`)

Не подкладывай в папку `bin` / `tgproxy` файлы из случайных архивов и «сборников» из чатов — бери то, что качает сама сборка, или сверяй хеши.

## Права администратора

WinDivert нужен для работы на уровне сети, поэтому DpiTray запрашивает права администратора. Это нормально для таких инструментов.

## Благодарности

Спасибо авторам [zapret](https://github.com/bol-van/zapret), [WinDivert](https://github.com/basil00/WinDivert), [zapret-discord-youtube](https://github.com/Flowseal/zapret-discord-youtube) и [tg-ws-proxy](https://github.com/Flowseal/tg-ws-proxy).
