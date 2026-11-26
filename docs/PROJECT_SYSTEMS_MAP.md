# Карта систем проекта и границ независимой работы

**Проект:** DarkTree FPS / Unity 2022.3.15f1  
**Назначение:** разбирать и развивать системы по отдельности, сокращая повторный анализ и избегая случайных изменений соседних модулей.

**Проверка фактических runtime-событий:** см. [протокол трассировки игровых событий](./UNITY_EVENT_TRACE.md). Он задаёт файловый захват всех Console log types и проверку event trigger/outcome по механикам. `Dummy` в сцене `Demo Zone` — постоянный разрешённый тестовый объект: сохранять его и намеренно назначенные тестовые компоненты; не удалять и не перепрофилировать без указания пользователя.

> Это карта по текущему расположению и статически просмотренным скриптам, не полная runtime-диаграмма. `Assets`/сцены/префабы могут содержать сериализованные связи, не видимые только из C# файлов. Перед изменением всё равно проверять вызовы, ссылки и Unity Inspector.

## 0. Статус разборов (обновлено 2026-11-26)

Новичку начинать с [обзора проекта](./PROJECT_OVERVIEW.md): там общая картина простым языком, а эта карта — справочник по границам систем.

| ID | Система | Разбор | Статус |
|---|---|---|---|
| S01 | Игрок: контроллер и движение | [PLAYER_CONTROLLER_ANALYSIS.md](./PLAYER_CONTROLLER_ANALYSIS.md) | разобрано |
| S02 | Игрок: здоровье и выживание | [PLAYER_STATS_ANALYSIS.md](./PLAYER_STATS_ANALYSIS.md) | разобрано |
| S03 | Оружие | [WEAPON_SYSTEM.md](./WEAPON_SYSTEM.md) | разобрано |
| S04 | NPC и AI | [NPC_AI_SYSTEM.md](./NPC_AI_SYSTEM.md) | разобрано |
| S05 | Инвентарь, предметы, экипировка | [INVENTORY_SYSTEM.md](./INVENTORY_SYSTEM.md) | разобрано, включая данные префабов |
| S06 | Строительство | [BUILDING_SYSTEM.md](./BUILDING_SYSTEM.md) | разобрано |
| S07 | Сохранение и переходы | [SAVE_SYSTEM.md](./SAVE_SYSTEM.md) | разобрано |
| S08 | Анимационные контракты | [ANIMATION_CONTRACTS.md](./ANIMATION_CONTRACTS.md) | разобрано (строки и события; привязка к объектам сцены не проверена) |
| S09, S10, S13 | Звук, UI/HUD/меню, сцены | [UI_MENU_SCENES_SOUND.md](./UI_MENU_SCENES_SOUND.md) | разобрано |
| S11 | Управление и мобильный ввод | [INPUT_SYSTEM.md](./INPUT_SYSTEM.md) | разобрано |
| S12 | MCP Unity Editor Bridge | `MCP_*.md`, `README_MCP.md` в корне | описано отдельно |
| S14 | Эффекты и интерактивность | [INTERACTION_AND_EFFECTS.md](./INTERACTION_AND_EFFECTS.md) | разобрано |
| S15 | Инструменты редактора | раздел в [PROJECT_OVERVIEW.md](./PROJECT_OVERVIEW.md) | кратко |

Все разборы — статические: по коду, префабам и ассетам. Ни один не подтверждён прогоном в Play Mode. Сцена `Demo Zone.unity` хранится в бинарном виде, поэтому её содержимое (расстановка объектов, переопределения префабов) в разборах не учтено.

## 1. Принцип модульности

Система считается безопасно изменяемой отдельно, когда изменение остаётся внутри её файлов и не меняет используемый другими системами контракт: публичные методы, сериализованные поля, ScriptableObject-формат, имена Animation Events, имена сценовых объектов, inventory item IDs/titles, теги/layers или формат сохранений.

Для первой фазы не делать широких рефакторингов общих менеджеров. Работать небольшими вертикальными задачами:

1. фиксировать фактическое поведение и его источники;
2. обозначать входы/выходы системы;
3. проверить, кто вызывает её публичные API;
4. менять только выбранный модуль;
5. проверить Unity compile/scene/Play Mode и соседние интеграции, которые входят в список ниже.

## 2. Системы и зоны ответственности

| ID | Система | Основные файлы/папки | Независимость | Основные внешние контракты |
|---|---|---|---|---|
| S01 | Игрок: FPS-контроллер и движение | `Player/FPSController.cs`, `Body.cs`, `CameraBob.cs`, `HeadBob.cs`, `Sway.cs`, `Lean.cs`, `GetCollisionTag.cs` | Средняя | `InputManager`, Camera Holder/Main Camera, `PlayerStats`, `Weapon`, `WeaponManager`, Animator, physics |
| S02 | Игрок: состояние, здоровье и выживание | `Player/PlayerStats.cs`, `RadioactiveZone.cs`, `WaterVolume*.cs`, `FootstepSound.cs` ([анализ](./PLAYER_STATS_ANALYSIS.md)) | Средняя | `FPSController`, `InputManager`, `WeaponManager`, HUD refs, AudioSource, save data |
| S03 | Оружие игрока | `Weapon/*.cs` кроме общих health/hit FX по необходимости; прежде всего `Weapon.cs`, `WeaponManager.cs`, `WeaponSettingSO.cs`, `BalisticProjectile.cs`, `Rocket.cs`, `Grenade.cs` | Средняя | DTInventory Items/Ammo, `PlayerStats`, `FPSController`, `InputManager`, NPC damage/target components, Animator Events, tags/layers, scene singletons |
| S04 | NPC и AI | `NPC/*.cs` и относящиеся AI состояния, включая `NPC.cs`, `AIControl.cs`, `NPCVision.cs`, `Cover*.cs`, `ZombieNPC.cs`, IK scripts | Средняя/низкая | NavMesh, Animator/avatar, `PlayerStats`, player hit targets, weapon hit pipeline, `CoverList`, factions, scene spawn/save |
| S05 | Инвентарь, предметы и экипировка | `DT Inventory/Inventory/`, `DTFPSInventroyExtended.cs`, `PickupItem.cs`, `TouchPickupItemHandler.cs`, `Tooltip.cs` | Средняя/низкая | WeaponManager, Building, SaveData, UI/Canvas, item databases/assets, input, scene pickups/loot boxes |
| S06 | Строительство и размещение объектов | `Building/BuildingManager.cs`, `BuildingScriptableObjects.cs`, `ObjectPlacement.cs`, `ObjectPlaceUtility.cs` | Средняя | Inventory/DTFPSInventoryExtended cost search, `InventoryItem`, `WeaponManager.HideAll`, Physics raycasts, renderer/materials, building prefabs |
| S07 | Сохранение/загрузка и переход между уровнями | `DT Inventory/SaveData.cs`, `Level Transition/*.cs` | Низкая (интеграционный модуль) | PlayerStats/FPSController, Camera Holder, WeaponManager, inventory/items/lootboxes, NPC/Zombie, `AssetsDatabase`, scenes/build settings, persistentDataPath |
| S08 | Анимации персонажа и оружия | Animator Controllers/Clips/Avatar assets + animation events; связанные вызовы в Player, Weapon, NPC | Низкая как отдельные assets, высокая интеграционная цена | Строковые имена state/trigger/bool, Animation Events (reload/grenade/rocket), root motion, rig/avatar mapping |
| S09 | Звук и аудио-вызовы | `Other/SoundManager.cs`, `Player/FootstepSound.cs`, Weapon/NPC audio refs, Audio assets | Высокая для SoundManager; средняя для общего аудио | AudioSource и сериализованные AudioClip refs; методы вызова Pickup/Click/WeaponPicking; AudioMixer/settings если есть в сцене |
| S10 | UI, HUD и меню | `Menu/MainMenu.cs`, `ResolutionChanger.cs`, Inventory UI, `Weapon` HUD, `PlayerStats` HUD, `VisionUI.cs`, Fade/Tooltip scripts | Средняя/низкая | конкретные сцены/Canvas, legacy `UnityEngine.UI.Text`, публичные ссылки, scene loading, input и gameplay state |
| S11 | Управление и мобильный ввод | `Mobile and Inputs/InputManager.cs`, `Joystick.cs`, `MobileInputRig.cs`, `TouchField.cs` | Средняя | Player/Weapon/Inventory consumers; клавиши и UI Event callbacks; `InputManager.useMobileInput` |
| S12 | MCP Unity Editor Bridge | `Editor/MCPBridge/MCPBridgeServer.cs`, `UnityCommandExecutor.cs`, `MCPBridgeSettings.cs`, Node `mcp-server/` | Высокая относительно gameplay, отдельная интеграция | stdio MCP protocol, HTTP localhost:7777, Unity main-thread dispatch, UnityEditor reflection APIs, клиентские конфиги |
| S13 | Меню/сцены и demo flow | `Menu/MainMenu.cs`, `Level Transition/*.cs`, Build Settings, scene assets | Средняя | build indices/scene names, сохранение между сценами, singleton persistence, menu buttons |
| S14 | Общие gameplay эффекты и интерактивность | `Weapon/HitFXManager.cs`, `ObjectHealth.cs`, `UseObjects.cs`, `ConsumeEvents.cs`, `Anomaly.cs`, `Heli*.cs`, `DistanceDisable.cs` | Средняя, нужна классификация по каждому скрипту | Physics/tags, player/weapon/inventory/audio, scene event wiring |
| S15 | Editor tooling и импорт ресурсов | `Assets/.../Editor/`, `MCPBridge/AnimationRetargeting.cs`, инспекторы/wizards | Высокая от runtime gameplay, но зависит от UnityEditor и данных ассетов | Serialized assets, Unity editor APIs, импортированные rig/animation assets |

## 3. Карта зависимостей

```text
InputManager ───────► Player/FPSController ─────► PlayerStats
      │                       │                       │
      ├────────► Weapon.Update/Fire/Aim             └────► SaveData
      ├────────► Inventory UI                           ▲
      └────────► Mobile UI                              │
                                                         │
Inventory/Items ◄────► WeaponManager ◄────► Weapon ─────┤
      │                   │                  │            │
      ├────► BuildingManager/ObjectPlacement │            │
      └────► SaveData                         ├────► hit FX / ObjectHealth
                                               └────► NPC hit targets

NPC / Zombie ──► NavMesh + Animator + NPCVision + CoverList
      │                          │
      ├──── damage to player ────┴────► PlayerStats
      └──── state/position/identity ─────► SaveData / AssetsDatabase

Menu + LevelTransition ──► SceneManager ──► SaveData/persistence

MCP client ──stdio──► Node MCP server ──HTTP localhost:7777──► Unity Editor bridge
```

Стрелка означает известную связь по просмотренному коду/назначению системы; детали нужно подтверждать поиском call sites и ссылками в сценах.

## 4. Какие задачи можно выполнять почти независимо

### Низкий риск при узком scope

1. **MCP-интеграция Node ↔ Unity Editor**, если не менять игровой runtime. Тесты: TypeScript build, MCP initialize/list tools, health, безопасная editor-команда.
2. **Один автономный звук/AudioClip**, если меняется только clip reference в конкретном компоненте/asset и не переименовываются методы.
3. **Чистые Editor tools** (инспектор/wizard) без изменения сериализованных типов данных или runtime-кода.
4. **Документация и каталогизация**, если утверждения проверяются по фактическим скриптам/assets и помечаются как статический анализ.
5. **Изолированный эффект** с уже существующим контрактом — например, правка визуального параметра конкретного эффекта без изменения вызывающего кода.

### Средний риск, но хорошо отделимые вертикальными задачами

- Баланс одного `WeaponSettingSO` asset: не менять формат SO и не менять тип/имя оружия.
- Логика одного оружия: оставаясь в `Weapon`/projectile path, проверить ammo, reload, animation callbacks, hit/damage.
- Размещение конкретного build prefab: не менять общий формат цены/инвентаря.
- Отдельное окно/элемент UI: сохранить события и сериализованные поля.
- Input mapping: можно менять mapping, но требуется проверить Player, Weapon, mobile и UI потребителей.

### Сильно связанные — не считать независимыми

- **SaveData:** интегратор игрока, инвентаря, loot boxes, сценовых предметов, NPC и zombie. Любое изменение требует совместимости старых файлов сейва и теста всех сериализуемых сущностей.
- **Inventory ↔ Weapon:** предметы экипировки и ammo управляют текущим магазином и выбором оружия.
- **Inventory ↔ Building:** стоимость строения списывается из inventory через `DTFPSInventoryExtended`.
- **Animation ↔ gameplay:** string state names и Animation Events вызывают reload, throw и другие методы; переименование может ломать поведение без compile error.
- **NPC ↔ Player/Weapon:** зрение/урон/фракции/хиты соединены через scene components и tags.
- **Player death/respawn ↔ WeaponManager/SaveData/Camera Holder:** затрагивает несколько объектов и состояние сцены.

## 5. Рекомендуемая последовательность разбора

Порядок выбран так, чтобы сначала понять базовые контракты, а поздние сложные системы изучать на уже описанных зависимостях.

1. **S11 Управление и ввод** — источники ввода PC/mobile и их потребители.
2. **S01 Игрок и камера** — движение, камера, sway, lean, climbing/swimming, root motion.
3. **S02 PlayerStats** — здоровье, stamina, hunger/thirst, death/respawn; отдельная карта связей HUD и звука.
4. **S05 Инвентарь/предметы** — база предметов, grid/equipment slots, pickup, stack, ammo и loot boxes.
5. **S03 Оружие** — документация уже начата (`WEAPON_SYSTEM_1/2/3.md`); следующий проход — анимации и реальные assets/prefabs, затем projectile/damage tests.
6. **S08 Анимационные контракты** — states, parameters, clips, events, root motion, rig/avatars; анализировать вместе с Player/Weapon/NPC, но документировать отдельным модулем.
7. **S04 NPC/AI** — vision, states, tactics, cover, NavMesh, damage, death, save interaction.
8. **S06 Строительство** — UI catalog, recipe validation, preview placement, collision validation, cost consumption/cancel behavior.
9. **S09 Звук** — общая SoundManager и локальные источники оружия/NPC/игрока; определить, есть ли единая громкость/mixer.
10. **S07 SaveData и S13 переходы** — только после схемы player/inventory/NPC; в начале составить список файлов и критических проблем без миграций.
11. **S10 UI/меню** — связать каждый экран с конкретным gameplay API; учесть разные сцены и мобильный UI.
12. **S12 MCP Bridge и S15 Editor tools** — отдельно от gameplay; вести отдельные регресс-тесты.
13. **S14 остаточные эффекты/интерактивность** — распределить каждый скрипт по вызывающей системе после поиска references.

## 6. Шаблон разбора каждой системы

Для каждой отдельной беседы/этапа фиксировать компактно:

1. **Назначение:** какой игровой результат обеспечивает.
2. **Состав:** реальные скрипты, assets, scenes/prefabs.
3. **Жизненный цикл:** Awake/Start/Update/events/coroutines и кто вызывает API.
4. **Данные:** ScriptableObjects, serialized fields, IDs, JSON/save keys.
5. **Входы/выходы:** input, calls, events, изменяемые данные, side effects.
6. **Зависимости:** что вызывает систему и что она вызывает.
7. **Контракт:** имена методов/полей/state/events/tags/layers, менять которые опасно.
8. **Проблемы:** подтверждённые баги отдельно от гипотез и предложений.
9. **Тест-план:** конкретные Unity шаги и ожидаемый результат.
10. **Трассировка триггеров:** фактические игровые события, состояние до/после и путь к файловому Console capture по [UNITY_EVENT_TRACE.md](./UNITY_EVENT_TRACE.md).
11. **Модульная граница:** какие файлы можно менять отдельно; какие проверки соседей обязательны.

## 7. Skill и накопление опыта

У проекта уже есть skill `.agents/skills/darktree-unity-project/SKILL.md`. Он задаёт постоянные правила работы именно с этим Unity-проектом (Unity version, `.meta`/GUID, MCP, Unity serialization, проверка изменений). Его уместно обновлять только устойчивыми общими правилами, а не полным пересказом каждой системы.

Предметные знания следует хранить в `docs/` рядом с соответствующими разборами. Длительные задачи и handoff — в `docs/ai-tasks/` с owner-prefix, предусмотренным проектными правилами. Для каждой новой системы можно вести документ `docs/<SYSTEM>_SYSTEM.md` и поддерживать эту карту ссылками/статусом.

## 8. Ограничения текущей карты

- Файловая структура не доказывает, что модуль полностью изолирован: Unity-сцены и prefabs соединяют компоненты сериализованными ссылками.
- На 2026-11-26 статически разобраны все системы из таблицы раздела 0. Не проверено: поведение в Play Mode и содержимое бинарной сцены `Demo Zone.unity`.
- Список не означает, что каждой системе нужно переписывание. Сначала документируем и определяем стабильные границы, затем исправляем только подтверждённую потребность.
