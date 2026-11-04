# v0.1.10 — 2026-11-04

Preserve Google API destinations and TLS bytes through the user VPN. Added an upstream mock regression test.

Сохранены исходные адреса Google API и TLS-байты через VPN. Добавлен тест upstream-прокси.

# Changelog / История изменений

## 0.1.9 — 2026-11-04

- Explicit UTF-8 input encoding for the Framework compiler, fixing builds on non-Russian Windows. / Явная кодировка UTF-8 для компилятора Framework: исправлена сборка в Windows с другой системной кодировкой.
- Unicode shell-link API replaces WScript.Shell, fixing Cyrillic shortcut creation on English Windows. / Unicode API ярлыков заменяет WScript.Shell: исправлено создание русских ярлыков в английской Windows.

## 0.1.8 — 2026-11-04

- Added subtle **by skam** attribution. / Добавлена небольшая подпись **by skam**.
- Fixed desktop shortcut removal when disabled in settings. / Исправлено удаление собственного ярлыка при выключении соответствующей настройки.
- CLI checks respect stored consent and tunnel preference. / CLI-проверка учитывает разрешение и настройку TUN.
- Cleared stale shortcut history after Disable. / После отключения очищается устаревшая история ярлыков.
- Added complete RU/EN manuals, architecture notes and release-review limitations. / Добавлены RU/EN-руководства, описание архитектуры и ограничения ревью.
- First public repository release with Windows CI and checksums. / Первый публичный выпуск с Windows CI и контрольными суммами.

## Earlier local iterations / Предыдущие локальные версии

Local iterations introduced the C# relay, transactional shortcut setup, TLS-record preservation, isolated regression tests, compact UI, the dot-free icon and persistent RU/EN button. They were not published as releases in this repository.

В локальных версиях появились C#-ретранслятор, откат ярлыков, сохранность TLS-записей, изолированные тесты, компактный интерфейс, иконка без точки и кнопка RU/EN. Отдельными релизами этого репозитория они не публиковались.
