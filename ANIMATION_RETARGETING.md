# Animation Retargeting Guide

**Дата**: 2026-08-25  
**Версия**: 0.1.0  
**Статус**: ✅ Базовая функциональность готова

## Обзор

Система AI-powered конвертации анимаций между различными типами скелетов человека с интеграцией Unity + Blender.

## Поддерживаемые форматы

### Текущие маппинги (готовы к использованию):

1. **Mixamo → Unity Mecanim**
   - Источник: Mixamo (mixamorig: префикс)
   - Цель: Unity Humanoid (стандартные имена костей)
   - Кости: 23 основных (без пальцев)

2. **Unreal Engine 4 → Unity Mecanim**
   - Источник: UE4 скелет (pelvis, spine_01, и т.д.)
   - Цель: Unity Humanoid
   - Кости: 23 основных

### Планируемые маппинги:

- 🔜 Unity Mecanim → Mixamo (обратная конвертация)
- 🔜 Unity Mecanim → UE4
- 🔜 Произвольный → Произвольный (AI-powered)
- 🔜 Пальцы рук и лицевая анимация

## Быстрый старт

### Пример 1: Конвертация Mixamo анимации

**Шаг 1**: Скачать анимацию с Mixamo
```
1. Перейти на mixamo.com
2. Выбрать анимацию (например, "Walking")
3. Download → Format: FBX for Unity
4. Сохранить в C:/Downloads/walking.fbx
```

**Шаг 2**: Импортировать в Unity через Claude
```
Import animation from C:/Downloads/walking.fbx into Assets/Animations/Raw/
```

**Шаг 3**: Конвертировать в Mecanim
```
Retarget animation:
- Source: Assets/Animations/Raw/walking.anim
- Source skeleton: Mixamo
- Target skeleton: Mecanim
- Output: Assets/Animations/Player/walking.anim
```

**Результат**: Готовая анимация для Unity Humanoid rig.

### Пример 2: Batch конвертация нескольких анимаций

```
I have 5 Mixamo animations in Assets/Animations/Raw/:
- idle.anim
- walk.anim
- run.anim
- jump.anim
- attack.anim

Retarget all of them from Mixamo to Mecanim
Save results in Assets/Animations/Player/ with the same names
```

## Структура костей

### Unity Mecanim (Humanoid)

```
Hips
├── Spine
│   ├── Chest
│   │   ├── UpperChest
│   │   │   ├── Neck
│   │   │   │   └── Head
│   │   │   ├── LeftShoulder
│   │   │   │   └── LeftUpperArm
│   │   │   │       └── LeftLowerArm
│   │   │   │           └── LeftHand
│   │   │   └── RightShoulder
│   │   │       └── RightUpperArm
│   │   │           └── RightLowerArm
│   │   │               └── RightHand
│   ├── LeftUpperLeg
│   │   └── LeftLowerLeg
│   │       └── LeftFoot
│   │           └── LeftToes
│   └── RightUpperLeg
│       └── RightLowerLeg
│           └── RightFoot
│               └── RightToes
```

### Mixamo (префикс: mixamorig:)

```
mixamorig:Hips
├── mixamorig:Spine
│   ├── mixamorig:Spine1
│   │   ├── mixamorig:Spine2
│   │   │   ├── mixamorig:Neck
│   │   │   │   └── mixamorig:Head
│   │   │   ├── mixamorig:LeftShoulder
│   │   │   │   └── mixamorig:LeftArm
│   │   │   │       └── mixamorig:LeftForeArm
│   │   │   │           └── mixamorig:LeftHand
...
```

### Unreal Engine 4

```
pelvis
├── spine_01
│   ├── spine_02
│   │   ├── spine_03
│   │   │   ├── neck_01
│   │   │   │   └── head
│   │   │   ├── clavicle_l
│   │   │   │   └── upperarm_l
│   │   │   │       └── lowerarm_l
│   │   │   │           └── hand_l
...
```

## API Использование

### Через MCP инструменты (Claude)

```typescript
unity_animation_retarget({
  sourceAnimationPath: "Assets/Animations/Raw/walk.anim",
  sourceSkeletonType: "Mixamo",
  targetSkeletonType: "Mecanim",
  outputPath: "Assets/Animations/Player/walk.anim"
})
```

### Через Unity C# API напрямую

```csharp
using DarkTreeFPS.MCPBridge;

var args = new Dictionary<string, object>
{
    { "sourceAnimationPath", "Assets/Animations/Raw/walk.anim" },
    { "sourceSkeletonType", "Mixamo" },
    { "targetSkeletonType", "Mecanim" },
    { "outputPath", "Assets/Animations/Player/walk.anim" }
};

string result = AnimationRetargeting.RetargetAnimation(args);
Debug.Log(result);
```

### Результат выполнения

```json
{
  "success": true,
  "sourceAnimation": "Assets/Animations/Raw/walk.anim",
  "outputAnimation": "Assets/Animations/Player/walk.anim",
  "mapping": "Mixamo_to_Mecanim",
  "bonesRetargeted": 23,
  "bonesSkipped": 5,
  "totalCurves": 180,
  "message": "Animation retargeted successfully: 23 bones mapped, 5 kept original"
}
```

## Добавление пользовательских маппингов

### Вариант 1: Через код C#

Добавить в `AnimationRetargeting.cs`:

```csharp
// В словарь SkeletonMappings
{
    "CustomSkeleton_to_Mecanim", new Dictionary<string, string>
    {
        { "Root", "Hips" },
        { "Spine1", "Spine" },
        { "Spine2", "Chest" },
        { "Spine3", "UpperChest" },
        // ... остальные кости
    }
}
```

### Вариант 2: Через Claude во время работы

```
Add custom bone mapping for "CustomSkeleton_to_Mecanim":
Root → Hips
Spine1 → Spine
Spine2 → Chest
Spine3 → UpperChest
Neck1 → Neck
Head1 → Head
...

Then retarget animation using this new mapping
```

### Вариант 3: Динамически через API

```csharp
var customMapping = new Dictionary<string, string>
{
    { "custom_bone_name", "Mecanim_bone_name" },
    // ...
};

AnimationRetargeting.AddCustomMapping("MyCustom_to_Mecanim", customMapping);
```

## AI-Powered маппинг (в разработке)

### Концепция

Для произвольных скелетов без предустановленного маппинга система будет использовать Claude AI для:

1. **Анализ структуры скелета**
   - Имена костей
   - Иерархия (parent-child)
   - Позиции и ориентация
   - Количество дочерних элементов

2. **Умный маппинг**
   - Сопоставление по семантике имен
   - Анализ позиций в иерархии
   - Сравнение с известными паттернами
   - Предложение вариантов для неоднозначных случаев

3. **Валидация результатов**
   - Проверка полноты маппинга
   - Предупреждения о потенциальных проблемах
   - Визуализация результата

### Пример использования AI-маппинга (будущее)

```
Analyze skeleton structure of Assets/Characters/Hero/hero_skeleton.fbx
Then create bone mapping from this skeleton to Unity Mecanim
Use AI to match bones by name semantics and hierarchy position
```

**Ожидаемый результат**:
```json
{
  "mapping": {
    "pelvis_joint": "Hips",
    "spine_joint_01": "Spine",
    "confidence": 0.95
  },
  "uncertain_mappings": [
    {
      "source": "spine_twist",
      "suggestions": ["Spine", "Chest"],
      "reason": "Ambiguous position in hierarchy"
    }
  ]
}
```

## Интеграция с Blender (в разработке)

### Планируемые возможности:

1. **Экспорт из Unity в Blender**
   ```
   Export character rig to Blender for manual animation adjustments
   ```

2. **Импорт из Blender в Unity**
   ```
   Import animation from Blender file with automatic retargeting
   ```

3. **Двусторонняя синхронизация**
   ```
   Sync animation Assets/Anim/walk.anim with Blender file C:/Blender/walk.blend
   Auto-retarget on each save
   ```

### Архитектура Blender интеграции

```
Unity Editor
     ↓ (FBX export)
Blender Python Script
     ↓ (bone analysis + AI mapping)
Claude API
     ↓ (mapping instructions)
Blender Python Script
     ↓ (animation adjustment)
Unity Import
     ↓ (AnimationClip)
Final Result
```

## Troubleshooting

### Проблема: "No preset mapping found"

**Причина**: Не существует предустановленного маппинга для указанных типов скелетов.

**Решение**:
1. Проверить поддерживаемые типы (см. начало документа)
2. Добавить пользовательский маппинг
3. Дождаться реализации AI-powered маппинга

### Проблема: Анимация выглядит искаженной

**Причины**:
- Различия в пропорциях скелетов
- Неправильный маппинг костей
- Отсутствие маппинга для важных костей

**Решение**:
1. Проверить маппинг в `AnimationRetargeting.cs`
2. Добавить недостающие кости в маппинг
3. Использовать Unity Humanoid Avatar для автоматической коррекции
4. Настроить Avatar Mask для игнорирования проблемных костей

### Проблема: Некоторые части тела не анимируются

**Причина**: Кости не найдены в маппинге.

**Решение**:
```
Analyze skeleton and show which bones are not mapped:
Source: Assets/Characters/hero_skeleton.fbx
Target: Mecanim
```

Затем добавить недостающие кости в маппинг.

### Проблема: "Animation clip not found"

**Причина**: Неверный путь к исходной анимации.

**Решение**:
```
Show all animation clips in Assets/Animations/ folder
```

Проверить правильность пути.

## Лучшие практики

### 1. Организация анимаций

```
Assets/
└── Animations/
    ├── Raw/              # Оригинальные импортированные анимации
    │   ├── Mixamo/
    │   ├── UE4/
    │   └── Custom/
    └── Player/           # Ретаргетированные анимации для использования
        ├── Locomotion/
        ├── Combat/
        └── Interactions/
```

### 2. Именование файлов

```
Формат: [action]_[variant]_[source].anim

Примеры:
- walk_normal_mixamo.anim → walk_normal.anim
- attack_sword_ue4.anim → attack_sword.anim
- idle_combat.anim
```

### 3. Batch обработка

Для множества анимаций:
```
Create a batch retargeting task:
1. Find all .anim files in Assets/Animations/Raw/Mixamo/
2. Retarget each from Mixamo to Mecanim
3. Save to Assets/Animations/Player/ with same names
4. Report success/failure for each
```

### 4. Версионирование

Сохраняйте оригиналы:
```
Assets/Animations/
├── Raw/              # Никогда не изменять
└── Player/           # Результаты ретаргетинга (можно переделать)
```

## Производительность

### Время обработки (примерно):

- Простая анимация (2 сек, 60 fps): ~1-2 сек
- Средняя анимация (5 сек, 60 fps): ~3-5 сек
- Сложная анимация (10 сек, 60 fps): ~5-10 сек

### Batch обработка:

- 10 анимаций: ~30-60 сек
- 50 анимаций: ~3-5 мин
- 100 анимаций: ~5-10 мин

## Roadmap

### ✅ Версия 0.1.0 (текущая)
- Базовый ретаргетинг с предустановленными маппингами
- Mixamo → Mecanim
- UE4 → Mecanim
- Пользовательские маппинги

### 🚧 Версия 0.2.0 (в разработке)
- AI-powered анализ скелетов
- Автоматический маппинг произвольных скелетов
- Визуализация маппинга в Unity Editor
- Batch обработка через UI

### 🔜 Версия 0.3.0 (запланировано)
- Интеграция с Blender Python API
- Двусторонняя синхронизация Unity ↔ Blender
- Экспорт/импорт через FBX с автоматическим ретаргетингом
- Поддержка пальцев рук и лицевой анимации

### 🔜 Версия 1.0.0 (цель)
- Полная автоматизация конвертации
- Поддержка всех популярных форматов скелетов
- UI инструменты в Unity Editor
- Документация и туториалы

## Примеры использования в реальных задачах

### Задача 1: Быстрое прототипирование персонажа

```
1. Download 10 animations from Mixamo (idle, walk, run, jump, etc.)
2. Batch import them to Assets/Animations/Raw/Mixamo/
3. Batch retarget all to Mecanim → Assets/Animations/Player/
4. Create Animator Controller with these animations
5. Test in Play Mode
```

### Задача 2: Миграция с UE4 на Unity

```
I have 50 UE4 animations in C:/UE4Project/Animations/
Import all of them to Unity Assets/Animations/Raw/UE4/
Then batch retarget from UE4 to Mecanim
Save results in Assets/Animations/Player/
```

### Задача 3: Кастомный скелет

```
I have a custom skeleton with non-standard bone names
Analyze the skeleton structure of Assets/Characters/Robot/skeleton.fbx
Compare with Unity Mecanim bone requirements
Suggest bone mapping for retargeting
```

## Дополнительные ресурсы

- Unity Humanoid Avatar Documentation
- Mixamo Character Animation Guide
- Unreal Engine Skeleton Reference
- Blender Python API Documentation

---

**Версия документа**: 0.1.0  
**Последнее обновление**: 2026-08-25T20:03:00Z  
**Статус**: Актуален

**Следующий шаг**: Реализация AI-powered маппинга для произвольных скелетов
