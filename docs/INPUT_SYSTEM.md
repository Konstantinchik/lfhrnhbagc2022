# Ввод игрока — инвентаризация (этап 1)

**Статус:** статический анализ исходников; Play Mode и сериализованные callbacks сцен/префабов не проверены.

**Scope:** `InputManager`, desktop/mobile consumers, `Joystick`, `MobileInputRig`, `TouchField`. Поведение не менялось.

## 1. Сводка потоков ввода

```text
Desktop legacy Input API ──► InputManager (smooth axes, KeyCode config)
            ├──────────────► FPSController / Weapon / Inventory / Lean / UseObjects
            ├──────────────► Body / CameraBob / HeadBob
            └──────────────► NPC (ссылка получена; прямой use пока не установлен)

Mobile UI EventSystem ──► Joystick ──► InputManager.joystickInputVector ──► FPSController
                     ├─► TouchField ─► InputManager.touchPanelLook ───────► FPSController / Sway
                     └─► MobileInputRig ─► WeaponManager/Weapon callbacks
```

`InputManager` — не абстракция, изолирующая платформенный ввод: это MonoBehaviour-конфигурация KeyCode плюс набор static флагов/векторов. Большая часть consumers продолжает непосредственно читать Unity legacy `Input`.

## 2. `InputManager`

Файл: `Assets/DarkTree FPS/Scripts/Mobile and Inputs/InputManager.cs`.

- Сериализованное поле `_useMobileInput` в `Awake()` копируется в `static useMobileInput`.
- Сериализованные `KeyCode`: `Crouch`, `Run`, `Jump`, `LeanLeft`, `LeanRight`, `Fire`, `Aim`, `Use`, `DropWeapon`, `Reload`, `FiremodeSingle`, `FiremodeAuto`, `Inventory`.
- Static состояние: `joystickInputVector`, `touchPanelLook`, `horizontalFactor`, `verticalFactor`.
- `Update()` безусловно вызывает `MovementDirection()`, который сглаживает legacy `Input.GetAxis("Horizontal"/"Vertical")` к static factors через `Mathf.Lerp(..., deltaTime * 5)`.
- `IsRunning()` — только `Input.GetKey(Run)`. Метод сам не проверяет mobile mode.
- В `Awake()` есть закомментированная ветка, предназначавшаяся для включения/выключения mobile buttons.

## 3. Desktop потребители и команды

| Ввод | Где читается | Эффект |
|---|---|---|
| `Horizontal`/`Vertical` | `FPSController`, `Body`, `CameraBob`, `HeadBob`, `InputManager` | Движение, анимация тела/головы, сглаженные факторы |
| `Mouse X`/`Mouse Y` | `FPSController`; также `Body` использует `GetAxisRaw` для turn animation | Поворот камеры и анимация поворота тела |
| `Jump` axis | `FPSController.Update()` через `GetButtonDown("Jump")` | Прыжок при grounded и stamina > 5 |
| `Run` KeyCode | `FPSController`, `PlayerStats.IsRunning()`, `Body`, `CameraBob`, `HeadBob` | Бег, расход/восстановление stamina, анимации |
| `Crouch`/`Jump` KeyCode | `FPSController.StandaloneMovement()` | Toggle/снятие crouch |
| `LeanLeft`/`LeanRight` | `Weapon/Lean.cs` | Наклон weapon и Animator bools |
| `Fire`, `Aim`, `Reload`, fire mode keys | `Weapon.cs` | Огонь, aim, reload, переключение режима |
| `Inventory` KeyCode, Escape | `InventoryManager.cs` | Открытие/закрытие UI |
| `Use` KeyCode | `UseObjects.cs` | Подбор предмета |

Конкретные KeyCode для перечисленных действий хранятся в сценовом Inspector и не установлены по умолчанию в коде. Их реальные значения нужно снять со сцены/объекта `InputManager` в Editor.

## 4. Mobile движение и обзор

### Joystick

`MobileInput/Joystick.cs` реализует EventSystem drag/pointer callbacks.

- `OnPointerDown()` сразу вызывает `OnDrag()`.
- `OnDrag()` рассчитывает нормализованный/ограниченный `inputVector`, двигает handle и присваивает `InputManager.joystickInputVector`.
- `OnPointerUp()` сбрасывает локальный и общий векторы в zero и возвращает handle в центр.
- `FPSController.MobileMovement()` читает общий вектор для Walk/Run/Crouch; `CharacterMovement()` использует x/y для ground, swimming и climbing движения.
- Mobile run-анимация определяется прямым условием `joystickInputVector.y > 0.5` (не `InputManager.IsRunning()`).

### TouchField

`MobileInput/TouchField.cs` отслеживает pointer delta.

- При активном pointer берёт смещение `Input.touches[pointerId]` (либо mouse position для pointer id вне touch range) относительно предыдущего кадра.
- При отпускании обнуляет `TouchDist`; каждый кадр записывает его в `InputManager.touchPanelLook`.
- `FPSController.MouseLook()` использует этот delta, умноженный на `Time.deltaTime`, для mobile look.
- `Sway` также читает `touchPanelLook` и рассчитывает sway.

### MobileInputRig и UI callbacks

`MobileInput/MobileInputRig.cs` получает `WeaponManager` в `Start()`.

- `SetFire(bool)` выставляет флаг; пока он true, каждый `Update()` вызывает `Fire()` и затем `manager.activeWeapon.Fire()` — это непрерывный вызов нажатой UI-кнопкой стрельбы.
- `Aim()` вызывает `activeWeapon.AimMobile()` при ненулевом active weapon; callback переключает aim toggle.
- `Reload()` вызывает `activeWeapon.ReloadBegin()` при ненулевом active weapon.
- Другие публичные UI hooks находятся не в `MobileInputRig`: `FPSController.MobileMovement()`/`CrouchMobile()`, `WeaponManager.MobileSlotInput(index)`, `Weapon.FireMobile()`.
- Конкретные OnClick/PointerDown/PointerUp assignments должны проверяться в Canvas/prefab/scene; они не подтверждены только исходниками.

## 5. Другие mobile consumers

- `InventoryManager` открывает/закрывает inventory на desktop через KeyCode; для mobile меняет `mobileLookPanel`, предоставляет `MobileToggle()` для UI.
- `UseObjects` в mobile включает `UseButton` при item/lootbox и программно назначает callback pickup/inspect; требует в сцене объект с точным именем `UseButton`.
- `Body.Start()` отключает объект тела в mobile mode.
- `FPSController`, `Weapon`, `Sway`, `PlayerStats`, `UseObjects`, `InventoryManager` ветвят поведение по static `useMobileInput`.
- `NPC` получает `InputManager` в инициализации и использует `inputManager.IsRunning()` в `FollowToTargetBehavior()` для изменения скорости преследования Player. Поскольку `IsRunning()` читает KeyCode Run, поведение NPC также зависит от desktop-клавиши бега.

## 6. Статические наблюдения и вопросы для проверки

Это не объявленные runtime-дефекты — только проверяемые по исходнику особенности.

1. **Смешанный mobile/legacy input:** `FPSController.MobileMovement()` всё ещё проверяет `Input.GetKeyDown(inputManager.Crouch)` и `Jump`; `FPSController.Update()` также проверяет `Input.GetButtonDown("Jump")` в общей логике прыжка. Нужно подтвердить, как на целевой платформе вызываются эти команды и не дублируются ли они через UI.
2. **Бег и stamina имеют разные источники:** движение mobile включает run-анимацию/скорость по joystick forward threshold, но `PlayerStats.ConsumableManager()` вызывает `InputManager.IsRunning()`, который проверяет клавишу Run через legacy input. На mobile run-анимация/ускорение могут не совпасть с расходом stamina; подтвердить в Play Mode.
3. **Часть camera animation helpers читают desktop axes напрямую:** `CameraBob`, `HeadBob`, `Body` используют `Input.GetAxis`/`GetAxisRaw`; `Body` отключается в mobile, но поведение CameraBob/HeadBob на mobile зависит от состава сцены.
4. **Инициализация общего static режима:** `useMobileInput` присваивается `InputManager.Awake()`. Результат зависит от наличия и порядка активации корректного InputManager в сцене. Статические векторы также не имеют отдельного общего reset при уничтожении/смене сцены; joystick/touch сбрасывают их только при своих pointer events/update.
5. **MobileInputRig.Fire() не проверяет activeWeapon на null**, тогда как `Aim()` и `Reload()` проверяют. Нужен тест на UI fire в момент отсутствия экипированного оружия.
6. **Lean.cs не ветвится по mobile mode** и всегда читает KeyCode-клавиши. Требуется проверить, доступен ли lean на мобильной платформе/нужен ли он там.
7. `InputManager.MovementDirection()` безусловно читает legacy axes и выполняет расчёт также в mobile mode; Body desktop отключается, но static factors могут читаться другими компонентами.
8. `Joystick.Start()` использует `new Camera()` при переводе background position в screen point. Это надо сверить в целевом UI/Canvas setup, не менять без воспроизведения.
9. `TouchField` записывает pointer delta в общий static state каждый Update. При нескольких touch fields последний вызвавший Update может перезаписать значение; число/порядок полей в UI не проверены.
10. В `UseObjects` часть мобильных путей обращается к `useCursor` без проверки null (например, предметный hit); инспекторная комплектация UI должна проверяться вместе со сценой.

## 7. Проверочный сценарий этапа 1

Unity Editor/устройство пока недоступны для подтверждённого runtime-прогона. После открытия Demo Zone выполнить отдельно desktop и mobile конфигурации.

### Desktop

1. Проверить в Inspector значение `_useMobileInput == false` и значения всех KeyCode.
2. Запустить сцену; проверить WASD/стрелки, мышь, Space, Run, Crouch, Lean.
3. Проверить Fire/Aim/Reload/fire-mode с экипированным оружием и inventory закрытым.
4. Проверить Inventory toggle/Escape и Use/pickup; убедиться, что ввод игрового оружия блокируется/возобновляется ожидаемо при открытом UI.

### Mobile

1. Проверить `_useMobileInput == true`, активность joystick/touch panel/buttons и их EventSystem bindings.
2. Нажать/отпустить joystick: игрок начинает двигаться и останавливается; повторить на земле, в воде и на ladder/climbing, если сцена содержит эти состояния.
3. Провести пальцем по touch panel: камера вращается; отпустить pointer — delta прекращается, sway возвращается в покой.
4. Удержать mobile fire, проверить непрерывный огонь; отпустить — огонь прекращается. Повторить без active weapon.
5. Проверить aim toggle, reload callback, crouch, mobile inventory toggle, slot selection и UseButton.
6. Сверить ускорение и stamina drain при движении joystick вперёд.

Для каждого шага записать активный объект, значения input vectors до/после pointer release, активное оружие и Console errors. До такого прогона отклонения выше остаются гипотезами/рисками статического анализа.

## 8. Граница следующего изменения

На этом шаге код не менялся. Перед исправлением любого найденного расхождения сначала воспроизвести его в Unity и выбрать один контракт (например mobile run ↔ stamina) как отдельную задачу. Обязательные соседи для проверки: `FPSController`, `PlayerStats`, Animator, mobile Canvas callbacks, `WeaponManager` и Inventory UI.

## Источники

- `Assets/DarkTree FPS/Scripts/Mobile and Inputs/InputManager.cs`
- `Assets/DarkTree FPS/Scripts/Mobile and Inputs/Joystick.cs`
- `Assets/DarkTree FPS/Scripts/Mobile and Inputs/TouchField.cs`
- `Assets/DarkTree FPS/Scripts/Mobile and Inputs/MobileInputRig.cs`
- `Assets/DarkTree FPS/Scripts/Player/FPSController.cs`
- `Assets/DarkTree FPS/Scripts/Player/PlayerStats.cs`
- `Assets/DarkTree FPS/Scripts/Player/Body.cs`, `CameraBob.cs`, `HeadBob.cs`, `Sway.cs`, `UseObjects.cs`
- `Assets/DarkTree FPS/Scripts/Weapon/Weapon.cs`, `WeaponManager.cs`, `Lean.cs`
- `Assets/DarkTree FPS/Scripts/DT Inventory/Inventory/InventoryManager.cs`
- `Assets/DarkTree FPS/Scripts/NPC/NPC.cs`
- `ProjectSettings/InputManager.asset`
