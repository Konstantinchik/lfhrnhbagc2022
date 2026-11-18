# Этап 0 — базовое состояние и проверочный сценарий

**Назначение:** зафиксировать исходные сведения и повторяемый smoke-test перед анализом/изменением runtime-систем.

**Дата проверки:** 2026-11-17

**Тип проверки:** статический анализ репозитория; Unity Editor/Play Mode не подтверждены.

## 1. Исходное состояние репозитория

- Unity project version: **2022.3.15f1**, см. `ProjectSettings/ProjectVersion.txt`.
- Последний зафиксированный на момент анализа коммит: `063a400 Create PROJECT_MODULARIZATION_PLAN.md`.
- Рабочая копия содержит незакоммиченный `docs/PROJECT_MODULARIZATION_PLAN.md`; этот анализ его не меняет.
- Игровой код в рамках этапа не изменён.

## 2. Сцена и игрок

- В `ProjectSettings/EditorBuildSettings.asset` единственная включённая сцена: `Assets/DarkTree FPS/Demo Zone.unity`.
- `Assets/DarkTree FPS/Weapons.unity` существует, но в Build Settings не включена.
- `Demo Zone.unity` в этой рабочей копии является бинарным файлом (содержит NUL bytes), а не читаемым YAML. Поэтому статическим поиском в файле нельзя надёжно установить GameObject игрока, его компоненты, Inspector references и runtime-активность.
- По исходникам ожидаемый игрок использует `FPSController`, `PlayerStats`, `CharacterController`, `Camera Holder` и `Weapon holder`. `FPSController.OnEnable()` ищет `Weapon holder`, main camera, `PlayerStats`, `WeaponManager`, `InputManager`; `PlayerStats.Start()` ищет `Camera Holder`, `WeaponManager`, `Sway`, `InputManager` и (для desktop mode) `Body`. Это требования к проверке сцены, а не доказательство, что все ссылки настроены в ней.
- Фактический экземпляр игрока и активная hierarchy требуют подтверждения в Unity Editor при открытии Demo Zone.

## 3. Доступность проверки Unity

- Unity Editor-компиляция и Play Mode в этой проверке **не запускались**.
- Попытка вызвать проектный `connect-to-unity.ps1` была заблокирована средой как потенциальное внешнее действие. Скрипт повторно не запускался и обходные способы не применялись.
- В окружении доступен `dotnet`, но из этого не следует, что проект успешно компилируется: Unity C# проект требует Unity Editor assemblies, настройки импорта и editor-generated project state.
- Следовательно, статус компиляции — **неизвестен**, а не «успешен».

## 4. Статически подтверждённые input-контракты

### Desktop

- `InputManager.Awake()` присваивает static `InputManager.useMobileInput` из сериализованного `_useMobileInput`.
- `FPSController` выбирает desktop/mobile ветвь по этому static флагу. Desktop mouse look использует `Mouse X`/`Mouse Y`; движение использует legacy axes.
- `FPSController` обрабатывает прыжок через `Input.GetButtonDown("Jump")`; `ProjectSettings/InputManager.asset` связывает Jump с Space.
- Там же оси `Horizontal`/`Vertical` настроены на стрелки и WASD. Mouse X/Y присутствуют.
- `InputManager` содержит сериализованные KeyCode поля Run, Crouch, Jump, Fire, Aim, Reload, Inventory и др. Реальные значения KeyCode берутся из scene Inspector и в данном анализе не подтверждены.

### Mobile

- `InputManager` хранит static `joystickInputVector` и `touchPanelLook`; `FPSController` читает их в mobile ветвях.
- В этом этапе назначение UI callbacks, наличие мобильного rig в Demo Zone и работа на устройстве не проверены.

### Ограничение Input System

- Рассмотренные компоненты используют legacy `UnityEngine.Input`; проектная конфигурация/совместимость Input System Package отдельно не проверялась.

## 5. Повторяемый Play Mode smoke-test

Открыть `Demo Zone.unity` в Unity 2022.3.15f1. Перед запуском проверить Console на compile/import errors и убедиться, что активный Player содержит ожидаемые компоненты/references. Тестировать на копии сцены или не сохранять случайные runtime/editor изменения.

| Шаг | Действие | Ожидаемый результат | Что записать при отклонении |
|---|---|---|---|
| 1. Старт сцены | Нажать Play | Сцена запускается; Console не получает новых исключений и ошибок Missing Reference от Player/Weapon | Точный первый exception, объект и stack trace |
| 2. Захват камеры | Нажать в Game view; переместить мышь | Если desktop mode включён, камера поворачивается; поворот ограничен вертикально | Значение `_useMobileInput`, cursor state, ошибки и наличие camera references |
| 3. Ходьба | Нажать WASD и стрелки по очереди | Игрок перемещается по горизонтали согласно доступной поверхности | CharacterController/Animator состояние, axes, блокирующие UI/состояния |
| 4. Прыжок | На земле нажать Space, затем попробовать в воздухе | На земле прыжок при доступной stamina; повторный прыжок в воздухе не запускается | Grounded/stamina/health, лог исключений |
| 5. Бег/приседание | Удержать назначенную в InputManager клавишу Run; проверить Crouch | При корректных Inspector KeyCode — меняются скорость/stamina и поза; конкретные клавиши до Inspector-проверки неизвестны | Значения KeyCode, stamina, Animator параметры |
| 6. Экипировка и переключение | Открыть inventory/equipment UI, экипировать доступное оружие, переключить слот | Оружие показывается, HUD/слот соответствует выбранному item | `Item.title`, `weaponName`, slot, WeaponManager и Animator errors |
| 7. Стрельба | На безопасной цели нажать/удержать назначенную Fire-клавишу | При наличии ammo выстрел расходует один item stack за выстрел, воспроизводит shot FX/audio и hit path | Активный item stack, fire mode/type, projectile/prefab refs, target tag/components |
| 8. Прицеливание | Нажать/удерживать назначенную Aim-клавишу, если поддерживается | Состояние прицела меняется; при scope-enabled weapon после задержки включается scope UI/FOV transition | `canUseScope`, Animator, camera refs, wall obstruction |
| 9. Перезарядка | Разрядить часть магазина и нажать назначенную Reload-клавишу | При наличии ammo начинается reload, после завершения патроны переносятся в магазин | ammoItemID, резервные stack, animation timing/events, reload flags |
| 10. Смерть/возрождение | Только в изолированном тесте безопасно нанести урон до смерти и выполнить предусмотренный игрой respawn | Death/respawn не оставляет управление/оружие/камеру в неконсистентном состоянии | `PlayerStats`, `WeaponManager`, cursor, camera/player transforms, save/load effects |

### Минимальный результат первого запуска

Для первого базового прогона достаточно шагов 1–5. Шаги 6–10 — интеграционная часть и выполняются только после подтверждения сцены, валидных item/weapon setup и безопасного способа respawn. Не менять игровой код во время измерения исходного поведения.

## 6. Что считать baseline доказанным

До фактического прогона в Unity доказанными являются только статические факты из файлов: версия проекта, Build Settings, кодовые input call sites и конфигурация legacy axes. Следующие наблюдения пока **не подтверждены**:

- чистая Unity-компиляция и отсутствие ошибок импорта;
- корректность references Player/Weapon/HUD в Demo Zone;
- реальное управление, камера, прыжок, бег и приседание;
- экипировка, стрельба, reload, анимационные события;
- смерть и respawn.

После доступа к Unity Editor заполнить дату/версию редактора, список ошибок до теста, фактический Player hierarchy, значения input fields, результаты каждого шага и точные условия воспроизведения обнаруженных отклонений.

## 7. Трассировка runtime-триггеров игровых механик

Для фактической проверки gameplay-сценариев Console захватывается в файл, потому что live-наблюдение по скриншотам не гарантирует, что будут замечены все сообщения. Общий протокол: [`docs/UNITY_EVENT_TRACE.md`](./UNITY_EVENT_TRACE.md).

- `Dummy` в `Demo Zone` — постоянный разрешённый тестовый объект проекта; он должен оставаться в сцене и может сохраняться/коммититься вместе с намеренно добавленными тестовыми компонентами из `Assets/Tests/`. Компоненты должны быть безвредны в player build или Editor-only.
- `Application.logMessageReceived` записывает все уровни Console, timestamps и stack traces в `Temp/UnityTestLogs/unity-console.log`; там же должны быть явные `TEST START`, `PASS`/`FAIL` и `TEST END`.
- Для конкретной механики отдельно логируется trigger/outcome с полезным контекстом и состоянием до/после. Общий Console capture сам по себе не доказывает срабатывание события, если игровой код его не логирует.
- После сообщения о завершённом прогоне агент читает файл напрямую, классифицирует ошибки/warnings и сопоставляет записи сценария по времени. Отсутствующий файл, маркер или event trace означает «не подтверждено».
- Не логировать каждый кадр; использовать переходы/результаты событий и избегать побочных эффектов harness. Не удалять и не перепрофилировать `Dummy` без прямого указания пользователя.

Подтверждённый radiation-прогон записан в [`docs/UNITY_EVENT_TRACE.md`](./UNITY_EVENT_TRACE.md) и [`docs/UNITY_CONSOLE_LOG.md`](./UNITY_CONSOLE_LOG.md). Для аномальной зоны подготовлен event-driven harness; runtime подтверждение будет только после `[Anomaly test] PASS` в новом capture.

## Источники

- `ProjectSettings/ProjectVersion.txt`
- `ProjectSettings/EditorBuildSettings.asset`
- `ProjectSettings/InputManager.asset`
- `Assets/DarkTree FPS/Scripts/Player/FPSController.cs`
- `Assets/DarkTree FPS/Scripts/Player/PlayerStats.cs`
- `Assets/DarkTree FPS/Scripts/Mobile and Inputs/InputManager.cs`
- `docs/PROJECT_MODULARIZATION_PLAN.md`
