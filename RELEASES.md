# Сборка и публикация релизов

## ПК (Windows)

### Сборка для распространения

Из папки с решением:

```
dotnet publish pc_csharp/RemoteMouse.PC/RemoteMouse.PC.csproj -c Release -r win-x64 --self-contained false -o publish/pc
```

Или без publish, только сборка:

```
dotnet build pc_csharp/RemoteMouse.PC/RemoteMouse.PC.csproj -c Release
```

Результат будет в `pc_csharp/RemoteMouse.PC/bin/Release/net8.0-windows/` или в `publish/pc/`.

### Что класть в ZIP для GitHub Release

В архив для скачивания включите содержимое папки сборки:

- RemoteMouse.PC.exe
- Все .dll из этой же папки
- Папка runtimes/ (если есть)
- photo1.png, photo2.png, photo3.png (для окна «Начало работы»)
- icon.jpg или icon.png (для иконки в трее и окнах, если используете)

Иконка приложения уже вшита в exe через ApplicationIcon в проекте. Остальные файлы должны лежать рядом с exe.

### Установщик (Setup Wizard) и путь C:\Program Files (x86)\Remote

**Какую папку поместить в установщик**

Папка, которую нужно упаковать в установщик и развернуть в **C:\Program Files (x86)\Remote**:

- После обычной сборки: **pc_csharp\RemoteMouse.PC\bin\Release\net8.0-windows\** (все файлы и подпапки из неё).
- После publish: содержимое указанной в `-o` папки (например **publish\pc\**).

В эту папку входят: RemoteMouse.PC.exe, все .dll, папка runtimes (если есть), photo1.png, photo2.png, photo3.png. Иконка приложения уже встроена в exe при сборке (если в корне репозитория есть файл иконки, указанный в .csproj).

В настройках установщика укажите каталог установки: **C:\Program Files (x86)\Remote**. Установщик должен копировать всё содержимое выбранной папки сборки в этот каталог.

В коде нет жёстко прописанных путей к диску: конфиг хранится в `%UserProfile%\.remote_mouse\config.json`, путь к exe и к ресурсам берётся через `Environment.ProcessPath` и `AppDomain.CurrentDomain.BaseDirectory`. После установки в Program Files (x86)\Remote приложение корректно находит себя и файлы рядом с exe.

---

## Телефон (Android)

### Сборка release APK

Из корня репозитория:

```
cd android_compose
./gradlew assembleRelease
```

На Windows:

```
cd android_compose
gradlew.bat assembleRelease
```

APK будет в `android_compose/app/build/outputs/apk/release/app-release.apk`.

Для подписи перед публикацией настройте `signingConfigs` в `app/build.gradle.kts` и укажите этот конфиг в `buildTypes.release`.

### Что выкладывать на GitHub

- ПК: ZIP с содержимым папки сборки (exe + dll + runtimes + photo1/2/3 + при необходимости icon).
- Android: файл `app-release.apk` (или подписанный APK с другим именем).

В описании релиза укажите версию (например 1.0.0), для ПК — что нужна Windows и .NET 8 Desktop Runtime, если сборка не self-contained.
