# Журнал сообщений Unity Console

Журнал фиксирует сообщения, переданные из Unity Console, независимо от статуса анализа или исправления кода. Записи только документируют сообщение и контекст, предоставленный пользователем; они не означают, что проблема воспроизведена или исправлена.

## Записи

### Протокол фактической трассировки событий игровых механик

- Для runtime-проверок сохранять все Unity Console log types, timestamps и stack traces в `Temp/UnityTestLogs/unity-console.log` через `Application.logMessageReceived`; подробные требования и формат события находятся в [`docs/UNITY_EVENT_TRACE.md`](./UNITY_EVENT_TRACE.md).
- Помимо общего захвата, instrumentировать конкретные триггеры механики: действие/условие, объект, результат и релевантное состояние до/после. Capture не может показать событие, которое само ничего не логирует.
- После Play Mode читать файл напрямую; Console screenshots/live наблюдение — вспомогательные, но не единственное свидетельство.

### 2026-11-18 22:12 (+0300) — радиационный урон, runtime trace

- **Источник:** `Temp/UnityTestLogs/unity-console.log`, созданный runtime capture через `Application.logMessageReceived`; даты/времена ниже взяты непосредственно из файла.
- **Тест алгоритма:** `TEST START` в `22:11:46.024`, `PASS` в `22:11:46.026`, `TEST END` в `22:11:46.028`. Проверены накопление двух `0.5` в 1 HP, целая часть `2.5` с сохранением остатка, а также игнорирование нулевого и отрицательного урона.
- **Триггеры в сцене:**
  - `22:12:18.694` `RadZone (2)`: 1 HP, `80 -> 79`.
  - `22:12:22.713` `RadZone (2)`: 1 HP, `79 -> 78`.
  - `22:12:26.725` `RadZone (2)`: 1 HP, `78 -> 77`.
  - `22:12:32.751` `RadZone (3)`: 2 HP, `77 -> 75`.
  - `22:12:34.765` `RadZone (3)`: 2 HP, `75 -> 73`.
  - `22:12:36.774` `RadZone (3)`: 1 HP, `73 -> 72`.
- **Результат:** runtime-файл подтверждает, что тест завершился PASS и указанные зоны фактически уменьшали HP. В этом capture не найдено `MissingReferenceException` или Error-level записей.
- **Отдельное предупреждение:** в `22:12:45.290` Unity выдала `Game object with animator is inactive`; stack trace указывает на `DarkTreeFPS.Weapon.OnDisable()` в `Weapon.cs:1149` во время деактивации объектов при выходе из Play Mode. Это не radiation warning; исправление не выполнялось.
- **Ограничение:** записи подтверждают урон и HP transitions, но не калибровку звуковых расстояний, ожидаемую частоту тиков или корректность границ/переходов trigger volume.

### 2026-11-17 01:52:14 RTZ (+0300)

- **Тип:** Warning / diagnostic (по тексту Unity stack trace; точный счетчик Console не передан).
- **Сообщение:** `Look rotation viewing vector is zero`.
- **Источник по stack trace:** `DarkTreeFPS.NPC.Update()` — `Assets/DarkTree FPS/Scripts/NPC/NPC.cs:249`.
- **Контекст:** stack trace передан пользователем в чате. Локальное воспроизведение в Unity Editor не выполнялось.
- **Связанный разбор:** в `NPC.Update()` направление до `desiredPosition`/`curretTarget` проецируется на горизонтальную плоскость; оно может стать нулевым. Проверка в коде рассматривается отдельно от факта регистрации сообщения.

### 2026-11-17 02:01:22 RTZ (+0300)

- **Тип:** Warning.
- **Сообщение:** `Setting linear velocity of a kinematic body is not supported.`
- **Количество на изображении:** повторяется несколько раз (видно около десяти строк; точное число не гарантируется).
- **Источник:** не установлен — на приложенном изображении stack trace предупреждения не раскрыт.
- **Контекст:** время `[02:01:22]` взято из Unity Console на скриншоте; дату и часовой пояс записи — из времени получения/анализа изображения в этой сессии. Локальное воспроизведение не выполнялось.
- **Связанный анализ:** первоначально по одному тексту предупреждения источник не был установлен. Позже пользователь указал, что предупреждения выдаёт zombie. В `ZombieNPC.Start()` все Rigidbody в иерархии переводились в kinematic, а `OnAnimatorMove()` затем записывал velocity корневого Rigidbody. Внесена кодовая правка в `ZombieNPC.cs`, оставляющая корневой Rigidbody dynamic; предупреждение после правки ещё не проверено в Play Mode.

### 2026-11-17 02:05:28 RTZ (+0300)

- **Тип:** Warning — повторное наблюдение.
- **Сообщение:** `Setting linear velocity of a kinematic body is not supported.`
- **Время в Console:** `[02:04:40]`.
- **Количество на изображении:** предупреждение повторяется около десяти раз; точный счёт не фиксируется.
- **Источник:** не установлен. На приложенном скриншоте сообщение не раскрыто до managed stack trace; видна только Unity native строка `StackWalker::GetCurrentCallstack`.
- **Контекст:** скриншот повторно передан пользователем; это не подтверждает исправление или новый источник ошибки.

### 2026-11-17 21:26:30 RTZ (+0300)

- **Источник записи:** скриншот Unity Console, переданный пользователем.
- **Сообщения (время по Console):**
  - `[21:22:12]` `Couldn't create a Convex Mesh from source mesh "Rock_02_LOD0 " within the maximum polygons limit (256). The partial hull will be used. Consider simplifying your mesh.`
  - `[21:22:12]` такое же предупреждение для `Rock_04_LOD0`.
  - `[21:22:12]` такое же предупреждение для `Rock_03_LOD0`.
  - `[21:22:13]` `Asset "Weapon holder : Transition Landing -> Idle" in state "Landing" doesn't have an Exit Time or any condition, transition will be ignored`.
  - `[21:22:14]` `Unity is running with Administrator privileges, which is not supported...` (остальная часть длинного предупреждения обрезана шириной Console/скриншота).
- **Проверка assets:** в `Assets/DarkTree FPS/Content/DemoScene/Graphics/Rock Package/Prefabs/Rock_02.prefab`, `Rock_03.prefab` и `Rock_04.prefab` на соответствующих LOD0 GameObject обнаружен `MeshCollider` с `m_Convex: 1`, использующий LOD0 mesh. Это подтверждает конфигурацию, связанную с тремя предупреждениями; runtime source call stack для них не предоставлен.
- **Статус:** журналирование не означает, что эти сообщения исправлены. Для предупреждения о convex hull решение требует выбрать тип коллайдера/отдельную упрощённую collision mesh с учётом того, статичен ли объект и должен ли он быть Rigidbody.

### 2026-11-17 21:42:05 RTZ (+0300) — проверка Zombie kinematic warning

- **Результат по сообщению пользователя:** после изменения `ZombieNPC.cs` предупреждение о velocity у kinematic Rigidbody исчезло; движение zombie и ragdoll при смерти работают корректно.
- **Статус:** пользователь подтвердил успешную Unity runtime-проверку.

### 2026-11-17 21:35:20 RTZ (+0300)

- **Тип:** Unity Editor diagnostic (иконка/счетчик не позволяют надёжно классифицировать по одному скриншоту).
- **Время по Console:** `[21:33:40]`.
- **Сообщение:** `Scene persistence wasn't loaded due to condition mismatch` (на скриншоте видно два одинаковых сообщения).
- **Источник:** managed stack trace не раскрыт; конкретный вызов/объект не установлен.
- **Статическая проверка проекта:** найдено использование `DontDestroyOnLoad` в `Assets/DarkTree FPS/Scripts/DT Inventory/Level Transition/DontDestroyInstance.cs`; также есть scene reload/load calls в `SaveData.cs`, `LevelTransition.cs`, `GameManagerDemo.cs` и `MainMenu.cs`. Явные вызовы `SceneManager.SetActiveScene` или `MoveGameObjectToScene` в проверенной папке скриптов не найдены.
- **Ограничение:** совпадение терминов не доказывает, что это сообщение вызвано конкретно `DontDestroyInstance` или обычным `SaveData`. Точное условие Unity persistence и источник требуют версии Unity, полного контекста Console/Editor и воспроизведения.
