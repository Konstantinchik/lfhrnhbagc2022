# Система оружия — часть 3: проверка по исходному коду

> Эта часть сверяет обзор из [части 1](./WEAPON_SYSTEM_1.md) и [части 2](./WEAPON_SYSTEM_2.md) с фактическими скриптами. Исходники имеют приоритет над упрощёнными примерами документации.

## Проверенные файлы

- `Assets/DarkTree FPS/Scripts/Weapon/Weapon.cs`
- `Assets/DarkTree FPS/Scripts/Weapon/WeaponManager.cs`
- `Assets/DarkTree FPS/Scripts/Weapon/WeaponSettingSO.cs`
- `Assets/DarkTree FPS/Scripts/Weapon/BalisticProjectile.cs`
- `Assets/DarkTree FPS/Scripts/Weapon/Rocket.cs`
- `Assets/DarkTree FPS/Scripts/Weapon/Grenade.cs`

## Фактическая архитектура

### Конфигурация и runtime

`WeaponSettingSO` хранит общие настройки типа оружия: урон, скорострельность, разброс, recoil, аудио/эффекты, пул гильз и баллистических снарядов, настройки прицела, рукопашной атаки и гранат.

`Weapon.Awake()` сразу читает `weaponSetting.weaponName`. `Weapon.Start()` копирует остальные настройки через `GetWeaponSettings()`, создаёт пулы и ищет зависимости сцены через `FindObjectOfType`/`GameObject.Find`.

### Оружие и предмет инвентаря

Боезапас оружия хранится в `currentItem.stackSize`; запасные патроны находятся в `DTFPSInventoryExtended.ammoItems` и выбираются по `ammoItemID`. Поля `Weapon.maxAmmo` и `Weapon.currentItem` связывают оружие с предметом и его ограничением магазина.

`WeaponManager` собирает оружия из дочерних объектов `Sway` в `Awake`, переключает активное оружие по слотам экипировки и имени предмета. Соответствие фактически основано на равенстве `weapon.weaponName` и `item.title`.

### Стрельба

- `Weapon.Update()` читает старый Unity Input API (`Input.GetKey`, `Input.GetKeyDown`) и выбирает автоматический/одиночный огонь с отдельными условиями для пистолета и гранатомёта.
- `Fire()` проверяет `nextFireTime`, `reloading`, `canShot`, лазание и наличие патронов. При успешном выстреле уменьшает `currentItem.stackSize`, запускает эффекты и выбирает обычный или дробовой менеджер снарядов.
- Обычные пули берутся из пула `BalisticProjectilesPool()`. Хит и урон обрабатывает `BalisticProjectile`, вызывая `Weapon.ApplyHit`.
- Для дробовика `ProjectilesManagerShotgun(10)` создаёт **10** направлений за выстрел. В `WEAPON_SYSTEM_2.md` пример говорит о 8 — это пример, не фактическое значение реализации.
- `calculatedDamage = Random.Range(damageMin, damageMax)`. Для целочисленного `Random.Range(min, max)` верхняя граница исключается, поэтому максимальное значение `damageMax` фактически не выбирается.
- Ракета создаётся отдельно через `Instantiate(weaponSetting.projectile)` и `Rocket.BeginMovement()`, не через обычный пул баллистических пуль.
- Граната создаётся по Animation Event через `ThrowGrenade()` и после этого списывается через `DTFPSInventoryExtended.UseGrenade(inventory)`.
- Melee использует Raycast и отдельные настройки дальности/скорости/урона.

### Перезарядка

Обычная перезарядка запускается `ReloadBegin()`, а завершение через `Invoke("ReloadEnd", reloadAnimationDuration)`. `ReloadEnd()` собирает подходящие ammo items, переносит доступный боезапас в `currentItem.stackSize` и удаляет опустевшие inventory entries.

Дробовик использует отдельный пошаговый Animator-процесс (`ShotgunReload`, `AddShotgunAmmo`, звуковые callbacks), а не тот же простой перенос после одного таймера.

## Расхождения и важные замечания к предыдущим частям

1. Enum в коде: `SMG`, `SniperRiffle`, `Pistol`, `Shotgun`, `Melee`, `Grenade`, `RocketLauncher`. В документах Part 1 перечислены `Rifle`/`Sniper`, которых в enum нет.
2. `WeaponSettingSO` использует имена `damageMinimum`, `damageMaximum`, `rigidBodyHitForce`, `canUseScope`; прежние фрагменты с `damageMin`, `damageMax`, `useScope` — концептуальный псевдокод, не реальные имена API.
3. Попадание обрабатывается не методом `ProcessHit` из части 1. Реальный путь — `BalisticProjectile` → `Weapon.ApplyHit(RaycastHit)`.
4. `WeaponSettingSO` asset menu — `Create → Weapon Data`; пример меню `Create → Weapon → Weapon Settings` в предыдущей документации не соответствует текущему атрибуту `CreateAssetMenu`.
5. Реализация прицеливания распределена между `Update()`, `SetAim()` и `AimFOVShift()`. `scopeUI` включается после таймера; одновременно меняются FOV основной и weapon camera и чувствительность контроллера.
6. `ShellsPool()` и `BalisticProjectilesPool()` создают объекты в `Start`, но не всякое оружие использует каждый пул. Rocket/Grenade/Melee исключены из обычного projectile pool; shell pool зависит от типа и наличия префаба.

## Риски надёжности, видимые при чтении кода

Это результаты статического анализа; поведение не подтверждалось Play Mode тестом.

- `Weapon.Awake()` не проверяет `weaponSetting == null` перед чтением `weaponName`.
- `Start()` предполагает существование множества объектов сцены (`WeaponCamera`, `AmmoText`, `WeaponText`, `Weapon holder`, `GamePrefab`, `FPSController`, `WeaponManager`, `HitFXManager`, `Sway`, `ReticleAnimation`, `DTInventory`, `InputManager`, `CoverList`). Отсутствующая ссылка может дать `NullReferenceException`.
- `Fire()` обращается к `currentItem.stackSize` без проверки `currentItem != null` после прохождения базовых ограничений.
- `WeaponManager.Awake()` делает `weapons.Add(...)`; список должен быть инициализирован в инспекторе/сериализован, иначе возможна ошибка.
- Поиск соответствия предмета оружию по строковому имени хрупок к переименованию/локализации/дубликатам.
- Несколько полей `WeaponSettingSO` и `Weapon` копируются/используются неочевидно. Например, в `GetWeaponSettings()` `shellForce` присваивается из `shellsPoolSize`, хотя фактический выброс гильзы использует `weaponSetting.shellEjectingForce` напрямую. Поле `shellForce` выглядит неиспользуемым в изученном скрипте.
- `Random.Range(damageMin, damageMax)` с int исключает максимум; проверить, соответствует ли это игровому балансу.
- В `Update()` часть UI/анимационных параметров и зависимости предполагаются доступными всегда. Это нужно учитывать при создании тестовой сцены.

## Предлагаемый порядок дальнейшего разбора

1. **Инвентарь и экипировка:** проследить создание/удаление `Item`, `EquipmentPanel`, ammo items и переходы вызовов `WeaponManager`.
2. **Баллистика и попадания:** сверить `BalisticProjectile.cs`, урон игроку/NPC/ObjectHealth, маски слоёв, pooling и время жизни.
3. **Анимационные контракты:** перечислить используемые состояния/параметры Animator и Animation Events для reload, shotgun, grenade, rocket.
4. **Данные проекта:** проверить реальные `WeaponSettingSO` assets и prefab references, а не только дефолтные поля скрипта.
5. После статического обзора выбрать один дефект, добавить минимальное исправление и проверить его в Unity Play Mode.

## Как фиксировать новые знания проекта

Подтверждённые наблюдения следует добавлять в функциональные документы `docs/` с указанием источников/путей и пометкой, если вывод пока статический. Повторяющиеся правила для будущих задач уместно добавлять в проектный skill `.agents/skills/darktree-unity-project/SKILL.md`; длительный текущий этап — вести как handoff/task в `docs/ai-tasks/` согласно owner-prefix правилам. Не переносить туда догадки как установленный факт.
