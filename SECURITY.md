# Безопасность DpiTray

## Чем отличаемся от «случайных сборок Flowseal»

- Исходники лаунчера открыты в этом репозитории.
- `winws` / WinDivert скачиваются из известных raw-URL при `build.bat` (см. `scripts/fetch-runtime.ps1`).
- **TgWsProxy** берётся только с официального релиза  
  https://github.com/Flowseal/tg-ws-proxy/releases  
  и проверяется по **SHA256** (`scripts/fetch-tgproxy.ps1`).
- Не используем сторонние «моднутые» архивы/EXE с неизвестных зеркал — там как раз часто подсовывают RAT.

## Что делать пользователю

1. Качай DpiTray только из своего GitHub: https://github.com/MajikkuQQ/DpiTray  
2. Не запускай чужие `general*.bat` / `service.bat` из непроверенных ZIP.  
3. TgWsProxy — отдельный пункт в трее; он не вшит в winws.

## Discord

Стратегии `Discord FIX` / `Discord ALT2` основаны на рабочих профилях Flowseal (SIMPLE FAKE ALT / ALT2), без лишних бинарников.
