# MCP Integration для Unity Project
**Проект**: DarkTree FPS  
**Unity версия**: 2022.3.15f1  
**Дата создания**: 2026-08-25  
**Статус**: В разработке

## Обзор

Интеграция Model Context Protocol (MCP) для автоматизации разработки Unity проекта через AI агентов.

## Архитектура

```
┌─────────────────────────────────────────────────┐
│                  Claude Agent                    │
│            (Anthropic API / CLI)                 │
└────────────────┬────────────────────────────────┘
                 │
                 │ MCP Protocol (JSON-RPC)
                 │
┌────────────────▼────────────────────────────────┐
│              MCP Server (Node.js)                │
│  - Unity Editor API bridge                       │
│  - File system operations                        │
│  - Build automation                              │
│  - Asset processing                              │
└────────────────┬────────────────────────────────┘
                 │
                 │ C# Unity Editor Scripts
                 │
┌────────────────▼────────────────────────────────┐
│           Unity Editor (2022.3.15f1)            │
│  - Editor API                                    │
│  - Asset Database                                │
│  - Animation System                              │
│  - Blender Integration                           │
└─────────────────────────────────────────────────┘
```

## Компоненты

### 1. MCP Server (Node.js)
**Путь**: `d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\mcp-server\`

**Основные инструменты**:
- `unity_execute_command` - Выполнение Unity Editor команд
- `unity_import_asset` - Импорт ассетов
- `unity_export_asset` - Экспорт ассетов
- `unity_get_scene_info` - Получение информации о сцене
- `unity_animation_retarget` - Конвертация анимаций между скелетами
- `blender_execute_script` - Выполнение Blender Python скриптов
- `unity_debug_code` - Анализ и отладка C# кода

### 2. Unity Editor Scripts (C#)
**Путь**: `Assets/DarkTree FPS/Scripts/Editor/MCPBridge/`

**Компоненты**:
- `MCPBridgeServer.cs` - HTTP сервер внутри Unity Editor
- `UnityCommandExecutor.cs` - Выполнение команд Editor API
- `AnimationRetargeting.cs` - Система конвертации анимаций
- `BlenderIntegration.cs` - Интеграция с Blender

### 3. Blender Integration Scripts (Python)
**Путь**: `d:\PROJECTS\DarkTreeFPS2022\lfhrnhbagc2022\blender-scripts\`

**Компоненты**:
- `skeleton_analyzer.py` - Анализ структуры скелета
- `animation_converter.py` - AI-powered конвертация анимаций
- `fbx_exporter.py` - Экспорт результатов в Unity

## Задачи

### ✅ Задача #1: MCP сервер для Unity Editor API
**Статус**: В работе  
**Приоритет**: Высокий

**Подзадачи**:
- [ ] Создать Node.js MCP сервер с базовыми инструментами
- [ ] Реализовать C# HTTP bridge в Unity Editor
- [ ] Протестировать взаимодействие Claude → MCP → Unity

### 📋 Задача #2: Система конвертации анимаций Unity-Blender
**Статус**: Запланировано  
**Приоритет**: Высокий

**Подзадачи**:
- [ ] Разработать алгоритм маппинга костей между скелетами
- [ ] Создать AI модель для предсказания соответствий
- [ ] Интегрировать Blender Python API
- [ ] Реализовать двустороннюю синхронизацию Unity ↔ Blender

### 📋 Задача #3: Автоматизация отладки через Claude
**Статус**: Запланировано  
**Приоритет**: Средний

**Подзадачи**:
- [ ] Подключить анализ логов Unity Console
- [ ] Настроить автоматическое исправление ошибок
- [ ] Создать систему рекомендаций по коду

## Структура файлов

```
lfhrnhbagc2022/
├── mcp-server/                      # MCP Server (Node.js)
│   ├── src/
│   │   ├── index.ts                 # Точка входа сервера
│   │   ├── tools/                   # MCP инструменты
│   │   │   ├── unity-commands.ts
│   │   │   ├── animation-tools.ts
│   │   │   └── blender-bridge.ts
│   │   └── unity-client.ts          # HTTP клиент для Unity Bridge
│   ├── package.json
│   └── tsconfig.json
│
├── blender-scripts/                 # Blender Python скрипты
│   ├── skeleton_analyzer.py
│   ├── animation_converter.py
│   └── fbx_exporter.py
│
├── Assets/DarkTree FPS/Scripts/Editor/MCPBridge/
│   ├── MCPBridgeServer.cs          # HTTP сервер Unity
│   ├── UnityCommandExecutor.cs     # Выполнение команд
│   ├── AnimationRetargeting.cs     # Ретаргетинг анимаций
│   └── BlenderIntegration.cs       # Интеграция Blender
│
├── MCP_INTEGRATION.md              # Этот документ
├── MCP_SETUP.md                    # Инструкции по установке
└── MCP_API.md                      # API референс
```

## Технологии

- **MCP Server**: Node.js + TypeScript
- **Unity Scripts**: C# (.NET Standard 2.1)
- **Blender Scripts**: Python 3.10+
- **AI Models**: Anthropic Claude (через API)
- **IPC**: HTTP REST + JSON

## Следующие шаги

1. Создать базовый MCP сервер с минимальным набором инструментов
2. Реализовать Unity Editor HTTP bridge
3. Протестировать простые команды (например, создание GameObject)
4. Разработать систему конвертации анимаций
5. Интегрировать Blender

## Вопросы для обсуждения

- Какие именно типы скелетов используются в проекте?
- Есть ли примеры анимаций для тестирования конвертации?
- Какая версия Blender будет использоваться?
- Нужна ли поддержка batch обработки анимаций?

## Заметки

- Unity Editor API доступен только в режиме Editor (не runtime)
- MCP сервер будет работать как отдельный процесс
- Claude Code может взаимодействовать с MCP сервером через `.claude/mcp_config.json`

---
**Обновлено**: 2026-08-25 22:55 UTC
