# MCP API Reference - Unity Integration

**Версия API**: 0.1.0  
**Дата обновления**: 2026-08-25

## Обзор

API предоставляет набор инструментов для взаимодействия Claude AI с Unity Editor через MCP протокол.

## Базовая конфигурация

### Настройка Claude Code

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

## Инструменты MCP

### 1. unity_check_connection

Проверяет подключение к Unity Editor.

**Параметры**: Нет

**Возвращает**:
```json
{
  "connected": true,
  "status": "Unity Editor MCP Bridge is active",
  "bridge_url": "localhost",
  "port": "7777"
}
```

**Пример использования в Claude Code**:
```
Check Unity connection status
```

---

### 2. unity_execute_command

Выполняет произвольную команду Unity Editor API.

**Параметры**:
- `command` (string, обязательный) - Название команды
- `args` (object, опциональный) - Аргументы команды

**Доступные команды**:
- `CreateGameObject` - Создать GameObject
- `GetProjectStructure` - Получить структуру проекта
- `GetConsoleLogs` - Получить логи консоли
- `SaveScene` - Сохранить текущую сцену
- `RefreshAssetDatabase` - Обновить базу ассетов

**Возвращает**:
```json
{
  "success": true,
  "command": "CreateGameObject",
  "result": { /* результат выполнения */ }
}
```

**Пример**:
```
Execute Unity command to save the current scene
```

---

### 3. unity_get_scene_info

Получает информацию о текущей открытой сцене в Unity.

**Параметры**: Нет

**Возвращает**:
```json
{
  "sceneName": "Demo Zone",
  "scenePath": "Assets/DarkTree FPS/Demo Zone.unity",
  "isDirty": false,
  "isLoaded": true,
  "rootCount": 12,
  "gameObjects": [
    {
      "name": "Main Camera",
      "active": true,
      "tag": "MainCamera",
      "layer": "Default",
      "components": ["Transform", "Camera", "AudioListener"],
      "childCount": 0
    }
  ]
}
```

**Пример**:
```
Get information about the current Unity scene
```

---

### 4. unity_create_gameobject

Создает новый GameObject в текущей сцене Unity.

**Параметры**:
- `name` (string, обязательный) - Имя GameObject
- `type` (string, опциональный) - Тип: "Empty", "Cube", "Sphere", "Capsule", "Cylinder", "Plane"
- `parent` (string, опциональный) - Путь к родительскому GameObject

**Возвращает**:
```json
{
  "success": true,
  "gameObjectName": "TestCube",
  "instanceId": 123456,
  "message": "Created GameObject: TestCube"
}
```

**Пример**:
```
Create a sphere named "PlayerSpawn" in Unity
```

---

### 5. unity_import_asset

Импортирует внешний файл в Unity проект.

**Параметры**:
- `path` (string, обязательный) - Путь к файлу (абсолютный или относительный)
- `destination` (string, опциональный) - Путь назначения в Assets/

**Возвращает**:
```json
{
  "success": true,
  "sourcePath": "C:/Models/character.fbx",
  "destinationPath": "Assets/Models/character.fbx",
  "message": "Asset imported successfully: character.fbx"
}
```

**Пример**:
```
Import FBX file from C:/Models/hero.fbx into Assets/Characters/
```

---

### 6. unity_animation_retarget

Конвертирует анимацию между разными типами скелетов.

**Параметры**:
- `sourceAnimationPath` (string, обязательный) - Путь к исходной анимации в Assets
- `sourceSkeletonType` (string, обязательный) - Тип исходного скелета: "Mixamo", "UE4", "Custom"
- `targetSkeletonType` (string, обязательный) - Тип целевого скелета: "Mecanim", "Custom"
- `outputPath` (string, обязательный) - Путь для сохранения результата

**Возвращает**:
```json
{
  "success": true,
  "sourceAnimation": "Assets/Animations/walk_mixamo.anim",
  "outputAnimation": "Assets/Animations/walk_retargeted.anim",
  "mapping": "Mixamo_to_Mecanim",
  "bonesRetargeted": 23,
  "bonesSkipped": 5,
  "totalCurves": 180,
  "message": "Animation retargeted successfully: 23 bones mapped, 5 kept original"
}
```

**Поддерживаемые маппинги**:
- `Mixamo_to_Mecanim` - Mixamo → Unity Humanoid
- `UE4_to_Mecanim` - Unreal Engine 4 → Unity Humanoid

**Пример**:
```
Retarget animation from Mixamo to Mecanim:
- Source: Assets/Animations/Mixamo/run.anim
- Source type: Mixamo
- Target type: Mecanim
- Output: Assets/Animations/Player/run.anim
```

---

### 7. unity_get_project_structure

Получает структуру папок и файлов проекта Unity.

**Параметры**:
- `depth` (number, опциональный, по умолчанию: 2) - Глубина сканирования

**Возвращает**:
```json
{
  "projectPath": "d:/PROJECTS/DarkTreeFPS2022/lfhrnhbagc2022/Assets",
  "structure": {
    "directories": [
      {
        "name": "Scripts",
        "path": "Assets/Scripts",
        "children": { /* подпапки */ }
      }
    ],
    "files": ["scene.unity", "config.asset"]
  }
}
```

**Пример**:
```
Show Unity project structure with depth 3
```

---

### 8. unity_console_logs

Получает последние записи из Unity Console (в разработке).

**Параметры**:
- `count` (number, опциональный, по умолчанию: 50) - Количество записей
- `filter` (string, опциональный, по умолчанию: "all") - Фильтр: "all", "error", "warning", "info"

**Возвращает**:
```json
{
  "message": "Console logs API is not directly accessible. Consider implementing custom logging system.",
  "count": 0,
  "logs": []
}
```

**Статус**: 🚧 В разработке - требует реализации кастомной системы логирования

---

## Примеры использования

### Пример 1: Проверка подключения и создание тестового объекта

```
1. Check if Unity Editor is connected
2. If connected, create a test cube named "MCPTest"
3. Get current scene info to confirm the cube was created
```

### Пример 2: Импорт и ретаргетинг анимации

```
Import animation from C:/Downloads/character_walk.fbx into Assets/Animations/Raw/

Then retarget the animation:
- Source: Assets/Animations/Raw/character_walk.anim
- Source skeleton: Mixamo
- Target skeleton: Mecanim
- Output: Assets/Animations/Player/walk.anim
```

### Пример 3: Анализ проекта

```
1. Get Unity project structure with depth 2
2. Check scene info for "Demo Zone" scene
3. List all GameObjects with "Enemy" in their name
```

## Обработка ошибок

Все инструменты возвращают ошибки в следующем формате:

```json
{
  "error": "Error message",
  "tool": "unity_create_gameobject",
  "timestamp": "2026-08-25T20:00:00.000Z"
}
```

### Типичные ошибки:

**Unity Editor не отвечает**:
```json
{
  "connected": false,
  "status": "Unity Editor not responding"
}
```
**Решение**: Запустить Unity Editor и убедиться что MCP Bridge активен

**Файл не найден**:
```json
{
  "error": "File not found: Assets/Animations/test.anim"
}
```
**Решение**: Проверить путь к файлу

**Порт занят**:
```json
{
  "error": "Failed to start server: The address is already in use"
}
```
**Решение**: Изменить порт в настройках или остановить другое приложение

## HTTP API (Unity Bridge)

MCP Server общается с Unity через HTTP API:

### Endpoints:

**GET** `/health`
- Проверка статуса сервера

**POST** `/execute`
- Выполнение команды
- Body: `{ "command": "string", "args": {} }`

**GET** `/scene`
- Получение информации о сцене

**POST** `/import`
- Импорт ассета
- Body: `{ "path": "string", "options": {} }`

**POST** `/animation/retarget`
- Ретаргетинг анимации
- Body: `{ "sourceAnimationPath": "string", "sourceSkeletonType": "string", ... }`

### Адрес сервера:
```
http://localhost:7777
```

## Расширение API

### Добавление нового инструмента MCP

1. Добавить tool definition в `mcp-server/src/index.ts`:
```typescript
{
  name: 'unity_my_new_tool',
  description: 'Description of the tool',
  inputSchema: {
    type: 'object',
    properties: {
      param1: { type: 'string', description: 'Parameter description' }
    },
    required: ['param1']
  }
}
```

2. Добавить обработчик в switch statement:
```typescript
case 'unity_my_new_tool': {
  const { param1 } = args as { param1: string };
  const result = await unityClient.executeCommand('MyCommand', { param1 });
  return { content: [{ type: 'text', text: JSON.stringify(result) }] };
}
```

3. Добавить команду в Unity `UnityCommandExecutor.cs`:
```csharp
case "MyCommand":
    return MyCommandHandler(args);
```

### Добавление нового маппинга скелетов

В `AnimationRetargeting.cs` добавить в словарь `SkeletonMappings`:

```csharp
{
    "CustomSkeleton_to_Mecanim", new Dictionary<string, string>
    {
        { "custom_bone_name", "Mecanim_bone_name" },
        // ... остальные кости
    }
}
```

## Безопасность

⚠️ **Важно**:
- MCP Bridge слушает только `localhost` - доступ только с локальной машины
- Нет аутентификации - подходит только для dev окружения
- Для production добавить API ключи и HTTPS

## Производительность

- Таймауты команд: 10 секунд (обычные), 20 секунд (анимации)
- Порт по умолчанию: 7777
- Максимальный размер запроса: не ограничен (зависит от .NET HttpListener)

---

**Следующие шаги**:
- Реализовать AI-powered маппинг костей для произвольных скелетов
- Добавить интеграцию с Blender Python API
- Реализовать систему логирования Console
- Добавить поддержку batch операций

**Обратная связь**: Создавайте issues в проектном трекере или обсуждайте в команде

---

**Документ**: MCP_API.md  
**Версия**: 0.1.0  
**Последнее обновление**: 2026-08-25T20:00:30Z
