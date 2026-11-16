# FPSController и связанные системы игрока — статический разбор

**Статус:** анализ исходников; сцена `Demo Zone.unity` бинарная в этой рабочей копии, поэтому Inspector references и runtime поведение не подтверждены. Unity Play Mode/компиляция не проверялись.  
**Scope:** `FPSController`, камера/движение, body/head/camera bob, sway, water trigger и известные внешние управляющие вызовы. Код не менялся.

## 1. Назначение и основные части

`Assets/DarkTree FPS/Scripts/Player/FPSController.cs` — `CharacterController`-based движение и камера игрока.

- Горизонтальное движение относительно `camHolder.forward/right`.
- Вертикальный `moveDirection.y`, gravity и прыжок.
- Desktop/mobile input paths.
- Crouch через изменение высоты CharacterController и локальной позиции camera holder.
- Run/stamina gate и Animator состояния weapon holder.
- Mouse/touch look, чувствительность, smoothing и recoil.
- Ground/landing checks, fall damage и audio/Animator feedback.
- Water/climbing ветви.

Связанные компоненты: `PlayerStats`, `InputManager`, `WeaponManager`, Animator на `Weapon holder` и main camera, `CharacterController`, `CameraBob`, `HeadBob`, `Body`, `Sway`, `WaterVolumeTrigger`, `ActionWait`.

## 2. Инициализация и контракты сцены

`FPSController` требует `AudioSource` через `[RequireComponent]`. В `OnEnable()`:

- для desktop устанавливает `CursorLockMode.Confined` и скрывает cursor;
- сбрасывает static `isSwimming = false`;
- получает `CharacterController`, сохраняет высоту и bounds extents;
- ищет точный объект `Weapon holder`, Animator и `Camera.main` Animator;
- ищет `PlayerStats`, `WeaponManager`, `InputManager` через `FindObjectOfType`.

Сериализуемые/публичные поля включают movement speeds, jump force, crouch height, `lockCursor`, `camHolder`, sensitivity/smoothing, landing clips и `hardLandingTime`. Их значения в Demo Zone не считаны.

Код предполагает корректные ссылки/объекты: `camHolder`, `Weapon holder`, main camera, PlayerStats и WeaponManager. Null-check на всех путях отсутствует.

Static/shared состояние:

- `FPSController.crouch` — глобальный crouch flag;
- `FPSController.canMove` — глобальная блокировка движения;
- `FPSController.isSwimming` и `highestTriggerEdge` — состояние воды;
- `isClimbing` — instance field, но в просмотренных скриптах внешних присваиваний не найдено.

Static flags не привязаны к конкретному экземпляру игрока и требуют внимания при reload scene/respawn.

## 3. Жизненный цикл Update

В `Update()` последовательно выполняется:

1. Попытка восстановить `CharacterController`, если поле null. (Присваивание результата в показанном коде отсутствует: `GetComponent<CharacterController>();` вызывается, но `characterController` не присваивается.)
2. Пока не swimming, ведётся расчёт времени в воздухе. При приземлении:
   - если `airTime > hardLandingTime`, проигрывается `HardLanding`, вызывается `PlayerStats.ApplyDamage((int)(minFailingDamage * airTime))`, возможно проигрывается hard landing sound;
   - если время в воздухе > 0.3s, проигрывается `SoftLanding` и опциональный звук;
   - таймер сбрасывается.
3. При `canMove == false` метод возвращается. Это блокирует следующие действия этого Update, но fall-time section выше уже выполнился.
4. Выбирается `StandaloneMovement()` либо `MobileMovement()` по `InputManager.useMobileInput`.
5. Вызываются `Crouch()`, `Landing()`, `CharacterMovement()`.
6. Mouse look работает только если `mouseLookEnabled && !InventoryManager.showInventory && lockCursor`.
7. При ground, не swimming/climbing и stamina > 5 `Input.GetButtonDown("Jump")` снижает stamina на 5 и задаёт вертикальную скорость; затем вызывается `CharacterController.Move`.
8. Обновляется water-entry state и weapon-holder `Reloading` Animator bool.

Важно: есть два независимых airborne timers — `airTime` для fall damage и `inAirTime` в `Landing()` для weapon-holder `Landing` state после >0.5s.

## 4. Движение, run и crouch

### Desktop

- `StandaloneMovement()` задаёт Animator `Walk`, `InAir`, `Run`, `Crouch`, `RaiseWeapon`.
- `moveSpeedLocal = moveSpeed` при движении; run multiplier применяется при удержании `inputManager.Run`, stamina > 0, не climbing, не crouched, Walk=true.
- Crouch устанавливает `moveSpeedLocal = crouchSpeed`, гасит Walk/Run и меняет Animator crouch по наличию движения.
- KeyCode `Crouch` переключает static crouch; `Jump` KeyCode снимает crouch (это отдельная проверка от Input Manager axis Jump в Update).

### Mobile

- `MobileMovement()` читает shared joystick vector через `CheckMovement()`.
- Run определяется порогом `joystickInputVector.y > 0.5`, не KeyCode Run; применяется такой же speed multiplier.
- Crouch toggle и jump-to-stand ветви всё ещё вызывают `Input.GetKeyDown(inputManager.Crouch/Jump)`.
- Для мобильной кнопки также существует `CrouchMobile()` callback, который toggle-ит crouch.
- В общей части `Update()` прыжок всё равно проверяется `Input.GetButtonDown("Jump")`; возможное дублирование/платформенная доступность требует реального mobile test.

`Crouch()` плавно интерполирует высоту CharacterController к `crouchHeight`/default height и camera holder local Y к 0.2/0.4. В коде нет отдельного headroom test перед вставанием.

### Направление

`CharacterMovement()` использует normalized camera forward/right на земле (y компонента убираются, когда не swimming и `hitTheGround`). Desktop input vector нормализуется; mobile joystick vector напрямую не нормализуется в этой функции. Поведение diagonal speed на mobile может отличаться — проверить, прежде чем менять.

- Ground: движение по плоскости камеры.
- Swimming: направление использует camera forward/right и gravity выставляется 0.05.
- Climbing без swimming: направление вертикально вдоль transform.up плюс camera-right для horizontal input; оружие прячется.
- При swimming/climbing также выставляется Animator `HideWeapon`.

## 5. Прыжок, ground и падение

- `isGrounded()` сначала читает `CharacterController.isGrounded`; если false — делает Raycast вниз от transform position на `distanceToGround + 0.5f`.
- При положительном ground check выставляется `hitTheGround = true`.
- Прыжок требует `isGrounded()`, stamina > 5 и `Input.GetButtonDown("Jump")`, расходует 5 stamina, задаёт `moveDirection.y = jumpForce`.
- `moveDirection.y` уменьшается на gravity (обычно 10) каждый `CharacterMovement()`, кроме climbing.
- Падение > `hardLandingTime` наносит damage пропорционально времени в воздухе через PlayerStats API; падение >0.3s даёт soft landing feedback.
- В water режиме fall timer не увеличивается.

Проверить в сцене: ground collider/слои, расстояние raycast, `CharacterController` bounds, Animator states `HardLanding`, `SoftLanding`, `Landing` и урон/FX от падения.

## 6. Camera look и recoil

`MouseLook()` выбирает:

- desktop: `Input.GetAxis("Mouse X")`, `Input.GetAxis("Mouse Y")`;
- mobile: `InputManager.touchPanelLook * Time.deltaTime`.

Затем применяет sensitivity*smoothing, lerp smoothing и интегрирует в `_mouseAbsolute`; вертикаль ограничивается ±`clampY/2` (по умолчанию ±80°). Если `recoil` не нулевой, добавляет его один раз и сбрасывает.

Для работы требуется `lockCursor == true`; значение сериализуется и в исходнике default false. В Desktop baseline надо проверить Inspector значение и Cursor behavior. `InventoryManager` меняет cursor visible/lock state, а FPSController прекращает look при `showInventory`.

`PlayerStats.PlayerDeath()`/`RespawnPlayer()` меняет `controller.lockCursor = false`; действия по возврату контроля нужно читать вместе с respawn flow и UI.

`PlayerCloseToWall()` использует Raycast от Camera.main по её forward с distance/масштабом, основанным на `distanceToWallRaiseWeaponUp` и передаёт результат Animator `RaiseWeapon`/Weapon aim restrictions.

## 7. Вода и climbing

`WaterVolumeTrigger` при collider с tag `Player` выставляет static `FPSController.isSwimming`, обновляет `highestTriggerEdge`, прячет/показывает weapon через `WeaponManager.HideWeapon/UnhideWeapon`. Для tag `MainCamera` переключает post-processing profile.

Обнаружение water state и hide weapon — разные tag callbacks; требуется, чтобы Player collider и camera collider были настроены соответствующим образом. `OnTriggerExit` напрямую сбрасывает swimming независимо от других overlapping water volumes — многократные/пересекающиеся объемы требуют отдельной проверки.

`isClimbing` используется как режим в `FPSController`, Weapon fire gate и запрет run, но поиск присваиваний в C# scripts обнаружил только объявление/сброс crouch внутри контроллера, не присваивание `isClimbing`. Источник состояния может быть сценовым UnityEvent/анимацией/непросмотренным скриптом; установить его нужно поиском references в Inspector/assets.

## 8. Камера и motion feedback

- `CameraBob` передаёт Animator `isMoving` из desktop `Input.GetAxis` и `Run` из Run KeyCode. В его Update нет mobile ветви.
- `HeadBob` передаёт desktop axes и `InputManager.IsRunning()` в Animator только на земле и при `FPSController.canMove`; иначе сбрасывает параметры. Прямой mobile joystick support отсутствует в этом файле.
- `Body` отключает себя при mobile mode. На desktop управляет body Animator: moving/turn/ground/crouch и direction factors `InputManager.verticalFactor/horizontalFactor`; использует mouse axis и camera-holder rotation.
- `Sway` на desktop читает Mouse X/Y, учитывает inventory; на mobile читает `touchPanelLook`. Sway привязан к общей mouse/touch input и Weapon setup.

Состав конкретных Animator компонентов и их фактические assets в сцене не подтверждены.

## 9. Внешние блокировки и соседние системы

- `ActionWait.Update()` выставляет static `FPSController.canMove = false` пока активен action timer, скрывает Weapon и закрывает Inventory; после окончания снова ставит canMove=true.
- `PlayerStats` отвечает за health/stamina и смерть/respawn; FPSController читает stamina, применяет падение урон через `ApplyDamage`.
- `Weapon` читает `isSwimming`/`isClimbing` и recoil; `WeaponManager`/Animator получают Run/Walk/Reload/HideWeapon состояния.
- `InventoryManager` блокирует MouseLook и управляет cursor state.
- `WaterVolumeTrigger` меняет swimming и weapon visibility.
- `InputManager` предоставляет режим ввода и joystick/touch state.

У `canMove` один глобальный bool с несимметричным ownership: `ActionWait` безусловно возвращает его в true при окончании таймера. Если другие системы также используют этот static state, возможна конкуренция владельцев; в статическом search найден `ActionWait` как writer.

## 10. Потенциальные риски для воспроизведения

Это статические наблюдения, не подтверждённые runtime-баги.

1. В ветке `Update()` `GetComponent<CharacterController()` вызывается без присваивания обратно в `characterController`; после runtime-потери компонента/ссылки последующие методы могут продолжить получать null. Обычно `OnEnable()` задаёт ссылку.
2. `distanceToGround` вычисляется только в `OnEnable()`; изменение высоты CharacterController позже может сделать Raycast distance устаревшим.
3. `CameraLook` требует `lockCursor` true, но default false; проверить serialized value и взаимодействие с Inventory/Death/Respawn.
4. Mobile run ускоряет Player по joystick forward, а `PlayerStats` и `NPC.FollowToTargetBehavior()` определяют running через `InputManager.IsRunning()` (KeyCode). Ожидаемо это может разъединять скорость, stamina и NPC follow-speed на mobile.
5. CameraBob/HeadBob читают desktop axes напрямую; на mobile их Animator может не отражать joystick, если компоненты активны.
6. Прыжок имеет общий Unity axis callback и KeyCode ветвь для снятия crouch; на mobile нужно проверить доступность Jump и отсутствие двойной активации.
7. Static `crouch`, `canMove`, `isSwimming`, `highestTriggerEdge` требуют корректного reset при disable, scene transition и respawn; не все имеют локального владельца/reset.
8. `isSwimming` — один bool, не счётчик overlapping volumes; выход из одного water trigger может сбросить состояние, даже если игрок остаётся в другом объёме.
9. `WaterVolumeTrigger` без null-check использует WeaponManager и post-processing component refs.
10. `CharacterMovement()` вызывает `CharacterController.Move()` в `Update`; перемещение и ground/fall behavior нужно оценивать в Play Mode с текущим timestep/collisions.
11. `canMove` return стоит после fall-time calculation: при заблокированном движении airborne timer может продолжать накапливаться и после разблокировки привести к landing feedback/damage.
12. В ряде путей `PlayerStats.ApplyDamage()` и UI/Animator refs ожидаются настроенными; падение — отдельный источник интеграционного урона.

## 11. Проверочный сценарий этапа 2

Unity Editor недоступен для проверки этой сессии. В Unity открыть Build Settings сцену `Demo Zone.unity`, подтвердить Player hierarchy и сделать исходный прогон без изменений кода:

1. **Camera/cursor:** проверить `lockCursor`, desktop/mobile mode, mouse-look lock, вертикальный clamp и возврат cursor после Inventory close.
2. **Ground movement:** пройти вперёд/назад/вбок/диагональ; сравнить нормированное desktop движение с mobile joystick.
3. **Run/stamina:** удерживать Run и двигаться; записать скорость, Animator, stamina drain/restore. На mobile двигаться joystick вперёд и сравнить.
4. **Crouch:** toggle, проверить скорость, capsule height, camera Y, collider obstruction при вставании.
5. **Jump/landing:** прыжок на земле/в воздухе, мягкое и безопасное падение; проверить damage, audio и Animator без провоцирования смерти.
6. **Water:** вход/выход Player и MainCamera colliders; проверить movement, gravity, weapon visibility/post-processing; при наличии проверить overlapping volumes.
7. **Climbing:** если сцена/интеракции включают состояние, установить источник `isClimbing`, проверить движение вверх, скрытие оружия и запрет стрельбы.
8. **Block/death/respawn:** в безопасной копии проверить ActionWait freeze/unfreeze и отдельно смерть/respawn/cursor/camera/weapon state.

Для каждого шага фиксировать значения `canMove`, `crouch`, `isSwimming`, `isClimbing`, `lockCursor`, stamina, PlayerStats health, Animator parameters и Console errors. До Editor-прогона результат каждого шага имеет статус «не проверен».

## 12. Следующая граница работы

Это только описание текущих контрактов. Предпочтительный следующий практический шаг — после получения baseline выбрать ровно одну воспроизведённую связь (например, mobile run ↔ stamina) и задать минимальный тест с соседними `PlayerStats`/`InputManager`/Animator. Не менять одновременно движение, камеру, water и death/respawn.

## Источники

- `Assets/DarkTree FPS/Scripts/Player/FPSController.cs`
- `Assets/DarkTree FPS/Scripts/Player/PlayerStats.cs`
- `Assets/DarkTree FPS/Scripts/Player/InputManager` consumers: `Body.cs`, `CameraBob.cs`, `HeadBob.cs`, `Sway.cs`
- `Assets/DarkTree FPS/Scripts/Player/WaterVolumeTrigger.cs`
- `Assets/DarkTree FPS/Scripts/Player/ActionWait.cs`
- `Assets/DarkTree FPS/Scripts/Weapon/Weapon.cs`, `WeaponManager.cs`
- `Assets/DarkTree FPS/Scripts/Mobile and Inputs/InputManager.cs`, `Joystick.cs`, `TouchField.cs`
- `Assets/DarkTree FPS/Scripts/DT Inventory/Inventory/InventoryManager.cs`
- `Assets/DarkTree FPS/Scripts/NPC/NPC.cs`
- `ProjectSettings/InputManager.asset`, `ProjectSettings/EditorBuildSettings.asset`
