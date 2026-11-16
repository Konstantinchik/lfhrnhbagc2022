# Система оружия DarkTree FPS — проверенный обзор

**Статус:** статический анализ C# исходников. Unity Play Mode и сериализованные ссылки prefabs/assets ещё требуют отдельной проверки.

**Основная папка:** `Assets/DarkTree FPS/Scripts/Weapon/`.

> Ниже описаны факты по исходникам; предположения отдельно помечены как статические риски.

## 1. Состав системы

- `Weapon.cs` — ввод оружия, стрельба, перезарядка, прицеливание, расход патронов, эффекты и применение попаданий.
- `WeaponSettingSO.cs` — настройки оружия как `ScriptableObject`.
- `WeaponManager.cs` — обнаружение, экипировка и переключение оружия.
- `BalisticProjectile.cs` — pooled баллистический projectile и регистрация столкновений.
- `Rocket.cs` — ракета, взрыв, урон по области.
- `Grenade.cs` — таймер гранаты, взрыв и урон по области.
- `HitFXManager.cs` — пулы декалей и hit effects.
- `ObjectHealth.cs` — компонент здоровья разрушаемого объекта.
- `Recoil.cs` — поворотное смещение, плавно возвращающееся к нулю; прямой вызов `AddRecoil` из просмотренной `Weapon.cs` не найден.
- `Shell.cs` — в текущем скрипте только закомментированная логика.
- `Slot.cs` — хранит `storedWeapon`/`storedDropObject`, проверяет занятость.
- `Lean.cs` — наклон, использующий `InputManager`, `WeaponManager` и Animator.
- `ReticleAnimation.cs` — визуальная анимация прицела.

## 2. Типы оружия и настройка

Реальный enum из `WeaponSettingSO.cs`:

```csharp
enum WeaponType { SMG, SniperRiffle, Pistol, Shotgun, Melee, Grenade, RocketLauncher }
```

`Rifle` и `Sniper` в этом enum отсутствуют. Создание asset задано атрибутом `CreateAssetMenu(fileName = "Weapon", menuName = "Weapon Data", order = 0)`, то есть меню Unity — **Create → Weapon Data**.

Поля `WeaponSettingSO` сгруппированы по назначению:

- Идентификация: `weaponName`, `weaponType`, `weaponIcon`.
- Эффекты/аудио: `MuzzleFlashParticlesFX`, `shotSFX`, `reloadingSFX`, `emptySFX`, `shell`, `shellsPoolSize`, `shellEjectingForce`.
- Баланс: `damageMinimum`, `damageMaximum`, `rigidBodyHitForce`, `fireRate`, `spread`, `recoil`, `weaponShotNPCDetectionDistance`.
- Прицел: `canUseScope`, `timeToActivateScope`, `scopeFOV`, `scopeSensitivityX/Y`.
- Melee: дальность, rate, damage, hit force/time и `meleeHitFX`.
- Баллистика: `bulletInitialVelocity`, `airResistanceForce`, `projectile`, `projectilePoolSize` (ограничен Range 1–100).
- Граната: `grenadePrefab`, `throwForce`.
- Дробовик: `shotgunLoadingClip`.

`Weapon.GetWeaponSettings()` копирует часть настроек в runtime-поля; отдельные значения затем также читаются прямо из `weaponSetting`.

## 3. Боезапас, inventory и экипировка

- Боезапас активного оружия хранится в `currentItem.stackSize`; `maxAmmo` используется как предел магазина при reload.
- Запасные патроны ищутся среди `DTFPSInventoryExtended.ammoItems` по `ammoItemID` и `Item.id`.
- Обычная перезарядка уменьшает stack подходящих ammo items и удаляет опустевшие inventory entries через `inventory.RemoveItem`.
- Имя weapon задаётся из `weaponSetting.weaponName` в `Awake()`. `WeaponManager` сопоставляет его с `Item.title` строковым сравнением.
- `WeaponManager.Awake()` ищет оружия в детях `Sway` (включая inactive) и получает inventory. При выборе предмета назначает его `weapon.currentItem`.
- Граната расходуется отдельным вызовом `DTFPSInventoryExtended.UseGrenade(inventory)` в `ThrowGrenade()`.

Это явные границы связности: оружие зависит от Inventory/Item и `WeaponManager`; изменение названий предметов или IDs может нарушить экипировку/боезапас.

## 4. Инициализация и зависимости сцены

`Weapon.Awake()` читает `weaponSetting.weaponName`. В `Start()` компонент загружает настройки, при необходимости создаёт пулы и ищет зависимости сцены: `FPSController`, `Camera.main`, `WeaponCamera`, `AmmoText`, `WeaponText`, `WeaponManager`, `Sway`, `ReticleAnimation`, `CoverList`, `HitFXManager`, `Weapon holder`, Inventory и `InputManager`. В коде также используются объект `GamePrefab` и назначенные transforms/prefabs.

Многие ссылки получаются через `FindObjectOfType`/`GameObject.Find`; часть null-check отсутствует. Поэтому одного факта компиляции недостаточно: нужно проверять сцену и weapon prefabs.

`WeaponManager` зависит от `Sway`, `SoundManager`, списка `weapons`, `equipmentPanel`, Inventory и `weaponHolderAnimator`.

## 5. Ввод и стрельба

`Weapon.Update()` использует legacy Input API и проверяет mobile mode, смерть, swimming, `Time.timeScale`, открытый Inventory и climbing.

- Удержание запускает автоматический огонь для соответствующих weapon types; Pistol исключён из этой ветви.
- Pistol и одиночный режим используют `GetKeyDown`.
- RocketLauncher направляется в отдельный `FireRocketLauncher()`.
- Mobile hooks: `FireMobile()` и `AimMobile()`.
- Обычный `Fire()` проверяет cooldown, reload, `canShot`, climbing и stack боеприпасов. При успехе уменьшает `currentItem.stackSize`, запускает FX, рассчитывает damage и активирует projectile из пула.
- Shotgun вызывает `ProjectilesManagerShotgun(10)`: в коде на один выстрел запускается 10 pooled projectiles. Число не берётся из поля `bulletsPerShot`.
- Обычный разброс зависит от spread и скорости движения; при aiming уменьшается для соответствующих типов. У дробовика отдельный spread (`spread * 5`). SniperRiffle при aiming запускает projectile без добавочного разброса.
- После выстрела вызываются reticle animation, `CoverList.NotifyNPCAboutShot` и обновляется recoil контроллера.

`calculatedDamage` задаётся через целочисленный `Random.Range(damageMin, damageMax)`. В Unity верхняя граница integer overload исключается; значит, `damageMax` не выбирается этим вызовом. Это статический вывод по API, следует сверить с намерением баланса.

## 6. Перезарядка

### Обычное оружие

`ReloadBegin()` отказывает, если игрок близко к стене или нет ammo. Иначе включает `reloading`, выключает `canShot`, сбрасывает aim, запускает Animator `Reload`, звук и `Invoke("ReloadEnd", reloadAnimationDuration)`. Для RocketLauncher отдельно планируется `ShowMissile`.

`ReloadEnd()` переносит доступные резервные патроны в `currentItem.stackSize` до `maxAmmo`, удаляет пустые ammo entries, затем разрешает стрельбу.

### Shotgun

Дробовик использует отдельный поэтапный процесс через `ShotgunReload()`, Animator bool `Reloading`, `AddShotgunAmmo()` и методы звуковых callbacks `PlayReloadSfxShotgun()`/`PlayLoadSfxShotgun()`. Часть переходов зависит от Animator и его Animation Events; точную работу нужно сверять с контроллером и prefab.

## 7. Прицеливание и FX

- Aim определяется в `Update()` через input и `controller.PlayerCloseToWall()`.
- При прицеливании скрывается reticle и меняются параметры Sway.
- `SetAim()` управляет Animator bool `Aim` и чувствительностью контроллера.
- После `timeToActivateScope` включается `weaponManager.scopeUI`, если оружие поддерживает scope.
- `AimFOVShift()` плавно меняет FOV основной и weapon camera.
- `PlayFX()` использует Animator states `Shot`, `Shot1`, `Shot2` или `AimShot`, запускает muzzle flash, звук и eject shell.
- Shell pool и ballistic projectile pool создаются в `Start()` для соответствующих типов; Rocket/Grenade создаются отдельно.

Имена Animator states/parameters и Animation Events — runtime-контракты: их нельзя безопасно переименовать только по C#.

## 8. Попадания, projectile и урон

### Баллистическая пуля

`BalisticProjectile.OnEnable()` прикладывает Rigidbody `VelocityChange` по `transform.forward` и запоминает позицию. В `Update()` проверяется `Physics.Linecast(lastPosition, transform.position)`. При попадании вызывается `weapon.ApplyHit(hit)`, затем объект выключается. Projectile также выключается после `livingTime = 1f`. В `OnDisable()` сбрасываются timer, Rigidbody velocity и позиция.

Поле `airResistance` присваивается при создании пула, но в `BalisticProjectile.cs` не читается. Поэтому этот скрипт сам по себе не реализует сопротивление воздуха; на полёт могут влиять Rigidbody и настройки prefab.

### Применение попадания оружия

`Weapon.ApplyHit()` запускает hit FX по тегам, уменьшает `ObjectHealth.health`, при необходимости создаёт decal, применяет силу к Rigidbody и передаёт урон через `PlayerHitTarget`/`ZombieNPC`.

Обработка зависит от тегов (`Wood`, `Concrete`, `Dirt`, `Metal`, `Flesh`, `Target`), компонентов и предварительно созданных FX pools.

### Rocket

`FireRocketLauncher()` создаёт `weaponSetting.projectile`, получает компонент `Rocket`, ставит его в позицию/поворот камеры, вызывает `BeginMovement()`, списывает предмет и скрывает `MissileObject`. `Rocket` движется Rigidbody, проверяет Linecast и вызывает explosion при столкновении или по timeout (5 сек). Взрыв использует `Physics.OverlapSphere`, воздействует на NPC, zombie, `ObjectHealth`, `PlayerStats` и Rigidbody, включает FX и уничтожает ракету.

### Grenade

Для Grenade `Fire()` ставит Animator trigger `Throw` и блокирует повторный бросок флагом. Предполагается Animation Event в `ThrowGrenade()`: там создаётся `weaponSetting.grenadePrefab`, прикладывается сила по forward объекта, снимается флаг броска и расходуется grenade item.

`Grenade.OnEnable()` создаёт FX, если prefab назначен, и запускает coroutine на `explosionTimer`. Взрыв применяет overlap damage/force, активирует FX и уничтожает объект.

### Важная общая деталь взрывного урона

`Rocket` и `Grenade` вызывают `GameObject.Find("Player").transform` как damage sender для NPC и напрямую меняют `PlayerStats.health`; это отличается от некоторых других путей урона, где вызывается `PlayerStats.ApplyDamage()`. Возможные отличия damage FX/death side effects требуют Play Mode проверки. Не считать это багом без теста.

## 9. Melee

Для `WeaponType.Melee` `Fire()` запускает звук/Animator `Shot`, затем `Invoke("MeleeHit", meleeHitTime)`. `MeleeHit()` делает Raycast от основной камеры на `meleeAttackDistance`, меняет `ObjectHealth`, передаёт удар `ZombieNPC`/`PlayerHitTarget` и применяет силу Rigidbody.

## 10. WeaponManager и переключение

`WeaponManager` активирует weapon, сопоставляя `weapon.weaponName` с `equipmentPanel[i].equipedItem.title`, использует анимации `Hide`/`Unhide` и задержки, поддерживает мобильный выбор слота, auto-equip, load equip, death hide и `HideAll()`.

Следовательно, оружейная система связана с inventory, input, Animator, Player controller, CoverList/NPC, hit effects, сценовыми объектами и prefab references.

## 11. Потенциальные риски по статическому анализу

Ниже не воспроизведённые runtime-дефекты, а места, которые стоит проверить отдельными тестами:

1. `Weapon.Awake()` обращается к `weaponSetting.weaponName` без проверки null.
2. `Start()` требует значительное число сценовых ссылок; отсутствующий объект/ref может вызвать исключение.
3. `Fire()` использует `currentItem.stackSize`; при неверном порядке экипировки возможен null reference.
4. `WeaponManager.weapons.Add()` требует инициализированного списка.
5. Сопоставление Item↔Weapon основано на строках `weaponName`/`title`.
6. `GetWeaponSettings()` присваивает `shellForce = weaponSetting.shellsPoolSize`, хотя eject force далее читается из `weaponSetting.shellEjectingForce`; поле/присваивание выглядят лишними или ошибочными.
7. `BalisticProjectile.airResistance` не используется в просмотренном компоненте.
8. При теге `Target` `Weapon.ApplyHit()` обращается к `hit.rigidbody.isKinematic`; код не проверяет наличие Rigidbody в этой ветви.
9. `DecalManager()` в некоторых ветвях использует массив `decals`, а активируемые объекты берёт из `hitFXManager`; `decals` в просмотренном файле не инициализируется. Нужно проверить вызовы и Play Mode до изменения.
10. Взрывной урон по игроку проходит прямым изменением health, не через тот же путь, что `ApplyDamage`; проверить UI/FX/death.
11. Explosion scripts требуют объект с именем `Player`.
12. `HitFXManager` ожидает назначенные эффекты и decal prefabs; это нужно проверить в Inspector.

## 12. Старые описания: исправленные неточности

- Типы оружия — `SMG`/`SniperRiffle`, не `Rifle`/`Sniper`.
- Поля damage — `damageMinimum`/`damageMaximum` в SO; runtime-копии в `Weapon` называются `damageMin`/`damageMax`.
- Попадание идёт через `BalisticProjectile` → `Weapon.ApplyHit`, не через ранее приведённый `ProcessHit`.
- `WeaponSettingSO` создаётся через `Create → Weapon Data`.
- Shotgun код вызывает `ProjectilesManagerShotgun(10)`, не использует `bulletsPerShot`.
- Grenade/rocket логика и ammo storage в старых фрагментах были схематичными; см. фактический поток выше.

## 13. Следующие этапы валидации

1. Найти реальные WeaponSettingSO assets и weapon prefabs; проверить назначения полей, IDs/titles и Animator.
2. Составить список Animator states, parameters и Animation Events для reload, shotgun, grenade, rocket.
3. В Unity протестировать отдельно: equip/switch, обычный выстрел, reload, shotgun reload, melee hit, grenade throw/explosion, rocket hit/timeout.
4. Проверить damage/death path и tags/layers на тестовых целях.
5. Исправлять только воспроизведённые проблемы маленькими изменениями, каждую проверять и коммитить отдельно.

## Источники

Статический разбор проводился по `Weapon.cs`, `WeaponManager.cs`, `WeaponSettingSO.cs`, `BalisticProjectile.cs`, `Rocket.cs`, `Grenade.cs`, `HitFXManager.cs`, `ObjectHealth.cs`, `Recoil.cs`, `Shell.cs`, `Slot.cs`, `Lean.cs`, а также `DTFPSInventroyExtended.cs` и связанным ссылкам.
