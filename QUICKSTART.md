# Quick Start Guide - MCP Integration

**Дата**: 2026-08-25  
**Время**: ~5-10 минут

## Быстрый старт за 5 минут

### Шаг 1: Установка зависимостей (2 мин)

```bash
# Перейти в папку MCP сервера
cd "d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server"

# Установить Node.js зависимости
npm install

# Скопировать файл настроек
copy .env.example .env
```

### Шаг 2: Запуск Unity Editor (1 мин)

1. Открыть проект в Unity 2022.3.15f1
2. Дождаться компиляции скриптов
3. В Console должно появиться: `[MCPBridge] Server started on port 7777`

Если сервер не запустился автоматически:
- `Window → MCP Bridge → Start Server`

### Шаг 3: Запуск MCP Server (1 мин)

В отдельном терминале:

```bash
cd "d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server"
npm run dev
```

Должно появиться:
```
Unity MCP Server running on stdio
Unity Editor Bridge: localhost:7777
```

### Шаг 4: Настройка Claude Code (1 мин)

Создать файл `.claude/mcp_config.json` в корне проекта:

```bash
mkdir .claude
```

Содержимое `.claude/mcp_config.json`:
```json
{
  "mcpServers": {
    "unity-editor": {
      "command": "node",
      "args": ["mcp-server/src/index.ts"],
      "env": {
        "UNITY_PROJECT_PATH": "d:/PROJECTS/DarkTreeFPS2022/lfhrnhbagc2022",
        "UNITY_EDITOR_PORT": "7777"
      }
    }
  }
}
```

### Шаг 5: Тест (30 сек)

В Claude Code написать:
```
Check Unity Editor connection and create a test cube named "MCPTest"
```

Ожидаемый результат:
- ✅ Unity подключен
- ✅ Куб создан и виден в Hierarchy
- ✅ MCP интеграция работает!

## Проверочный список

После установки проверьте:

- [ ] Node.js >= 18 установлен (`node --version`)
- [ ] Unity Editor 2022.3.15f1 открыт
- [ ] MCP Bridge Server запущен (Unity Console: `[MCPBridge] Server started`)
- [ ] MCP Server запущен (`npm run dev` без ошибок)
- [ ] `.claude/mcp_config.json` создан
- [ ] Тестовая команда выполнена успешно

## Что дальше?

### Основные сценарии использования:

1. **Создание GameObjects**:
   ```
   Create a player spawn point at position (0, 1, 0)
   ```

2. **Анализ проекта**:
   ```
   Show me the structure of Assets/Scripts folder
   ```

3. **Импорт ассетов**:
   ```
   Import model from C:/Models/character.fbx into Assets/Characters/
   ```

4. **Конвертация анимаций**:
   ```
   Retarget Mixamo animation Assets/Anims/walk.anim to Mecanim
   ```

### Документация:

- 📖 **MCP_INTEGRATION.md** - Полная архитектура системы
- 📖 **MCP_SETUP.md** - Детальные инструкции по установке
- 📖 **MCP_API.md** - Референс всех доступных команд

### Работа с анимациями:

См. **ANIMATION_RETARGETING.md** (будет создан далее) для деталей по:
- AI-powered конвертации скелетов
- Интеграции с Blender
- Пользовательским маппингам костей

## Troubleshooting

### Unity Console: "Address already in use"

**Проблема**: Порт 7777 занят

**Решение**:
```
Window → MCP Bridge → Settings
Изменить порт на 7778
Restart Server
```

Обновить `.env` в `mcp-server/`:
```env
UNITY_EDITOR_PORT=7778
```

### MCP Server не видит Unity

**Проблема**: Connection refused

**Проверить**:
1. Unity Editor открыт?
2. В Unity Console есть `[MCPBridge] Server started`?
3. Порты совпадают в Unity и `.env`?

**Решение**:
```bash
# В Unity
Window → MCP Bridge → Restart Server

# Перезапустить MCP Server
# Ctrl+C в терминале, затем npm run dev
```

### Claude Code не видит MCP инструменты

**Проблема**: Инструменты unity_* не доступны

**Решение**:
1. Проверить `.claude/mcp_config.json` существует
2. Перезапустить Claude Code session
3. Проверить что MCP Server запущен

## Автоматизация запуска

Создать скрипт `start-mcp.ps1` в корне проекта:

```powershell
# Запуск MCP окружения
Write-Host "Starting MCP Integration..." -ForegroundColor Green

# 1. Запустить MCP Server
Start-Process powershell -ArgumentList "-NoExit", "-Command", "cd 'mcp-server'; npm run dev"

Write-Host "MCP Server started!" -ForegroundColor Green
Write-Host "Open Unity Editor manually and MCP Bridge will start automatically" -ForegroundColor Cyan
Write-Host "Then start Claude Code in this directory" -ForegroundColor Cyan
```

Запуск одной командой:
```powershell
.\start-mcp.ps1
```

## Полезные команды

### Unity Editor

```
Window → MCP Bridge → Start Server      # Запустить Bridge
Window → MCP Bridge → Stop Server       # Остановить Bridge  
Window → MCP Bridge → Settings          # Настройки
Window → MCP Bridge → Restart Server    # Перезапуск
```

### MCP Server

```bash
npm run dev      # Запуск в dev режиме (с hot reload)
npm run build    # Сборка TypeScript → JavaScript
npm run start    # Запуск production версии
npm run clean    # Очистить build артефакты
```

### Claude Code (примеры промптов)

```
Check Unity connection
Get scene info
Create cube named "Test"
Import FBX from path/to/file.fbx
Retarget animation from Mixamo to Mecanim
Show project structure
```

## Дополнительная настройка

### Автозапуск MCP Bridge в Unity

По умолчанию включен. Для отключения:
```
Window → MCP Bridge → Settings
[ ] Auto-start on Editor load
```

### Изменение порта

Unity:
```
Window → MCP Bridge → Settings
Port: 7777 → 8888
Save Settings
```

MCP Server `.env`:
```env
UNITY_EDITOR_PORT=8888
```

### Логирование

MCP Server выводит логи в stderr:
```bash
npm run dev 2> mcp-server.log
```

Unity логи: `Window → General → Console`

## Поддержка

При проблемах:
1. Проверить все шаги Quick Start
2. Изучить раздел Troubleshooting
3. Посмотреть логи Unity Console и MCP Server
4. Обратиться к команде или создать issue

---

**Готово!** Теперь можно использовать Claude для автоматизации работы с Unity проектом.

**Следующий шаг**: Изучить MCP_API.md для полного списка возможностей.
