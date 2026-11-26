# Анимационные контракты — что связывает клипы и код

**Статус:** статический разбор: поиск по C#-скриптам, файлам `.controller`, `.anim` и `.fbx.meta`. Play Mode не проверялся. Код и ассеты не изменялись.
**Система в карте проекта:** S08 «Анимации персонажа и оружия» ([карта систем](./PROJECT_SYSTEMS_MAP.md)).
**Чего не видно:** какой именно Animator Controller назначен какому объекту в сцене `Demo Zone` (она бинарная). Соответствие «скрипт → контроллер» ниже выведено по совпадению имён параметров и состояний.

---

## 1. Зачем этот документ

Анимации в проекте связаны с кодом **строками**: код вызывает `animator.Play("Hide")` или `SetBool("Run", true)`, а клип в нужный кадр вызывает метод по имени — «событие анимации» (Animation Event). Компилятор эти строки не проверяет. Если переименовать состояние в контроллере или метод в скрипте, проект соберётся без ошибок, а в игре перестанет работать перезарядка или бросок гранаты.

Здесь собраны все такие строки. Перед переименованием чего-либо из списка ищите его по проекту.

## 2. Какие аниматоры есть

В проекте 51 файл `.controller`. Рабочие (остальное — наследие ассета, тестовые и старые версии):

| Контроллер | Чем управляет | Параметры |
|---|---|---|
| `Animations/Weapon holder.controller` | «Держатель оружия»: общая поза рук при ходьбе, беге, приседе, смене оружия | `Walk`, `Run`, `Crouch`, `HideWeapon`, `InAir`, `Fire`, `RaiseWeapon`, `Reloading` |
| `Animations/<Оружие> animator.controller` (AK74M, AR15, SCAR, Glock, Makarov, Barrett, RPG, Knife, Grenade…) | Анимации конкретного оружия: выстрел, перезарядка, доставание | `Aim`, `Use`, `Take` |
| `Animations/Shotgun New.controller`, `WeaponsNew Rigged/Shotgun/Shotgun.controller` | Дробовик — отдельная схема с патронами по одному | `Reloading`, `Ammo Count`, `Aim` |
| `Animations/Controller animations/Camera & Recoil.controller` | Камера: покачивание, приземление, толчок от удара | `Horizontal`, `Vertical`, `Run` |
| `Animations/Controller animations/CameraBobbing.controller` | Покачивание камеры при движении | `isMoving`, `Run` |
| `Animations/Controller animations/Weapon additional animations.controller` | Наклоны влево/вправо | `LeanLeft`, `LeanRight` |
| `Content/NPC/ThirdPersonAnimatorController.controller` | Тело NPC-стрелка | `Forward`, `Horizontal`, `Turn`, `Crouch`, `OnGround`, `Jump`, `JumpLeg`, `LookAtTarget`, `Armed` |
| `Content/NPC/ThirdPersonAnimatorControllerPlayer.controller` | Тело игрока (вид от третьего лица/тень) | то же + `IsMoving` вместо `Armed` |
| `Content/Zombie/ZombieAnimator.controller` | Зомби | `Vertical` |

Состояния ключевых контроллеров:

- **Weapon holder:** `WeaponHolderIdle`, `Idle`, `Walk`, `WalkStrafe`, `Run`, `Crouch`, `WeaponHolderAir`, `Landing`, `Hide`, `Unhide`, `WeaponHolderRaiseWeapon`, `Blend Tree`.
- **AK74M (типовой для оружия):** `Idle`, `AK74M Idle`, `Default position`, `Shot`, `Shot1`, `Shot3`, `Aim`, `Aim Reverse`, `Reload`, `Take`, `Take Begin`, `Take Begin 0`.
- **Camera & Recoil:** `CameraBob`, `CameraBobMovement`, `HeadBobRun`, `SoftLanding`, `HardLanding`, `CameraKick`.
- **NPC:** `Empty`, `Crouching`, `Airborne`, `Aim`, `Armed Strafe`, `Shot`.
- **Зомби:** `Idle`, `Movement`, `Attack`.

## 3. Строки, которые код отправляет в аниматоры

### Игрок и камера

| Скрипт | Вызов | Имена |
|---|---|---|
| `FPSController.cs` | `SetBool` | `Walk`, `Run`, `Crouch`, `InAir`, `HideWeapon`, `RaiseWeapon`, `Reloading` |
| `FPSController.cs` | `Play` | `Landing`, `SoftLanding`, `HardLanding`, `Unhide` |
| `Body.cs` | `SetBool` / `SetFloat` | `Crouch`, `IsMoving`, `OnGround` / `Forward`, `Horizontal`, `Turn` |
| `CameraBob.cs` | `SetBool` | `isMoving`, `Run` |
| `HeadBob.cs` | `SetBool` / `SetFloat` | `Run` / `Horizontal`, `Vertical` |
| `Lean.cs` | `SetBool` | `LeanLeft`, `LeanRight` |
| `Anomaly.cs`, `ZombieNPC.cs` | `Play` | `CameraKick` (толчок камеры игрока) |

Обратите внимание на регистр: `Body.cs` пишет `IsMoving`, `CameraBob.cs` — `isMoving`. Это разные параметры разных контроллеров, и оба написания должны остаться как есть.

### Оружие

| Скрипт | Вызов | Имена |
|---|---|---|
| `Weapon.cs` | `Play` | `Shot`, `Shot1`, `Shot2`, `Reload`, `ReloadBegin`, `Take`, `Idle`, `Walk`, `Default position` |
| `Weapon.cs` | `SetBool` / `GetBool` | `Aim`, `Reloading`, `Use` |
| `Weapon.cs` | `SetTrigger` | `Throw` |
| `Weapon.cs` | проверка наличия состояния | `AimShot`, `Shot` |
| `WeaponManager.cs` | `Play` | `Hide`, `Unhide` |

### NPC и зомби

| Скрипт | Вызов | Имена |
|---|---|---|
| `NPC.cs` | `Play` | `Shot`, `Reload` |
| `NPC.cs`, `ZombieNPC.cs` | `SetBool` | `Crouch`, `OnGround`, `LookAtTarget` |
| `NPC.cs`, `ZombieNPC.cs` | `SetFloat` | `Forward`, `Horizontal`, `Turn`, `Jump`, `JumpLeg` |
| `ZombieNPC.cs` | `Play` | `Attack` |

## 4. События анимаций: клип вызывает метод

Событие срабатывает в заданный кадр клипа и вызывает метод с указанным именем на объекте, где висит Animator. Метод должен быть на том же объекте.

| Метод | Кто вызывает (клип) | Где реализован | Что делает |
|---|---|---|---|
| `PlayFootstep` | 18 FBX-клипов ходьбы и бега (`rifle run.fbx`, `Rifle Walk.fbx`, `HumanoidRun.fbx` и др.), а также `Run.anim` и `Walk.anim` | `FootstepSound.cs:43`; заглушка — `EmptyAnimationEventFix.cs:7` | Звук шага |
| `ThrowGrenade` | `Grenade.fbx`, `GrenadeRigged.fbx` | `Weapon.cs:497` | Создаёт гранату и списывает её из инвентаря |
| `AddShotgunAmmo` | `Shotgun.fbx` (обе версии) | `Weapon.cs:986` | Добавляет один патрон при перезарядке дробовика |
| `PlayLoadSfxShotgun` | `Shotgun.fbx` (обе версии) | `Weapon.cs:863` | Звук вставки патрона |
| `PlayReloadSfxShotgun` | `Shotgun.fbx` (обе версии) | `Weapon.cs:858` | Звук передёргивания |
| `SetReloadingStateFalse` | `Shotgun.fbx` (обе версии) | `Weapon.cs:984` | Снимает флаг «идёт перезарядка» |
| `FightHit` | `zombie attack.fbx` | `ZombieNPC.cs:226` | Момент нанесения урона зомби |
| `Throw` | `Grenade_throw.anim` | **не найден в коде** | — |

`EmptyAnimationEventFix` — пустой приёмник `PlayFootstep`: его вешают на объекты, которые проигрывают клипы ходьбы, но шагов издавать не должны, чтобы Unity не писала ошибку «у события нет получателя».

## 5. Отложенные вызовы по имени

Ещё один вид строковой связи — `Invoke("ИмяМетода", задержка)`. Задержки подобраны под длину анимаций, поэтому при замене клипа их нужно пересматривать.

| Скрипт | Методы |
|---|---|
| `Weapon.cs` | `ReloadEnd` (через `reloadAnimationDuration`), `MeleeHit`, `ShowMissile` |
| `WeaponManager.cs` | `HideWeapon` (0,5 с), `UnhideWeapon` (0,6 с), `SetChangingWeaponFalse` (0,5 с) |
| `NPC.cs` | `Shot`, `AddAmmo`, `GoToIdle` |
| `Shell.cs` | `ChangeLayer` |

Важное следствие: **перезарядка автоматов привязана ко времени, а не к событию клипа** — патроны добавляются через `reloadAnimationDuration` секунд после начала (`Weapon.cs:641`). Перезарядка дробовика, наоборот, целиком управляется событиями клипа (раздел 4).

## 6. Наблюдения

### Подтверждено

1. У события `Throw` в `Grenade_throw.anim` нет метода-получателя в коде. Бросок гранаты в рабочей версии идёт через событие `ThrowGrenade` из FBX.
2. **Анимация `Shot2` никогда не проигрывается.** Вариант выстрела выбирается через `Random.Range(0, 2)` (`Weapon.cs:928`), а для целых чисел верхняя граница не включается: получается только 0 или 1. Ветка `range == 2` с `Play("Shot2")` (`:934-935`) недостижима. Попутно это прячет несоответствие в данных: `Shot2` есть в 13 оружейных контроллерах, а в `AK74M animator.controller` его нет (там `Shot`, `Shot1`, `Shot3`) — если расширить диапазон случайного выбора, для AK74M появится обращение к несуществующему состоянию.
3. Не у всех оружейных контроллеров одинаковый набор параметров: у дробовика нет `Use` и `Take`. Это уже приводило к предупреждениям и было закрыто проверками в коде (см. [UNITY_CONSOLE_LOG.md](./UNITY_CONSOLE_LOG.md)).
4. Состояния `AimShot` нет в `Weapon holder.controller`; код проверяет его наличие перед использованием.
5. У `WeaponManager` есть `CancelInvoke("UnhideDelay")` в закомментированной строке — метода `UnhideDelay` не существует.

### Требует проверки в Unity

- Соответствие «объект в сцене → контроллер» для всех 10 видов оружия.
- Что у каждого оружейного контроллера есть все состояния, которые вызывает `Weapon.cs` для его типа оружия.
- Что на объектах с клипами ходьбы всегда есть либо `FootstepSound`, либо `EmptyAnimationEventFix`.

## 7. Модульная граница

**Безопасно:** менять сами клипы (позы, длительность кадров), скорости переходов, добавлять новые состояния и параметры.

**Опасно без поиска по проекту:** переименовывать состояния и параметры из разделов 3; переименовывать, удалять или делать приватными в другом классе методы из раздела 4; переносить компонент со скриптом-получателем на другой объект, чем Animator; менять длительность клипов перезарядки и смены оружия, не меняя задержки из раздела 5.

**Как проверять после изменений:** готовый пример проверки совместимости контроллера и кода — `Assets/Tests/WeaponAnimatorCompatibilityDebugTest.cs`.
