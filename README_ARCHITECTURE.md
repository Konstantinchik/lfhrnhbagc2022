# DarkTree FPS - Техническая Документация

**Версия:** 1.4  
**Дата анализа:** 26 августа 2026  
**Unity версия:** 2022.3.15f1

---

## 📋 Обзор Проекта

**DarkTree FPS** - полнофункциональный движок для создания FPS игр в Unity с модульной архитектурой.

### Ключевые Особенности
- ✅ Модульная архитектура с namespace изоляцией
- ✅ Система оружия с поддержкой различных типов
- ✅ Продвинутая система инвентаря с drag-and-drop
- ✅ AI система для NPC с патрулированием и боевым поведением
- ✅ Система строительства объектов
- ✅ Поддержка мобильных устройств
- ✅ Интеграция с MCP Bridge для AI-агентов

### Технический Стек
- **Engine:** Unity 2022.3.15f1
- **Language:** C# (.NET Framework)
- **Main Namespace:** DarkTreeFPS, DTInventory
- **Architecture:** Component-Based + Singleton Managers

---

## 📂 Структура Проекта

```
Assets/DarkTree FPS/
├── Scripts/
│   ├── Building/          # Система строительства
│   ├── DT Inventory/      # Расширения инвентаря для FPS
│   ├── Editor/            # Editor tools и wizards
│   ├── Inventory/         # Базовая система инвентаря
│   ├── Menu/              # Главное меню и UI
│   ├── Mobile and Inputs/ # Управление и мобильные контроллеры
│   ├── NPC/               # AI и NPC системы
│   ├── Player/            # Контроллеры игрока
│   ├── Weapon/            # Система оружия
│   ├── Other/             # Утилиты (SoundManager, etc.)
│   └── MCPBridge/         # Интеграция с AI агентами
├── Content/               # Ресурсы (модели, текстуры)
└── Standard Assets/       # Unity Standard Assets
```

---

## 🗂️ Документация по Модулям

### Основные Системы
1. [Система Оружия](./docs/WEAPON_SYSTEM.md) - Полное описание системы оружия
2. [Система Инвентаря](./docs/INVENTORY_SYSTEM.md) - DTInventory и интеграция
3. [Система NPC и AI](./docs/NPC_AI_SYSTEM.md) - Поведение и логика NPC
4. [Система Игрока](./docs/PLAYER_SYSTEM.md) - Контроллеры и статистика
5. [Система Строительства](./docs/BUILDING_SYSTEM.md) - Крафт и размещение объектов

### Интеграции
6. [MCP Bridge Integration](./docs/MCP_BRIDGE.md) - AI агенты и автоматизация
7. [Mobile Input System](./docs/MOBILE_INPUT.md) - Поддержка мобильных устройств

### Разработка
8. [Руководство по Разработке](./docs/DEVELOPMENT_GUIDE.md) - Best practices
9. [Troubleshooting](./docs/TROUBLESHOOTING.md) - Типичные проблемы и решения

---

## 🎯 Core Managers (Singleton Pattern)

### WeaponManager
- **Путь:** `Scripts/Weapon/WeaponManager.cs`
- **Функции:** Управление оружием, переключение, интеграция с инвентарём

### InventoryManager  
- **Путь:** `Scripts/Inventory/InventoryManager.cs`
- **Функции:** UI инвентаря, лут боксы, взаимодействие

### SoundManager
- **Путь:** `Scripts/Other/SoundManager.cs`
- **Функции:** Централизованное управление звуками

### InputManager
- **Путь:** `Scripts/Mobile and Inputs/InputManager.cs`
- **Функции:** Управление для PC и мобильных устройств

---

## 🔧 Быстрый Старт

### Добавление Нового Оружия
1. Create → Weapon → Weapon Settings (ScriptableObject)
2. Создать префаб оружия с компонентом `Weapon`
3. Создать Item в базе данных
4. Связать Weapon с Item

### Создание Нового NPC
1. Humanoid модель с Animator
2. Добавить компоненты: NPC, NavMeshAgent, AIControl, NPCVision
3. Настроить параметры боя и AI
4. Добавить в базу данных для дропа

### Расширение Инвентаря
1. Create → Inventory → Item (ScriptableObject)
2. Настроить параметры (тип, размер, стакание)
3. Добавить в ItemDatabase
4. Создать логику использования (UnityEvent)

---

## 📊 Архитектурные Принципы

1. **Модульность** - Каждая система изолирована
2. **Extensibility** - Легко добавлять новые элементы через ScriptableObjects
3. **Component-Based** - Unity ECS подход
4. **Event-Driven** - UnityEvents для связи систем
5. **Editor Integration** - Мощные wizard'ы для настройки

---

## 🚀 MCP Bridge

**Интеграция с AI агентами (Claude)** для автоматизации разработки:
- HTTP сервер на порту 7777
- API для управления Unity Editor
- Команды: CreateGameObject, GetSceneInfo, ImportAsset, RetargetAnimation

**Запуск:** Window → MCP Bridge → Start Server

---

## 📞 Поддержка

**Разработчик:** DarkTreeDevelopment  
**Email:** darktreedevelopment@gmail.com  
**Версия:** DarkTree FPS v1.4

---

**Документация создана:** 26.08.2026  
**Создано с помощью:** Cline AI + MCP Bridge
