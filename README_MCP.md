# MCP Integration для Unity - DarkTree FPS

**Версия**: 0.1.0  
**Дата**: 2026-08-25  
**Unity**: 2022.3.15f1  
**Статус**: ✅ Базовая интеграция готова

## 🎯 Что это?

Система интеграции **Model Context Protocol (MCP)** с Unity Editor, позволяющая AI агентам (Claude) автоматизировать разработку игры через:

- 🔧 **Unity Editor API** - создание объектов, управление сценами, импорт ассетов
- 🎭 **AI-powered конвертация анимаций** - автоматический ретаргетинг между разными скелетами
- 🔄 **Интеграция Unity + Blender** - двусторонняя синхронизация через Python API
- 🐛 **Автоматизация отладки** - анализ и исправление кода через Claude

## 🚀 Быстрый старт

### За 5 минут:

```powershell
# 1. Установить зависимости
cd mcp-server
npm install

# 2. Запустить всё одной командой
cd ..
.\start-mcp.ps1

# 3. Открыть Unity Editor
# 4. Запустить Claude Code
```

Подробнее: **[QUICKSTART.md](QUICKSTART.md)**

## 📁 Структура проекта

```
lfhrnhbagc2022/
├── mcp-server/                           # Node.js MCP Server
│   ├── src/
│   │   └── index.ts                      # Основной сервер с инструментами
│   ├── package.json
│   └── .env.example
│
├── Assets/DarkTree FPS/Scripts/Editor/MCPBridge/
│   ├── MCPBridgeServer.cs                # HTTP сервер в Unity Editor
│   ├── UnityCommandExecutor.cs           # Выполнение команд Unity API
│   ├── AnimationRetargeting.cs           # AI конвертация анимаций
│   └── MCPBridgeSettings.cs              # Настройки и UI
│
├── blender-scripts/                      # Python скрипты для Blender (в разработке)
│
├── .claude/
│   └── mcp_config.json                   # Конфигурация Claude Code
│
├── MCP_INTEGRATION.md                    # 📖 Архитектура системы
├── MCP_SETUP.md                          # 📖 Детальная установка
├── MCP_API.md                            # 📖 API референс
├── QUICKSTART.md                         # 📖 Быстрый старт
└── start-mcp.ps1                         # Автозапуск скрипт
```

## 🛠️ Доступные инструменты MCP

### Базовые операции:
- ✅ `unity_check_connection` - Проверка подключения к Unity
- ✅ `unity_execute_command` - Выполнение произвольных команд
- ✅ `unity_get_scene_info` - Информация о текущей сцене
- ✅ `unity_create_gameobject` - Создание GameObject
- ✅ `unity_get_project_structure` - Структура проекта

### Работа с ассетами:
- ✅ `unity_import_asset` - Импорт файлов в проект
- ✅ `unity_animation_retarget` - Конвертация анимаций между скелетами

### В разработке:
- 🚧 `unity_console_logs` - Получение логов Console
- 🚧 `blender_execute_script` - Выполнение Blender Python скриптов
- 🚧 `unity_debug_code` - AI-powered отладка C# кода

Полный список: **[MCP_API.md](MCP_API.md)**

## 💡 Примеры использования

### Создание GameObject через Claude:
```
Create a player spawn point (Empty GameObject) named "PlayerStart" at position (0, 1, 0)
```

### Импорт и конвертация анимации:
```
Import character animation from C:/Downloads/walk.fbx into Assets/Animations/Raw/
Then retarget it from Mixamo to Mecanim skeleton and save to Assets/Animations/Player/walk.anim
```

### Анализ проекта:
```
Show me the structure of Assets/Scripts folder with depth 3
Then list all scripts that contain "Weapon" in the name
```

## 📋 Статус задач

### ✅ Задача #1: MCP сервер для Unity Editor API
**Статус**: Завершена (2026-08-25)

**Реализовано**:
- ✅ Node.js MCP Server с 8 инструментами
- ✅ Unity C# HTTP Bridge Server
- ✅ Автозапуск при загрузке Unity Editor
- ✅ Конфигурация Claude Code
- ✅ Документация и примеры

### 📋 Задача #2: Система конвертации анимаций Unity-Blender
**Статус**: В разработке

**Реализовано**:
- ✅ Базовый ретаргетинг анимаций
- ✅ Предустановленные маппинги: Mixamo→Mecanim, UE4→Mecanim
- ✅ API для пользовательских маппингов

**Осталось**:
- 🚧 AI-powered маппинг для произвольных скелетов
- 🚧 Интеграция с Blender Python API
- 🚧 Анализ структуры скелетов через Claude
- 🚧 Batch обработка анимаций

### 📋 Задача #3: Автоматизация отладки через Claude
**Статус**: Запланировано

**План**:
- 🔜 Система сбора логов Unity Console
- 🔜 Анализ ошибок компиляции C#
- 🔜 Автоматическое исправление простых ошибок
- 🔜 Рекомендации по оптимизации кода

## 🔧 Технологии

- **MCP Server**: Node.js 18+, TypeScript, MCP SDK
- **Unity Bridge**: C# (.NET Standard 2.1), HttpListener, Newtonsoft.Json
- **Unity Editor**: 2022.3.15f1
- **Claude AI**: Anthropic API (опционально для AI-маппинга)
- **Blender**: 3.6+ с Python 3.10+ (для будущей интеграции)

## 📚 Документация

| Документ | Описание |
|----------|----------|
| **[QUICKSTART.md](QUICKSTART.md)** | Быстрый старт за 5 минут |
| **[MCP_INTEGRATION.md](MCP_INTEGRATION.md)** | Полная архитектура системы |
| **[MCP_SETUP.md](MCP_SETUP.md)** | Детальные инструкции по установке |
| **[MCP_API.md](MCP_API.md)** | API референс всех инструментов |

## 🎯 Следующие шаги

### Для начала работы:
1. Прочитать **QUICKSTART.md**
2. Запустить `.\start-mcp.ps1`
3. Открыть Unity Editor
4. Протестировать базовые команды

### Для работы с анимациями:
1. Подготовить тестовые анимации (Mixamo, UE4)
2. Изучить существующие маппинги в `AnimationRetargeting.cs`
3. Протестировать `unity_animation_retarget`
4. При необходимости добавить пользовательские маппинги

### Для расширения функционала:
1. Изучить архитектуру в **MCP_INTEGRATION.md**
2. Добавить новые команды в `UnityCommandExecutor.cs`
3. Расширить MCP инструменты в `mcp-server/src/index.ts`
4. Обновить документацию в **MCP_API.md**

## ❓ FAQ

**Q: Unity не видит скрипты MCPBridge?**  
A: Убедитесь что скрипты находятся в `Assets/.../Editor/MCPBridge/` - папка Editor обязательна.

**Q: Порт 7777 занят?**  
A: Измените порт в Unity (`Window → MCP Bridge → Settings`) и в `mcp-server/.env`.

**Q: Claude не видит MCP инструменты?**  
A: Проверьте что `.claude/mcp_config.json` существует и MCP Server запущен.

**Q: Как добавить свой маппинг скелета?**  
A: Добавьте запись в словарь `SkeletonMappings` в `AnimationRetargeting.cs`.

**Q: Можно ли использовать без Claude Code?**  
A: Да, HTTP API Unity Bridge доступен напрямую через `http://localhost:7777`.

## 🤝 Работа в команде

### Для коллег с другими AI агентами:

Все результаты работы сохранены в `.md` файлах:
- Архитектура и структура - **MCP_INTEGRATION.md**
- API и команды - **MCP_API.md**
- Установка - **MCP_SETUP.md**, **QUICKSTART.md**

Код разделен на модули:
- MCP Server: `mcp-server/src/index.ts`
- Unity Bridge: `Assets/.../MCPBridge/*.cs`
- Конфиг: `.claude/mcp_config.json`, `.env`

### Git ignore

Добавьте в `.gitignore`:
```
mcp-server/node_modules/
mcp-server/.env
mcp-server/dist/
.claude/mcp_config.json  # если содержит чувствительные данные
```

## 🐛 Troubleshooting

Общие проблемы и решения в **[MCP_SETUP.md](MCP_SETUP.md)** → раздел Troubleshooting.

При проблемах проверьте:
1. Unity Console - логи MCP Bridge
2. Терминал MCP Server - логи сервера
3. Версии Node.js (>= 18) и Unity (2022.3.15f1)
4. Порты не заняты другими приложениями

## 📞 Поддержка

- 📝 Создавайте issues в проектном трекере
- 💬 Обсуждайте в команде
- 📖 Читайте документацию в папке проекта

## 📄 Лицензия

Внутренний проект DarkTree FPS Team.

---

**Создано**: 2026-08-25T20:02:30Z  
**Автор**: AI Agent (Claude Sonnet 4)  
**Проект**: DarkTree FPS  
**Unity**: 2022.3.15f1

**Статус интеграции**: 🟢 Работает (базовые функции)  
**Готовность для продакшена**: 🟡 Dev-only (требуется добавить аутентификацию)

---

### 🎉 MCP Integration готов к использованию!

Начните с **[QUICKSTART.md](QUICKSTART.md)** → 5 минут до первого результата.
