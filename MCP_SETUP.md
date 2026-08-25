# MCP Setup Guide - Unity Integration

**Дата**: 2026-08-25  
**Проект**: DarkTree FPS  
**Unity**: 2022.3.15f1

## Требования

### Обязательные компоненты

- **Node.js** >= 18.0.0
- **npm** >= 9.0.0
- **Unity Editor** 2022.3.15f1
- **.NET SDK** >= 6.0 (для Unity scripting)
- **Claude Code CLI** (для работы с MCP)
- **Git** (для версионирования)

### Опциональные компоненты

- **Blender** >= 3.6 (для конвертации анимаций)
- **Python** >= 3.10 (для Blender скриптов)
- **Visual Studio** или **VS Code** (для разработки)

## Установка

### Шаг 1: Создание MCP сервера

```bash
# Перейти в директорию проекта
cd d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022

# Создать папку для MCP сервера
mkdir mcp-server
cd mcp-server

# Инициализировать Node.js проект
npm init -y

# Установить зависимости
npm install @modelcontextprotocol/sdk express cors body-parser axios
npm install --save-dev typescript @types/node @types/express @types/cors tsx

# Создать конфигурацию TypeScript
npx tsc --init
```

### Шаг 2: Настройка Unity Editor Scripts

```bash
# Создать папку для Editor скриптов
cd "d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\Assets\DarkTree FPS\Scripts"
mkdir -p Editor/MCPBridge
```

Затем создать следующие C# скрипты в Unity:
- `MCPBridgeServer.cs` - HTTP сервер внутри Unity
- `UnityCommandExecutor.cs` - Обработчик команд
- `AnimationRetargeting.cs` - Система ретаргетинга

### Шаг 3: Настройка Claude Code для работы с MCP

Создать/обновить конфигурацию MCP в Claude Code:

```bash
# В корне проекта создать .claude папку если её нет
mkdir -p .claude

# Создать конфигурацию MCP
```

Файл `.claude/mcp_config.json`:
```json
{
  "mcpServers": {
    "unity-editor": {
      "command": "node",
      "args": ["d:/PROJECTS/DarkTreeFPS2022/lfhrnhbagc2022/mcp-server/dist/index.js"],
      "env": {
        "UNITY_PROJECT_PATH": "d:/PROJECTS/DarkTreeFPS2022/lfhrnhbagc2022",
        "UNITY_EDITOR_PORT": "7777"
      }
    }
  }
}
```

### Шаг 4: Настройка Blender интеграции (опционально)

```bash
# Создать папку для Blender скриптов
cd d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022
mkdir blender-scripts
```

## Конфигурация

### Unity Editor HTTP Bridge

В Unity Editor:
1. Открыть `Window → MCP Bridge → Settings`
2. Установить порт: `7777` (по умолчанию)
3. Включить автозапуск: `✓ Auto-start on Editor load`
4. Сохранить настройки

### MCP Server Environment Variables

Создать файл `mcp-server/.env`:

```env
# Unity Editor
UNITY_EDITOR_HOST=localhost
UNITY_EDITOR_PORT=7777
UNITY_PROJECT_PATH=d:/PROJECTS/DarkTreeFPS2022/lfhrnhbagc2022

# Blender (optional)
BLENDER_PATH=C:/Program Files/Blender Foundation/Blender 3.6/blender.exe
BLENDER_SCRIPTS_PATH=d:/PROJECTS/DarkTreeFPS2022/lfhrnhbagc2022/blender-scripts

# Claude API (optional для прямой интеграции)
# ANTHROPIC_API_KEY=your_api_key_here

# Logging
LOG_LEVEL=info
```

## Запуск

### Вариант 1: Ручной запуск

**Терминал 1 - Unity Editor:**
```bash
# Открыть Unity проект
# Unity автоматически запустит HTTP Bridge при загрузке
```

**Терминал 2 - MCP Server:**
```bash
cd d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server
npm run dev
```

**Терминал 3 - Claude Code:**
```bash
cd d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022
# Claude Code автоматически подключится к MCP серверу
```

### Вариант 2: Автоматический запуск

Создать скрипт `start-mcp.ps1`:

```powershell
# Запуск MCP окружения для Unity проекта

Write-Host "Starting MCP integration for Unity..." -ForegroundColor Green

# 1. Запустить Unity Editor (если не запущен)
$unityPath = "C:\Program Files\Unity\Hub\Editor\2022.3.15f1\Editor\Unity.exe"
$projectPath = "d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022"

if (-not (Get-Process Unity -ErrorAction SilentlyContinue)) {
    Write-Host "Starting Unity Editor..." -ForegroundColor Yellow
    Start-Process -FilePath $unityPath -ArgumentList "-projectPath `"$projectPath`""
    Start-Sleep -Seconds 10
}

# 2. Запустить MCP Server
Write-Host "Starting MCP Server..." -ForegroundColor Yellow
$mcpServerPath = "$projectPath\mcp-server"
Start-Process -FilePath "npm" -ArgumentList "run dev" -WorkingDirectory $mcpServerPath -NoNewWindow

Write-Host "MCP Integration started!" -ForegroundColor Green
Write-Host "Unity Editor Port: 7777" -ForegroundColor Cyan
Write-Host "MCP Server: Running" -ForegroundColor Cyan
```

Запустить:
```powershell
.\start-mcp.ps1
```

## Проверка установки

### Тест 1: Unity Bridge

В Claude Code выполнить:
```
Test Unity connection
```

Ожидаемый результат:
```
✓ Unity Editor HTTP Bridge: Connected
✓ Port: 7777
✓ Unity Version: 2022.3.15f1
✓ Project: DarkTree FPS
```

### Тест 2: MCP Tools

Проверить доступные инструменты:
```
List available MCP tools for Unity
```

Должны быть доступны:
- `unity_execute_command`
- `unity_import_asset`
- `unity_get_scene_info`
- `unity_animation_retarget`

### Тест 3: Простая команда

Создать тестовый GameObject:
```
Create a test cube in Unity scene
```

Unity должен создать GameObject с именем "TestCube".

## Troubleshooting

### Проблема: MCP сервер не запускается

**Решение:**
```bash
# Проверить версию Node.js
node --version  # Должна быть >= 18

# Переустановить зависимости
cd mcp-server
rm -rf node_modules package-lock.json
npm install
```

### Проблема: Unity Bridge не отвечает

**Решение:**
1. Открыть Unity Console (Ctrl+Shift+C)
2. Проверить логи: `[MCPBridge] Server started on port 7777`
3. Если сервер не запустился:
   - `Window → MCP Bridge → Restart Server`
   - Проверить, не занят ли порт 7777

### Проблема: Claude Code не видит MCP инструменты

**Решение:**
```bash
# Проверить конфигурацию
cat .claude/mcp_config.json

# Перезапустить Claude Code session
# Или перезагрузить конфигурацию
```

### Проблема: Ошибки компиляции C# скриптов

**Решение:**
1. Проверить версию .NET в Unity: `Edit → Preferences → External Tools`
2. Убедиться, что используется `.NET Standard 2.1`
3. Reimport Editor скриптов: `Right-click → Reimport`

## Безопасность

### Важные замечания:

1. **Локальный доступ**: Unity Bridge слушает только `localhost:7777`
2. **Аутентификация**: В production добавить API ключи
3. **Файловые операции**: MCP имеет полный доступ к проекту
4. **Git ignore**: Добавить в `.gitignore`:
   ```
   mcp-server/node_modules/
   mcp-server/.env
   mcp-server/dist/
   .claude/mcp_config.json  # если содержит чувствительные данные
   ```

## Следующие шаги

После успешной установки:

1. Прочитать `MCP_API.md` - референс всех доступных команд
2. Изучить примеры в `MCP_EXAMPLES.md`
3. Начать работу с конвертацией анимаций (см. `ANIMATION_RETARGETING.md`)

## Поддержка

При возникновении проблем:
1. Проверить логи Unity Console
2. Проверить логи MCP сервера (`mcp-server/logs/`)
3. Создать issue в проектном трекере
4. Обсудить с командой

---

**Статус документа**: Актуален  
**Последнее обновление**: 2026-08-25  
**Автор**: AI Agent (Claude)
