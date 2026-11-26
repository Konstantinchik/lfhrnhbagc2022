# S09 Звук, S10 UI/HUD/меню, S13 Меню/сцены и demo flow — разбор

**Статус: статический разбор; Play Mode не проверялся. Код не изменялся.**

Общий контекст: [PROJECT_SYSTEMS_MAP.md](./PROJECT_SYSTEMS_MAP.md) (строки S09, S10, S13). Не дублируется: HUD здоровья/потребностей — [PLAYER_STATS_ANALYSIS.md](./PLAYER_STATS_ANALYSIS.md), раздел 4; ввод — [INPUT_SYSTEM.md](./INPUT_SYSTEM.md); оружие и его HUD-вызовы — [WEAPON_SYSTEM.md](./WEAPON_SYSTEM.md).
Все пути скриптов — относительно `Assets/DarkTree FPS/`.

**Как проверялось использование.** GUID каждого скрипта (из `.cs.meta`) искался в `Assets/**/*.prefab`, `*.asset` и в текстовой сцене `Weapons.unity`. Сцена `Demo Zone.unity` сохранена в **бинарном** формате (55 МБ) — в ней искалась двоичная запись GUID. Такой поиск отвечает только «да/нет: ассет упомянут в сцене»; имена объектов, количество и значения полей из бинарной сцены не извлекались.

Термины: **HUD** — постоянные элементы интерфейса поверх игры (здоровье, патроны, прицел). **Canvas** — корневой объект Unity UI. **AudioSource** — компонент-«динамик», **AudioClip** — звуковой файл, **AudioMixer** — ассет-микшер с общими каналами громкости. **Build Settings** — список сцен, попадающих в сборку; номер в списке — build index. **Статическое поле** — одно значение на весь класс, доступное без ссылки на объект.

---

## Главное в трёх строках

1. **Меню в проекте нет.** В сборку входит одна сцена — `Demo Zone`; игра стартует сразу в ней. `MainMenu`, `ResolutionChanger`, `GameManagerDemo`, `LevelTransition`, `Tooltip`, `TooltipPointer` не висят ни на одном объекте.
2. **Единой громкости и AudioMixer нет.** Каждый звук играет через свой `AudioSource` напрямую.
3. Весь действующий UI и `SoundManager` живут внутри одного префаба `Prefabs/GamePrefab.prefab`.

---

# Часть A. S13 — сцены и demo flow

## A1. Какие сцены есть и какие в билде

| Сцена | Формат | В Build Settings | Что внутри (подтверждено) |
|---|---|---|---|
| `Assets/DarkTree FPS/Demo Zone.unity` | бинарный | **да, index 0, единственная** (`ProjectSettings/EditorBuildSettings.asset:6-9`) | Упоминает префабы `GamePrefab`, `SaveManagerInstance`, `Anomaly`, `RadZone`, `AssaulterForest`, `Zombie`, `Cover`, `NPC&CoverList`, `weapon_box Variant`; напрямую скрипты `HeliRotor`, `HeliTrigger`, `WaterVolumeTrigger`, `WaterVolumePostProcessing`, `DisableRendererOnStart`, `LevelPoint`, `LootBox` и три тестовых (`Assets/Tests`) |
| `Assets/DarkTree FPS/Weapons.unity` | текст | нет | Тестовый «тир»: экземпляр `GamePrefab` (`Weapons.unity:504`), мишени `Target.prefab`, предметы оружия/патронов, `Plane`, `Cube`, `Directional Light`. Скриптов из этого документа напрямую нет |
| `Assets/DarkTree FPS/Content/DemoScene/Graphics/Rock Package/Scene/Rock map.unity` | текст | нет | Демо графического пакета камней; ни одного `m_Script` |

Других `.unity` в `Assets` нет.

## A2. Как игрок попадает из меню в игру

**Никак — меню отсутствует.** Подтверждения:
- `MainMenu.cs` не используется ни в одном префабе/сцене (GUID `705a2953…` не найден нигде, включая бинарную `Demo Zone`).
- `MainMenu.LoadDemoLevel()` грузит сцену с build index `1` (`MainMenu.cs:12, :16`), а в Build Settings есть только index `0`. Вызов привёл бы к ошибке Unity «scene … couldn't be loaded».
- Вызовов `LoadDemoLevel`/`Quit` из C# нет; в `GamePrefab.prefab` нет UnityEvent-привязок к ним (полный список `m_MethodName` в префабе: `Aim, CrouchMobile, Jump, MobileSlotInput, MobileToggle, Reload, SetFire`).

Фактический поток по коду: запуск → `Demo Zone` (index 0) → экземпляр `GamePrefab` с игроком, HUD и инвентарём.

## A3. Состав

| Файл | Роль | Где висит |
|---|---|---|
| `Scripts/Menu/MainMenu.cs` | `LoadDemoLevel()` — загрузка сцены index 1; `Quit()` — выход (`:14-22`) | **не найдено** |
| `Scripts/GameManagerDemo.cs` | Клавиша `T` перезагружает текущую сцену (`:14-15`) | **не найдено** |
| `Scripts/ResolutionChanger.cs` | Заполняет `Dropdown` разрешениями экрана, `ChangeResolution()` применяет выбранное | **не найдено** |
| `Scripts/DT Inventory/Level Transition/LevelTransition.cs` | Триггер-портал: сохраняет «персистентность» уровня и грузит сцену `sceneId` (`:12-16`) | **не найдено** |
| `…/Level Transition/LevelPoint.cs` | Точка входа на уровень: опционально двигает игрока сюда, сообщает инвентарю «корень сцены», опционально грузит сохранённые предметы уровня | Корень `GamePrefab` (`loadPersistentItemsOnSceneStart: 0`, `movePlayerHereOnSceneEnter: 0`); GUID также упомянут в `Demo Zone` |
| `…/Level Transition/DontDestroyInstance.cs` | `DontDestroyOnLoad(gameObject)` в `Awake` — объект переживает смену сцены (`:5-8`) | Корень `GamePrefab`; корень `Prefabs/SaveManagerInstance.prefab` |
| `…/Level Transition/RegisterPlayerInstance.cs` | Защита от дубля игрока: первый записывает себя в `SaveData.instance`, следующий уничтожается (`:11-19`) | Корень `GamePrefab` |

## A4. Как работает

**Старт сцены.**
1. `GamePrefab.Awake`: `DontDestroyInstance` помечает его неуничтожаемым; `NameFix` принудительно задаёт имя `"GamePrefab"` (`Other/NameFix.cs:9`) — на это имя опирается `HitFXManager` (см. [INTERACTION_AND_EFFECTS.md](./INTERACTION_AND_EFFECTS.md)).
2. `RegisterPlayerInstance.Start`: если `SaveData.instance == null` — регистрирует себя; иначе печатает `"Destroy player duplicate…"` и уничтожает свой объект (`RegisterPlayerInstance.cs:11-19`). Смысл: при повторной загрузке сцены в ней снова лежит `GamePrefab`, а старый уже перенесён через `DontDestroyOnLoad` — дубль удаляется.
3. `LevelPoint.Start` (`LevelPoint.cs:13-31`): запоминает свой transform; при `movePlayerHereOnSceneEnter` ставит объект с тегом `Player` в свою позицию (`:17-18`); пишет `DTInventory.levelPoint = transform` (`:20`); при `loadPersistentItemsOnSceneStart && !SaveData.loadDataTrigger && SaveData.instance != null` вызывает `SaveData.LoadLevelPersistence()` (`:22-26`), иначе печатает `"Scene persistence wasn't loaded due to condition mismatch"` (`:29`).
4. `DTInventory.levelPoint` используется при выбрасывании предмета: выброшенный предмет становится дочерним `levelPoint.parent`, т.е. попадает в иерархию сцены, а не в неуничтожаемый `GamePrefab` (`DTInventory.cs:62-65, :730-732`).

**Сценарий «игрок входит в портал» (заложен в коде, в проекте не собран).** Коллайдер-триггер с `LevelTransition`: любой вошедший коллайдер (проверки тега нет) → `SaveData.SaveLevelPeristence()` → `SceneManager.LoadScene(sceneId)` (`LevelTransition.cs:12-16`). В новой сцене `LevelPoint` переставит игрока и подгрузит сохранённые предметы уровня.

**Перезагрузка сцены.** Реально доступный путь — клавиша загрузки в `SaveData` (`loadKeyCode: 290` = F9 в `SaveManagerInstance.prefab:50`): `SceneManager.LoadScene(текущий buildIndex)` + `loadDataTrigger = true` (`SaveData.cs:53-56`). Клавиша `T` из `GameManagerDemo` не работает, так как компонент нигде не размещён.

## A5. Данные и контракт

- `MainMenu.demoLevelId = 1` — приватное, не сериализуется (`MainMenu.cs:12`).
- `LevelTransition.sceneId` — build index целевой сцены.
- `LevelPoint`: `myTransform`, `loadPersistentItemsOnSceneStart`, `movePlayerHereOnSceneEnter`.
- Опасно менять: порядок сцен в Build Settings (всё грузится по числовому индексу); имена сцен (файлы сохранений называются `<имя сцены>_playerData` и т.п., `SaveData.cs:101-288`); тег `Player`; статические `SaveData.instance`, `SaveData.loadDataTrigger`; метод `SaveLevelPeristence` (с опечаткой — так в коде, `SaveData.cs:70`).

## A6. Наблюдения

**Подтверждено кодом/ассетами**
1. Меню, экран настроек и пауза отсутствуют как размещённые объекты (A2, A3).
2. `MainMenu.LoadDemoLevel()` несовместим с текущими Build Settings (index 1 не существует).
3. `LevelTransition.OnTriggerEnter` не фильтрует, кто вошёл в триггер (`LevelTransition.cs:12`).
4. `LevelPoint` на корне `GamePrefab` имеет оба флага выключенными, т.е. там он только передаёт `levelPoint` инвентарю.
5. `ResolutionChanger.qualitySlider` объявлен, но не используется (`ResolutionChanger.cs:8`). Если текущее разрешение не найдено в списке, `dropdown.value` получит `-1` (`:16, :32`).
6. `Application.Quit()` вызывается только из `MainMenu.Quit()` (`MainMenu.cs:21`) → в игре нет кнопки выхода.

**Гипотезы (проверить в Unity)**
1. В `Demo Zone` скрипт `LevelPoint` может стоять на отдельном объекте сцены с другими флагами — бинарный поиск показывает лишь упоминание GUID.
2. Меню могло существовать в оригинальном ассете как отдельная сцена, не вошедшая в этот репозиторий.
3. При F9 после смерти игрока создаётся новый `GamePrefab` (`SaveData.cs:58-63`) — корректность повторной регистрации в `RegisterPlayerInstance` нужно смотреть в Play Mode.

---

# Часть B. S09 — звук

## B1. Назначение

`SoundManager` — маленький набор «общих» звуков интерфейса и действий (подбор, открытие инвентаря, клик, достать/убрать оружие). `FootstepSound` — звук шагов игрока и NPC в зависимости от поверхности. Остальные звуки (выстрелы, боль, аномалии, счётчик Гейгера) играют собственные `AudioSource` в своих системах и здесь только перечислены.

## B2. Состав

| Файл | Роль | Где висит |
|---|---|---|
| `Scripts/Other/SoundManager.cs` | 4 метода, каждый делает `PlayOneShot` нужного клипа | Корень `GamePrefab` (тот же объект, что `InputManager`, `ActionWait`); на объекте один `AudioSource` |
| `Scripts/Player/FootstepSound.cs` | `PlayFootstep()` — случайный клип шага по тегу поверхности | `GamePrefab/Player/Camera Holder/Lean Controller/WeaponCamera/Weapon holder` (`usedByNPC: 0`); корень `Prefabs/AssaulterForest.prefab` и `Prefabs/Zombie.prefab` (`usedByNPC: 1`) |
| `Scripts/Other/EmptyAnimationEventFix.cs` | Пустой приёмник события `PlayFootstep`, чтобы Unity не ругался на отсутствие метода (`:7-10`) | В `GamePrefab` — на объекте внутри вложенной модели `Content/Soldier/AssaulterForest.prefab` под `Player/Body pivot` (тело игрока) |

## B3. Кто вызывает методы `SoundManager`

| Метод | Клип (поле) | Вызывающий код | Когда |
|---|---|---|---|
| `Pickup()` (`SoundManager.cs:24`) | `pickupSound` | `Player/UseObjects.cs:91` | Игрок нажал клавишу Use, глядя на предмет (только ПК-ветка) |
| `InventoryOpen()` (`:29`) | `inventoryOpenSound` | `DT Inventory/Inventory/InventoryManager.cs:99` | Нажата клавиша инвентаря и он открывается |
| `Click()` (`:34`) | `clickSound` | `DT Inventory/Inventory/InventoryItem.cs:269` | Перетаскиваемый предмет слит в стопку с таким же |
| `WeaponPicking(false)` (`:39`) | `weaponUnhide` | `Weapon/WeaponManager.cs:122` (автоэкипировка), `:309` (`UnhideWeapon`) | Оружие достаётся |
| `WeaponPicking(true)` | `weaponHide` | `Weapon/WeaponManager.cs:297` (`HideWeapon`) | Оружие убирается |

Все четверо получают ссылку через `FindObjectOfType<SoundManager>()` (`UseObjects.cs:45`, `InventoryManager.cs:59`, `InventoryItem.cs:81`, `WeaponManager.cs:51`). Привязок через UnityEvent в `GamePrefab.prefab` нет. Других вызовов в `Assets/**/*.cs` не найдено.

Косвенные вызовы `WeaponPicking`: через `WeaponManager.HideWeapon()` звук «убрать» запускают также `ActionWait` (каждый кадр таймера, но звук играет только пока `activeWeapon != null`, `WeaponManager.cs:293-298`) и `WaterVolumeTrigger` (вход в воду).

## B4. Шаги: как работает `FootstepSound`

`PlayFootstep()` вызывается не из C#, а **Animation Event** — меткой на кадре анимации, которая вызывает метод по имени на объекте с `Animator`. Метка `PlayFootstep` есть в 40 местах: `Animations/Controller animations/Walk.anim` и `Run.anim` (используются в `Animations/Weapon holder.controller` — отсюда шаги игрока) и в клипах NPC (`Content/NPC/Animations/*.fbx.meta`, `Standard Assets/.../HumanoidWalk|Run.fbx.meta`).

- **Игрок** (`usedByNPC == false`): `AudioSource` берётся с того же объекта, `GetCollisionTag` — у родителя (`FootstepSound.cs:32-33`; он висит на `Player`). `GetCollisionTag` каждый кадр лучом вниз на 4 м записывает тег поверхности в `contactTag` (`Player/GetCollisionTag.cs:17-20`). При событии: случайная высота тона 0.8–1.0 и случайный клип из массива по тегу `Dirt`/`Wood`/`Concrete`/`Metal`, иначе — `dirtSteps` (`FootstepSound.cs:47-66`).
- **NPC** (`usedByNPC == true`): `NavMeshAgent` у родителя, `GetCollisionTag` на том же объекте (`:38-39`); громкость = `agent.desiredVelocity.magnitude` (`:71`); играет через `audioSourceNPC`; ветка по умолчанию — `concreteSteps` (`:90`).

## B5. Данные

- `SoundManager` в `GamePrefab`: все пять клипов назначены; `source` указывает на `AudioSource` того же объекта и в `Start` всё равно перезаписывается `GetComponent<AudioSource>()` (`SoundManager.cs:21`).
- `FootstepSound` у игрока (`GamePrefab`): `woodSteps` 4 клипа, `dirtSteps` 4, `concreteSteps` 6, `metalSteps` 4; `audioSourceNPC` пуст. У NPC (`AssaulterForest`, `Zombie`): `woodSteps` 4, `dirtSteps` 6, `concreteSteps` 6, `metalSteps` 4; `audioSourceNPC` назначен.

## B6. Есть ли единая громкость / AudioMixer

**Нет.** Подтверждения:
- файлов `*.mixer` в `Assets` нет;
- у всех 19 `AudioSource` в `GamePrefab.prefab` `OutputAudioMixerGroup: {fileID: 0}` (не подключены к микшеру); то же в `Anomaly.prefab:121` и `RadZone.prefab:97, :224`;
- в коде нет `AudioMixer`, `outputAudioMixerGroup`; `AudioListener.volume` встречается один раз и закомментирован (`DT Inventory/SaveData.cs:457`);
- единственная запись `.volume` — громкость шагов NPC (`FootstepSound.cs:71`);
- глобально `ProjectSettings/AudioManager.asset`: `m_Volume: 1`;
- UI-регулятора громкости нет.

Громкость каждого звука определяется настройками его `AudioSource` и клипа.

## B7. Контракт

- Имена методов `Pickup`, `InventoryOpen`, `Click`, `WeaponPicking(bool)` — вызываются из четырёх систем.
- Имя метода **`PlayFootstep`** — строка внутри 40 анимационных событий; переименование не даст ошибки компиляции, но шаги пропадут, а Unity начнёт писать «AnimationEvent has no receiver». По той же причине нельзя удалять `EmptyAnimationEventFix`.
- Теги поверхностей `Dirt`, `Wood`, `Concrete`, `Metal` (`ProjectSettings/TagManager.asset`).
- `SoundManager` должен быть на объекте с `AudioSource`, и в сцене он ищется как единственный.

## B8. Наблюдения

**Подтверждено кодом**
1. `SoundManager` и `ActionWait` используют **один и тот же** `AudioSource` на корне `GamePrefab` (`SoundManager.cs:21`, `Player/ActionWait.cs:24`) — звуки еды/лечения и UI идут через общий источник.
2. В NPC-ветке по умолчанию индекс для `concreteSteps` выбирается по длине `dirtSteps` (`FootstepSound.cs:90`). Сейчас в обоих NPC-префабах эти массивы по 6 клипов, ошибки нет; при разной длине возможен выход за границы.
3. Методы `SoundManager` не проверяют `source`/клип на `null` (`:24-46`).
4. На мобильной ветке подбора звук `Pickup` не вызывается (`UseObjects.cs:96-103` против `:89-94`).
5. `Click()` звучит только при слиянии стопок, а не при любом клике по UI (`InventoryItem.cs:262-272`).
6. Закрытие инвентаря звука не имеет (`InventoryManager.cs:94-100`).

**Гипотезы**
1. Громкость шагов NPC = длина вектора желаемой скорости; значения выше 1 Unity обрежет до 1, стоящий NPC даст 0 — проверить на слух.
2. Шаги игрока привязаны к анимации `Weapon holder`: без активной анимации ходьбы оружейного держателя (например, со спрятанным оружием) шагов может не быть.
3. Если на сцене окажется два `SoundManager`, разные системы могут получить разные экземпляры.

---

# Часть C. S10 — UI, HUD и подсказки

## C1. Назначение

Набор мелких «презентеров» — скриптов, которые только показывают состояние и не принимают игровых решений: прицел, патроны, здоровье, индикатор обнаружения, сообщения, подсказка по управлению.

## C2. Где лежит UI

Всё внутри `Prefabs/GamePrefab.prefab`:

```text
GamePrefab                    InputManager, SoundManager, ActionWait, DontDestroyInstance, NameFix, RegisterPlayerInstance, LevelPoint
├─ Player                     FPSController, PlayerStats, GetCollisionTag, PlayerStatsPresenter
├─ Mobile Input Canvas        (выключен) Joystick, TouchLook, Fire, Reload, Aim, Crouch, Jump, UseButton, Inventory, Weapon1-3
├─ Inventory Canvas           InventoryManager; Building (панель строительства), Loot window, Equipment panels, Inventory
├─ Canvas Main                HUD: UseCursor, HealthSlider, DamageScreenFX, Reticle, InfoPanel, Messages, Thirst, Hunger,
│                             ActionTimer, VisionDetector, UIScope (выключен), Stamina, Help (выключен)
└─ WeaponManager&HitFXManager WeaponManager, HitFXManager, WeaponHUDPresenter
```

## C3. Состав

| Файл | Роль | Где висит | Кто пишет данные |
|---|---|---|---|
| `Scripts/Weapon/WeaponHUDPresenter.cs` | `Show(name, ammo)` / `Clear()` — текст имени оружия и патронов | `GamePrefab/WeaponManager&HitFXManager`; ссылка лежит в `WeaponManager.weaponHUD` (`WeaponManager.cs:32`, назначена в префабе) | `Weapon.UpdateAmmoText()` → `Show` (`Weapon.cs:1108-1128`); `Clear` (`Weapon.cs:1193-1194`) |
| `Scripts/Weapon/ReticleAnimation.cs` | Четыре штриха прицела расходятся при выстреле и плавно сходятся | `GamePrefab/Canvas Main/Reticle` | `Weapon.cs:451` ставит `ReticleAnimation.shot = true`; сам объект прицела включают/выключают `Weapon.cs:255, :339, :348, :1177-1178` |
| `Scripts/Player/PlayerStatsPresenter.cs` | `Present(health, stamina, hunger, thirst)` — тексты и слайдеры | `GamePrefab/Player` | `PlayerStats.DrawHealthStats()` (`PlayerStats.cs:324-328`); подробно — [PLAYER_STATS_ANALYSIS.md](./PLAYER_STATS_ANALYSIS.md) |
| `Scripts/NPC/VisionUI.cs` | Индикатор «меня замечают»: заполнение круга и прозрачность значка глаза | `GamePrefab/Canvas Main/VisionDetector` | `NPCVision.cs:84-86` поднимает статическое `VisionUI.visionAmount` |
| `Scripts/FadeMessage.cs` | Плавно гасит строку сообщения и очищает её | `GamePrefab/Canvas Main/Messages` (`fadeSpeed: 1`) | **писатели не найдены** (см. C6) |
| `Scripts/HideMessage.cs` | F1 переключает видимость текста-подсказки | `GamePrefab/Canvas Main/Help` — объект **выключен** | — |
| `Scripts/DT Inventory/Tooltip.cs` | Всплывающая подсказка у курсора с названием/описанием предмета | **не найдено** | **вызовов нет** |
| `Scripts/TooltipPointer.cs` | Включает `Tooltip` при наведении на предмет | **не найдено** | — |
| `Scripts/ResolutionChanger.cs` | Выпадающий список разрешений | **не найдено** | — (см. часть A) |

## C4. Как работает (по шагам)

**Игрок стреляет.** `Weapon` ставит `ReticleAnimation.shot = true` (`Weapon.cs:451`) → в ближайшем `Update` штрихи прицела мгновенно ставятся на расстояние `shiftAmount` (200 в префабе) от центра и флаг сбрасывается (`ReticleAnimation.cs:26-34`) → в следующих кадрах `Lerp` возвращает их к `startPosValue` (40) со скоростью `coolSpeed` (5) (`:21-24`). Параллельно `Weapon.UpdateAmmoText()` формирует строку `"{патроны в магазине}|{всего}\n Mode:{1|A}"` и передаёт в `WeaponHUDPresenter.Show` (`Weapon.cs:1124-1128`).

**Игрок прицеливается (ПКМ).** `Weapon` выключает объект `Reticle` (`Weapon.cs:339`), при выходе из прицеливания и не во время перезарядки — включает (`:347-348`).

**NPC замечает игрока.** `NPCVision` копит `detectionTimer`; если он больше текущего `VisionUI.visionAmount` и игрок ещё не обнаружен — записывает его в индикатор (`NPCVision.cs:84-86`). `VisionUI.Update` каждый кадр уменьшает значение на `Time.deltaTime` и выставляет `image.fillAmount` и альфа-канал `eye` как `visionAmount * 0.33` (`VisionUI.cs:15-19`), т.е. индикатор полон при значении около 3 секунд.

**Игрок убирает оружие.** `Weapon` вызывает `weaponHUD.Clear()` — оба текста пустые (`WeaponHUDPresenter.cs:24-31`).

**Здоровье/выносливость/голод/жажда.** `PlayerStats` каждый кадр вызывает `Present(...)`; при отсутствии презентера работает старый путь через поля самого `PlayerStats` (`PlayerStats.cs:324-332`).

**Сообщения (`FadeMessage`).** В `OnEnable` запоминает свой `Text` в статическое `_text` (`FadeMessage.cs:17-20`). Пока текст непустой — уменьшает альфа-канал со скоростью `fadeSpeed`; при альфе ≤ 0 сбрасывает цвет в белый и очищает текст (`:24-35`). Задумка: любой код пишет `FadeMessage._text.text = "..."`, и сообщение само гаснет.

**Подсказка (`HideMessage`).** F1 инвертирует флаг, цвет текста — белый или прозрачный (`HideMessage.cs:17-29`). Текст объекта `Help` — справка по управлению («DT FPS v1.4 … Movement - WASD … Inventory - Tab …», `GamePrefab.prefab:57735`).

## C5. Данные и контракт

- `WeaponHUDPresenter`: приватные сериализованные `ammoText`, `weaponNameText` (оба назначены в префабе).
- `ReticleAnimation`: `top, bottom, left, right` (Image), `startPosValue: 40`, `shiftAmount: 200`, `maxShift: 150`, `coolSpeed: 5`; статическое `shot`.
- `PlayerStatsPresenter`: `source` + 8 ссылок на тексты/слайдеры (все назначены в префабе).
- `VisionUI`: `image`, `eye`; статическое `visionAmount`.
- `FadeMessage`: `fadeSpeed`; статическое `_text`.

Опасно менять:
- статические имена `ReticleAnimation.shot`, `VisionUI.visionAmount`, `FadeMessage._text`, `Tooltip.showTooltip`, `Tooltip.hoveredItem` — это и есть «API» этих скриптов;
- методы `WeaponHUDPresenter.Show/Clear`, `PlayerStatsPresenter.Present`;
- публичное поле `WeaponManager.weaponHUD`;
- **имена объектов** `UseCursor`, `UseButton`, `Camera Holder`, `Weapon holder`, `GamePrefab`, `Player` — их ищут по строке (`UseObjects.cs:33, :51`; `HitFXManager.cs:63`; `DistanceDisable.cs:14`; `NPCVision.cs:54`);
- наличие в сцене ровно одного `ReticleAnimation`: `Weapon.Start` делает `FindObjectOfType<ReticleAnimation>().gameObject` без проверки на `null` (`Weapon.cs:255`);
- UI построен на старом `UnityEngine.UI.Text`, не TextMeshPro.

## C6. Наблюдения

**Подтверждено кодом/ассетами**
1. **В `FadeMessage._text` никто не пишет.** Поиск `FadeMessage` и `_text` по всем `.cs` даёт только сам файл. Строка `Messages` в HUD сейчас не получает сообщений из кода.
2. **`Help` выключен в префабе**, поэтому `HideMessage.Update` не выполняется и F1 ничего не показывает, пока объект не включат.
3. **Система `Tooltip` отключена целиком**: компоненты нигде не размещены; обращения из `InventoryItem` закомментированы (`InventoryItem.cs:384-398`); у `Tooltip.GenerateContent` нет вызовов.
4. **`Tooltip` сам себя выключает**: `rectTransform` — это его же объект (`Tooltip.cs:18`), и при `showTooltip == false` он делает `SetActive(false)` (`:30`), после чего его `Update` перестаёт выполняться и включить себя обратно он не может.
5. `ReticleAnimation.maxShift` объявлен, но не используется (`ReticleAnimation.cs:13`).
6. `VisionUI.visionAmount`, `ReticleAnimation.shot`, `FadeMessage._text` — статические: при перезагрузке сцены значения не сбрасываются автоматически (в коде явного сброса нет).
7. `VisionUI` показывает максимум по всем NPC: каждый пишет своё значение, только если оно больше текущего (`NPCVision.cs:84`).
8. Рядом с подбором существует вторая надпись — `PickupItem.itemNameTooltip` (назначена в префабе; `PickupItem.cs:26-31`); её заполнение для предметов закомментировано (`:65-80`), а имя предмета показывает `UseObjects` через `UseCursor` (`UseObjects.cs:85`).

**Гипотезы**
1. После выстрела в прицеле (ADS) флаг `shot` может остаться взведённым, пока объект `Reticle` выключен, и «выстрелить» анимацией при выходе из прицела — зависит от того, по какой ветке идёт `Weapon.cs:451`.
2. `visionAmount` не ограничен сверху в `VisionUI`; при `timeToDetect` > 3 с заполнение достигнет 100% раньше реального обнаружения.
3. Вне `GamePrefab` в `Demo Zone` есть объект с именем, содержащим `Canvas`, — его содержимое не выяснено (бинарная сцена).

## C7. Не найдено

- Экран паузы, экран смерти/«Game Over», экран настроек, главное меню.
- Локализация текстов.
- Любые записи в `FadeMessage._text`.

---

# Общий тест-план в Unity

Перед каждым шагом — снимок Console по [UNITY_EVENT_TRACE.md](./UNITY_EVENT_TRACE.md).

**Сцены / flow**
1. `File → Build Settings`: убедиться, что в списке только `Demo Zone` (index 0).
2. Play в `Demo Zone`: игрок появляется сразу, без меню. В Hierarchy найти `GamePrefab` в разделе `DontDestroyOnLoad`.
3. Поиск в Hierarchy `t:MainMenu`, `t:LevelTransition`, `t:GameManagerDemo`, `t:ResolutionChanger`, `t:Tooltip` — ожидается 0 объектов. `t:LevelPoint` — записать, сколько и с какими флагами (гипотеза A6.1).
4. F5, затем F9: сцена перезагружается, в Console одно `"Destroy player duplicate…"`, игрок один.
5. Клавиша `T` — ничего не происходит.

**Звук**
6. Подобрать предмет (F) → звук подбора. Tab → звук открытия; закрыть → тишина.
7. Перетащить стекуемый предмет на такой же → звук клика.
8. Сменить оружие 1/2/3 → звуки убрать/достать.
9. Пройти по земле, дереву, бетону, металлу → меняются шаги. Спрятать оружие и пройтись — записать, слышны ли шаги (гипотеза B8.2).
10. Подойти к идущему и стоящему NPC — оценить громкость его шагов.
11. `Window → Audio → Audio Mixer`: убедиться, что микшеров нет.

**HUD**
12. Выстрел от бедра → штрихи прицела расходятся и сходятся. ПКМ → прицел исчезает; выстрел в прицеле, отпустить ПКМ → записать, дёргается ли прицел (гипотеза C6.1).
13. Текст патронов: формат `N|M` и `Mode:1/A`; убрать оружие → тексты пустые.
14. Встать в поле зрения NPC → индикатор глаза заполняется; уйти → убывает.
15. F1 → подсказка не появляется (объект `Help` выключен). Включить `Help` вручную в Play Mode → F1 переключает видимость.
16. Строка `Messages` — за сессию ни разу не заполняется.

# Модульные границы

| Система | Можно менять отдельно | Обязательно проверить соседей |
|---|---|---|
| S09 Звук | Клипы в `SoundManager`/`FootstepSound`, настройки `AudioSource`; добавление новых методов в `SoundManager` | Переименование существующих методов → Inventory, UseObjects, WeaponManager; `PlayFootstep` → все анимационные клипы; ввод AudioMixer → все 19 `AudioSource` в `GamePrefab` + NPC/зоны |
| S10 UI/HUD | Вёрстка и ссылки внутри `Canvas Main`; внутренняя логика презентеров | Статические поля и методы из C5 → Weapon, NPCVision, PlayerStats; имена объектов, которые ищут по строке; `Reticle` должен существовать |
| S13 Сцены | Добавление новой сцены в конец Build Settings; размещение `LevelTransition`/`LevelPoint` в новых сценах | Смена порядка/имён сцен → `SaveData` (имена файлов сохранений, `ClearScenePersistence`); `DontDestroyInstance` + `RegisterPlayerInstance` → дубли игрока; создание меню потребует сдвига индексов и правки `MainMenu.demoLevelId` |

**Следующий практический шаг:** тесты 2–4 (подтвердить flow без меню) и 15–16 (подтвердить неработающие `Help`/`Messages`). Решение «нужны ли меню, пауза, общая громкость» — продуктовое, до него код не менять.

## Источники

`Scripts/Other/SoundManager.cs`, `NameFix.cs`, `EmptyAnimationEventFix.cs`; `Scripts/Player/FootstepSound.cs`, `GetCollisionTag.cs`, `PlayerStatsPresenter.cs`, `PlayerStats.cs:100-112, 322-332`, `UseObjects.cs`, `ActionWait.cs`; `Scripts/Menu/MainMenu.cs`; `Scripts/ResolutionChanger.cs`, `FadeMessage.cs`, `HideMessage.cs`, `TooltipPointer.cs`, `GameManagerDemo.cs`; `Scripts/DT Inventory/Tooltip.cs`, `PickupItem.cs`, `SaveData.cs:12-63, 70, 292-296, 457`, `Level Transition/*.cs`, `Inventory/InventoryManager.cs:52-100`, `Inventory/InventoryItem.cs:81, 262-272, 384-398`, `Inventory/DTInventory.cs:62-65, 730-732`; `Scripts/NPC/VisionUI.cs`, `NPCVision.cs:84-86`; `Scripts/Weapon/WeaponHUDPresenter.cs`, `ReticleAnimation.cs`, `Weapon.cs:251-255, 339-348, 451, 1108-1128, 1177-1194`, `WeaponManager.cs:32, 51, 122, 291-311`; `Prefabs/GamePrefab.prefab`, `SaveManagerInstance.prefab`, `AssaulterForest.prefab`, `Zombie.prefab`; `ProjectSettings/EditorBuildSettings.asset`, `AudioManager.asset`, `TagManager.asset`; сцены `Demo Zone.unity` (бинарный поиск GUID), `Weapons.unity`, `Rock map.unity`.
