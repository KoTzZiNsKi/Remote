# Remote

Удалённое управление компьютером с телефона или планшета: мышь, клавиатура, буфер обмена, громкость и команды питания.

## Состав репозитория

- **pc_csharp** — приложение для Windows (WPF). Сервер в трее, настройки, пароль, IP и QR-код, обновления из GitHub.
- **android_app** — приложение для Android. Подключение по IP или QR, экран мыши, клавиатура, доп. функции.

Связь по локальной сети (TCP, при необходимости UDP для поиска). Телефон и ПК должны быть в одной сети.

## Сборка и релизы

Инструкции по сборке и выкладке релизов (exe для ПК, APK для телефона) — в [RELEASES.md](RELEASES.md).

## Публикация на GitHub

Репозиторий: https://github.com/KoTzZiNsKi/Remote

Если репозиторий ещё не создан на GitHub — создайте пустой репозиторий **Remote** в аккаунте KoTzZiNsKi (без README и .gitignore). Затем в папке проекта выполните:

```bash
git init
git add .
git commit -m "Initial commit"
git branch -M main
git remote add origin https://github.com/KoTzZiNsKi/Remote.git
git push -u origin main
```

Дальнейшие обновления:

```bash
git add .
git commit -m "описание изменений"
git push
```

Либо запустите **push_to_github.bat** из папки проекта (должен быть установлен Git и добавлен в PATH).

## Документация и FAQ

Описание возможностей и ответы на частые вопросы — в [pc_csharp/README.md](pc_csharp/README.md).
