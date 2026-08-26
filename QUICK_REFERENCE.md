# DarkTree FPS - Краткий Справочник

**Версия:** 1.4 | **Unity:** 2022.3.15f1 | **Дата:** 26.08.2026

---

## 🎯 Архитектура Проекта

### Основные Модули
```
DarkTree FPS/Scripts/
├── Weapon/         - Система оружия
├── Inventory/      - Инвентарь DTInventory
├── Player/         - Контроллер игрока
├── NPC/            - AI и враги
├── Building/       - Строительство
├── Editor/         - Инструменты редактора
└── MCPBridge/      - Интеграция с AI
```

### Core Managers (Singleton)
- **WeaponManager** - управление оружием
- **InventoryManager** - UI инвентаря
- **SoundManager** - звуки
- **InputManager** - ввод (PC + mobile)

---

## 🔫 Система Оружия

### Компоненты
```csharp
Weapon.cs                  // Главный компонент
WeaponSettingSO.cs        // ScriptableObject настройки
WeaponManager.cs          // Менеджер оружия
```

### Типы Оружия
- Pistol, Rifle, Shotgun, Sniper
- RocketLauncher, Grenade, Melee

### Ключевые Методы
```csharp
Shot()              // Выстрел
Reload()            // Перезарядка
SetAim(bool)        // Прицеливание
```

### Создание Нового Оружия
1. Create → Weapon → Weapon Settings
2. Префаб с компонентом Weapon
3. Create → Inventory → Item
4. Связать Weapon.currentItem = Item

---

## 🎒 Инвентарь (DTInventory)

### Компоненты
```csharp
DTInventory.cs            // Core класс
Item.cs                   // ScriptableObject предмета
GridSlot.cs               // Слот сетки
EquipmentPanel.cs         // Панель экипировки
```

### Item Типы
- Consumable (еда, медикиты)
- Equipment (оружие, броня)
- Resource (ресурсы для крафта)

### Методы
```csharp
AddItem(Item)
RemoveItem(InventoryItem)
CheckIfItemExist(string)
EquipItem(Item)
```

---

## 🤖 NPC и AI

### Компоненты
```csharp
NPC.cs                    // Главный компонент
AIControl.cs              // State Machine
NPCVision.cs              // Система зрения
NavMeshAgent              // Навигация
```

### AI States
- Idle → Patrol → Chase → Attack → TakeCover

### Параметры
```csharp
int health                // Здоровье
int damage                // Урон
float shootingAccuracySpread  // Точность
float visionRange         // Дальность зрения
float visionAngle         // Угол обзора
```

---

## 🏃 Игрок

### FPSController
```csharp
float moveSpeed           // Скорость
float runSpeedMultiplier  // Множитель бега
float jumpForce           // Прыжок
Vector2 sensitivity       // Чувствительность мыши
```

### PlayerStats
```csharp
int health = 100
float stamina = 100
int hydration = 100       // Жажда
int satiety = 100         // Голод
```

---

## 🏗️ Строительство

### BuildingScriptableObjects
```csharp
string BuildingName
GameObject BuildingGameObject
GameObject[] buildingCostItems
int[] builingCostItemsAmont
```

### Процесс
1. Проверка ресурсов
2. Режим размещения (ObjectPlacement)
3. Удаление ресурсов

---

## 🔌 MCP Bridge

### HTTP Server (порт 7777)

**API Endpoints:**
```
GET  /health          - Статус
POST /execute         - Команда Unity
GET  /scene           - Информация о сцене
POST /import          - Импорт ассета
```

**Запуск:**
```
Unity → Window → MCP Bridge → Start Server
```

---

## 🔧 Быстрые Рецепты

### Добавить Оружие
1. Create → Weapon → Weapon Settings
2. Префаб + Weapon + Animator
3. Create → Inventory → Item
4. Связать

### Создать NPC
1. Humanoid модель + Animator
2. Add: NPC, NavMeshAgent, AIControl
3. Настроить параметры
4. Добавить в базу

### Добавить Постройку
1. Create → Building → Building Data
2. Указать стоимость ресурсов
3. Добавить в BuildingManager

---

## 🐛 Типичные Проблемы

**Оружие не стреляет:**
- Проверить canShot, reloading, ammo
- Назначить WeaponSettingSO

**NPC не видит:**
- Проверить targetMask, visionRange
- Убрать препятствия

**"Newtonsoft not found":**
- Install: com.unity.nuget.newtonsoft-json

**Convex Mesh limit:**
- Tools → Fix Rock Colliders

---

## 📞 Поддержка

**Developer:** DarkTreeDevelopment  
**Email:** darktreedevelopment@gmail.com

**Создано:** 26.08.2026 | Cline AI + MCP Bridge
