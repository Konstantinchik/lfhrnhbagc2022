# S14. Общие gameplay-эффекты и интерактивность — разбор

**Статус: статический разбор; Play Mode не проверялся. Код не изменялся.**

Общий контекст: [PROJECT_SYSTEMS_MAP.md](./PROJECT_SYSTEMS_MAP.md) (строка S14: «нужна классификация по каждому скрипту»). Этот документ и есть такая классификация. Не дублируется: здоровье и урон — [PLAYER_STATS_ANALYSIS.md](./PLAYER_STATS_ANALYSIS.md); плавание — [PLAYER_CONTROLLER_ANALYSIS.md](./PLAYER_CONTROLLER_ANALYSIS.md), раздел 7; попадания оружия — [WEAPON_SYSTEM.md](./WEAPON_SYSTEM.md), разделы 7–8; ввод — [INPUT_SYSTEM.md](./INPUT_SYSTEM.md); `SoundManager` и HUD — [UI_MENU_SCENES_SOUND.md](./UI_MENU_SCENES_SOUND.md).
Пути скриптов — относительно `Assets/DarkTree FPS/`, если не указано иное.

**Как проверялось использование.** GUID скрипта (из `.cs.meta`) искался в `Assets/**/*.prefab`, `*.asset`, в текстовой `Weapons.unity`. `Demo Zone.unity` — **бинарная** (55 МБ): в ней искалась двоичная запись GUID и текстовые имена объектов. Это даёт «упомянут / не упомянут», но не значения полей и не точный объект.

Термины: **триггер** — коллайдер с флажком `Is Trigger`, который не толкает объекты, а вызывает `OnTriggerEnter/Exit`. **Raycast** — невидимый луч, сообщающий, во что он упёрся. **Пул** — заранее созданный набор объектов, которые переиспользуются вместо создания/удаления. **Декаль** — «наклейка» следа от пули. **Animation Event** — метка на кадре анимации, вызывающая метод по имени. **Статическое поле** — одно значение на весь класс.

---

## 1. Назначение

S14 — не одна система, а «остаток»: 20+ мелких скриптов, каждый из которых обслуживает какую-то большую систему. Вместе они дают: подбор предметов взглядом, «занятость» игрока при еде/лечении, опасные зоны (аномалия, радиация), воду, следы и искры от пуль, разрушаемые объекты, декоративный вертолёт и несколько технических заплаток.

## 2. Сводная классификация

| Скрипт | Что делает (одной фразой) | Где висит | Относится к системе |
|---|---|---|---|
| `Scripts/Player/UseObjects.cs` | Луч из камеры: показывает имя предмета и подбирает его по клавише Use | `GamePrefab` → `…/MainCamera - Recoil - HeadBob` (`distance: 2`) | S05 Инвентарь (подбор) + S10 HUD |
| `Scripts/Player/ActionWait.cs` | Таймер «игрок занят»: панель с отсчётом, блок движения, оружие убрано | Корень `GamePrefab` | S02 Игрок/выживание |
| `Scripts/ConsumeEvents.cs` | На предмете: при использовании добавляет здоровье/сытость/воду и запускает `ActionWait` | `Prefabs/Items/Bottle`, `Conserve`, `Medkit`, `Stimpack` | S05 Инвентарь ↔ S02 PlayerStats |
| `Scripts/Anomaly.cs` | Триггер: при входе игрока −20 HP, рывок камеры, звук | `Prefabs/Anomaly.prefab` (в `Demo Zone` ~46 объектов с именем `Anomaly…`) | S02 (урон игроку) |
| `Scripts/Player/RadioactiveZone.cs` | Зона радиации: урон по расстоянию, звук счётчика Гейгера | `Prefabs/RadZone.prefab` (в `Demo Zone` ~22 объекта `RadZone…`) | S02 |
| `Scripts/Player/WaterVolumeTrigger.cs` | Триггер воды: включает плавание, прячет оружие, меняет пост-обработку | `Demo Zone` (в сцене есть объект `WaterVolume`) | S01 Контроллер игрока |
| `Scripts/Player/WaterVolumePostProcessing.cs` | Контейнер из трёх ссылок для предыдущего скрипта | `Demo Zone`, тот же объект (берётся через `GetComponent`) | S01 |
| `Scripts/Weapon/HitFXManager.cs` | Создаёт пулы декалей и частицы попаданий, хранит звуки рикошета | `GamePrefab/WeaponManager&HitFXManager` (`usedByNPC: 0`); корень `Prefabs/AssaulterForest.prefab` (`usedByNPC: 1`) | S03 Оружие, S04 NPC |
| `Scripts/Weapon/ObjectHealth.cs` | «Здоровье» предмета; при `health < 0` уничтожает объект | **не найдено** ни в одном префабе/сцене | S03 Оружие (приёмник урона) |
| `Scripts/HeliRotor.cs` | Вращает объект вокруг своей оси Y | `Demo Zone` (есть объект `Rotor`) | Декор сцены |
| `Scripts/HeliTrigger.cs` | Триггер: включает `Animator` вертолёта при входе игрока | `Demo Zone` (есть объект `HeliTrigger`) | Декор сцены / demo flow |
| `Scripts/DistanceDisable.cs` | Выключает объект, когда игрок дальше порога | `Prefabs/Anomaly.prefab` (`objToDisable` → `Particle System`, 50 м) | Оптимизация |
| `Scripts/DisableRendererOnStart.cs` | Выключает `MeshRenderer` на старте | `Demo Zone` (объект не установлен) | Утилита сцены |
| `Scripts/AddItemsOnStart.cs` | Один раз кладёт список предметов в инвентарь | **не найдено** | S05 Инвентарь (отладка) |
| `Scripts/TestEquipmentCallback.cs` | Печатает `"Callback"` | **не найдено** | Отладка инвентаря |
| `Scripts/Other/SoundManager.cs` | Общие звуки | Корень `GamePrefab` | S09 — см. [UI_MENU_SCENES_SOUND.md](./UI_MENU_SCENES_SOUND.md), часть B |
| `Scripts/Other/NameFix.cs` | В `Awake` задаёт объекту имя `"GamePrefab"` | Корень `GamePrefab` | Заплатка для `HitFXManager` и S07 |
| `Scripts/Other/EmptyAnimationEventFix.cs` | Пустой метод `PlayFootstep` | `GamePrefab` → модель тела игрока под `Player/Body pivot` | Заплатка для S08 Анимации |
| `Assets/Scripts/HelloWorld.cs` | `Debug.Log("Привет")` в `Start` | **не найдено** | Учебный/пробный |
| `Assets/Scripts/LogMessage.cs` | `Debug.Log("Привет всем")` в `Start` | **не найдено** | Учебный/пробный |
| `Assets/Tests/AnomalyZoneDebugTest.cs` | Слушает реальное срабатывание аномалии и пишет PASS/FAIL | `Demo Zone` | Тесты S14/S02 |
| `Assets/Tests/RadioactiveZoneDebugTest.cs` | Проверяет формулу накопления урона радиации | `Demo Zone` | Тесты S02 |
| `Assets/Tests/WeaponAnimatorCompatibilityDebugTest.cs` | Проверяет параметры аниматоров Shotgun/Barrett | `Demo Zone` | Тесты S03/S08 |
| `Assets/Tests/UnityConsoleFileCapture.cs` | Пишет весь Console в файл во время Play Mode | не компонент (статический класс) | Тестовая инфраструктура |

---

## 3. Разбор по скриптам

### 3.1. `UseObjects` — подбор предметов взглядом

**Жизненный цикл.** `Start` (`UseObjects.cs:31-54`): ищет по имени объект `UseCursor`, берёт его дочерний `Text`, выключает курсор; находит инвентарь, `InputManager`, `WeaponManager`, `SoundManager`, `PickupItem`; в мобильном режиме ищет по имени кнопку `UseButton`. `Update` каждый кадр вызывает `Pickup()` (`:58`).

**Сценарий «игрок смотрит на банку и жмёт F».**
1. Луч из камеры вперёд на `distance` метров (`:71`; в префабе 2, в коде по умолчанию 1.5).
2. Коллайдер с тегом `Item` → `useState = true`, курсор включён, текст = `Item.title` (`:73-85`).
3. ПК: нажата `input.Use` (`Use: 102` = F в `GamePrefab`) → `soundManager.Pickup()` → `inventory.AddItem(item)` (`:89-93`).
4. Мобильный режим: на `UseButton` перевешивается обработчик `inventory.AddItem(item)` (`:96-102`).
5. Тег `LootBox`: текст `Inspect`; на мобильном — кнопка открывает ящик через `PickupItem.InspectLootBox` (`:107-121`). На ПК ящик открывает не этот скрипт, а `PickupItem.PickupWithRaycast` (`DT Inventory/PickupItem.cs:82-89`).
6. Иначе/мимо: `useState = false`, курсор и текст очищаются (`:123-150`).

**Связи.** Пишет статическое `UseObjects.useState`, которое читает `Weapon.cs:397` (`animator.SetBool("Use", …)` — анимация «рука тянется»). Вызывает `SoundManager.Pickup`, `DTInventory.AddItem`, `PickupItem.InspectLootBox`. Публичный `Pickup()` извне не вызывается.

**Контракт.** Тег `Item`, тег `LootBox`, имена объектов `UseCursor` и `UseButton`, статическое `useState`, компонент `Item` на подбираемом объекте.

### 3.2. `ActionWait` — «игрок занят»

**Жизненный цикл.** `Start`: берёт `AudioSource` своего объекта и `WeaponManager` (`ActionWait.cs:24-25`). `Update` (`:28-59`): каждый кадр уменьшает `actionTimer`. Пока `actionTimer >= 0`: показывает `ActionPanelUI`, пишет `"<текст> :<секунды>"`, вызывает `weaponManager.HideWeapon()`, ставит `FPSController.canMove = false` и `InventoryManager.showInventory = false`. Когда таймер ушёл в минус: прячет панель и возвращает `canMove = true`.

**Данные.** `ActionTextUI`, `ActionPanelUI` — назначены в `GamePrefab` (вероятно, `Canvas Main/ActionTimer`; имя по fileID не сверялось). `actionTimer`, `actionText`, `audioSource` — публичные, но скрыты от Inspector; их пишет `ConsumeEvents`.

**Связи.** Единственный, кто запускает таймер, — `ConsumeEvents.ActionWait()` (`ConsumeEvents.cs:60-66`). `ActionWait` — единственный, кто пишет `FPSController.canMove` (читают `FPSController.cs:153, :265, :386`, `HeadBob.cs:28`).

### 3.3. `ConsumeEvents` — эффект расходуемого предмета

**Жизненный цикл.** `OnEnable` (`ConsumeEvents.cs:36-58`): находит `ActionWait` и `PlayerStats`, берёт `Item` на своём объекте и подписывает один из трёх методов на `item.onUseEvent` согласно `m_Event`.

**Сценарий «игрок двойным кликом использует аптечку».**
1. `InventoryItem.OnPointerClick`: при двойном клике и наличии `ConsumeEvents` → `inventory.UseItem(this, false)` (`InventoryItem.cs:108-119`). Второй путь — пункт контекстного меню `InventoryInteraction.Useitem()` (`InventoryInteraction.cs:27-31`).
2. `DTInventory.UseItem` вызывает `item.onUseEvent.Invoke()`; нестекуемый предмет удаляется, у стекуемого `stackSize -= 1` (`DTInventory.cs:1226-1246`).
3. Срабатывает подписанный метод, например `AddHealth()` → `playerStats.AddHealth(pointsToAdd)` → `ActionWait()` (`ConsumeEvents.cs:68-72`).
4. `ActionWait()`: задаёт текст, играет `actionWaitSFX` через `AudioSource` из `ActionWait`, и если `actionWaitTimer > 0` — запускает таймер (`:60-66`).
5. Дальше работает `ActionWait.Update` (3.2): N секунд игрок стоит без оружия с надписью «Healing :4…».

**Данные в префабах.**

| Префаб | `Item.title` | `m_Event` | `pointsToAdd` | Таймер | Текст |
|---|---|---|---|---|---|
| `Items/Medkit.prefab` | Medkit | 0 = addHealth | 50 | 4 с | Healing |
| `Items/Stimpack.prefab` | Pevetin | 0 = addHealth | 30 | 1 с | Injecting |
| `Items/Conserve.prefab` | Conserve | 1 = addSatiety | 50 | 3 с | Eating |
| `Items/Bottle.prefab` | Water | 2 = addHydration | 50 | 2 с | Drinking |

**Контракт.** Порядок значений `enum ConsumableEvents { addHealth, addSatiety, addHydration }` (`:18`) — в префабах хранится число; перестановка поменяет эффект предметов. Методы `PlayerStats.AddHealth/AddSatiety/AddHydration`. Наличие `ConsumeEvents` — признак «предмет можно использовать двойным кликом» (`InventoryItem.cs:115, :121`).

### 3.4. `Anomaly` — аномальная зона

`Start`: находит `FPSController`, свой `AudioSource`, `Animator` на `Camera.main` (`Anomaly.cs:21-23`). `OnTriggerEnter` для коллайдера с тегом `Player` (`:27-39`): `headBobAnimator.Play("CameraKick")`, `playerStats.ApplyDamage(20)`, `audioSource.PlayOneShot(anomalyEnterReaction)`. В редакторе дополнительно пишет строку `[Anomaly] TriggerEnter …` и вызывает событие `PlayerTriggerObserved` для теста (`:42-59`).

Префаб `Prefabs/Anomaly.prefab`: `SphereCollider` — триггер радиусом 0.5 (`:86-96`), зацикленный `AudioSource` с автозапуском (`:113-126`), `Anomaly` (клип реакции назначен), `DistanceDisable`, дочерний `Particle System`.

Контракт: тег `Player`; имя состояния аниматора `"CameraKick"` (существует в `Animations/Controller animations/Camera & Recoil.controller`); `PlayerStats.ApplyDamage(int)`; сигнатура события `PlayerTriggerObserved` (на неё подписан тест).

### 3.5. `RadioactiveZone` — радиация

Каждый кадр `LateUpdate` по расстоянию до игрока выбирает режим (`RadioactiveZone.cs:90-112`): ближе `highRadDistance` — включён объект «сильного» треска, `damage = damageHi`; ближе `lowRadDistance` — «слабый», `damage = damageLow`; дальше — оба выключены, `damage = 0`. `Update` раз в `timeToDamage` секунд вызывает `ApplyDamageTick` (`:50-57`), который через `CalculateDamageToApply` накапливает дробный урон и вычитает из `PlayerStats.health` только целую часть (`:60-88`).

Префаб `Prefabs/RadZone.prefab`: `lowRadDistance: 8`, `highRadDistance: 3`, `damageLow: 0.5`, `damageHi: 2`, `timeToDamage: 2`; дочерние `hi` и `low` — зацикленные источники звука.

Контракт: тег `Player`; прямой доступ к полю `PlayerStats.health`; `internal static CalculateDamageToApply(float, ref float)` — вызывается тестом.

### 3.6. `WaterVolumeTrigger` + `WaterVolumePostProcessing` — вода

Вход коллайдера с тегом `Player` (`WaterVolumeTrigger.cs:18-28`): `FPSController.isSwimming = true`; `FPSController.highestTriggerEdge = позиция.y + localScale.y / 2` (верхняя грань объёма); `weaponManager.HideWeapon()`. Вход коллайдера с тегом `MainCamera` (`:29-32`): профиль пост-обработки меняется на `water`. Выход (`:35-47`): `UnhideWeapon()`, `isSwimming = false`, `highestTriggerEdge = -9999`; для камеры — профиль `standard`.

`WaterVolumePostProcessing` — только три публичные ссылки: `postProcessVolume`, `standard`, `water` (`WaterVolumePostProcessing.cs:8-11`). У главной камеры в `GamePrefab` есть `SphereCollider` и `Rigidbody` — поэтому она сама вызывает события триггера.

Контракт: теги `Player` и `MainCamera`; статические `FPSController.isSwimming`, `highestTriggerEdge`; оба компонента на одном объекте; пакет Post Processing v2 (`UnityEngine.Rendering.PostProcessing`).

### 3.7. `HitFXManager` — искры, кровь и следы от пуль

**Жизненный цикл.** `Start` (`HitFXManager.cs:57-81`): вызывает `DecalsPool()`, затем создаёт по одному экземпляру частиц для бетона, дерева, земли, металла и крови (`obj…HitFX`). `DecalsPool()` (`:83-140`): создаёт 4 объекта-контейнера `decalsPool_concrete/wood/dirt/metal` и в каждом `decalsPoolSizeForEachType` выключенных декалей. При `usedByNPC == false` всё созданное становится дочерним объекта, найденного по имени `"GamePrefab"`.

**Кто пользуется.** Сам менеджер ничего не проигрывает — это склад.
- `Weapon` находит менеджер через `FindObjectOfType<HitFXManager>()` (`Weapon.cs:280`, повторно `:695-697`), берёт у него `AudioSource` для рикошетов и массив `ricochetSounds` (`:282-283`), при попадании переставляет нужную систему частиц в точку попадания и включает следующую декаль из пула по кругу (`Weapon.cs:699-821`).
- `NPC` обязан иметь менеджер на своём объекте (`[RequireComponent]`, `NPC.cs:21`), берёт его через `GetComponent` (`:152`) и делает то же самое для своих выстрелов (`NPC.cs:914-1040`).

**Данные.** У игрока: все 5 частиц, `impactSound`, 4 звука рикошета, `decalsPoolSizeForEachType: 20`, 4 префаба декалей. У `AssaulterForest`: частицы и декали назначены, 4 звука рикошета, `impactSound` пуст, пул 20.

**Контракт.** Публичные имена `objConcreteHitFX … objBloodHitFX`, `concreteDecal_pool … metalDecal_pool`, `decalsPoolSizeForEachType`, `ricochetSounds`; имя объекта `GamePrefab` (его гарантирует `NameFix`, см. 3.11); на объекте менеджера игрока должен быть `AudioSource`.

### 3.8. `ObjectHealth` — разрушаемый объект

`Update`: при `health < 0` — если `instantiateAfterDeath`, создаёт `objToInstantiate` на своём месте; затем уничтожает себя (`ObjectHealth.cs:19-28`).

Кто уменьшает `health`: удар ножом (`Weapon.cs:517-518`), попадание пули (`Weapon.cs:588-589`), взрыв гранаты (`Grenade.cs:60-62`), взрыв ракеты (`Rocket.cs:75-77`). У NPC аналогичный код закомментирован (`NPC.cs:441-442`).

Контракт: публичное поле `health` (float) — пишется напрямую четырьмя местами.

### 3.9. `HeliRotor` и `HeliTrigger` — вертолёт

`HeliRotor.Update`: `transform.Rotate(0, Time.deltaTime * speed, 0, Space.Self)`, `speed` по умолчанию 360°/с (`HeliRotor.cs:7-11`). `HeliTrigger.OnTriggerEnter`: при теге `Player` включает `heliAnimator.enabled = true` (`HeliTrigger.cs:7-12`) — т.е. заранее выключенный аниматор начинает проигрывать анимацию (предположительно пролёт вертолёта). Обратного выключения нет.

### 3.10. `DistanceDisable`, `DisableRendererOnStart`, `AddItemsOnStart`, `TestEquipmentCallback`

- **`DistanceDisable`**: `Start` ищет объект по имени `"Player"` (`DistanceDisable.cs:14`). `Update`: если `objToDisable` активен **и** игрок дальше `distanceToDisable` — выключает, **иначе** включает (`:19-26`).
- **`DisableRendererOnStart`**: `GetComponent<MeshRenderer>().enabled = false` (`DisableRendererOnStart.cs:9`). Типичное применение — объект-маркер, видимый в редакторе и невидимый в игре.
- **`AddItemsOnStart`**: в первом `Update` создаёт копию каждого предмета из списка `items` и кладёт в инвентарь (`AddItemsOnStart.cs:13-28`). Стартовый набор для отладки.
- **`TestEquipmentCallback`**: метод `TestCallBack()` печатает строку (`TestEquipmentCallback.cs:7-10`). Задуман как цель UnityEvent панели экипировки; привязок в префабах не найдено.

### 3.11. `Scripts/Other/*`

- **`SoundManager`** — см. [UI_MENU_SCENES_SOUND.md](./UI_MENU_SCENES_SOUND.md), часть B.
- **`NameFix`**: `Awake` → `name = "GamePrefab"` (`NameFix.cs:7-10`). Зачем: `SaveData` при загрузке после смерти создаёт игрока через `Instantiate(gamePrefab)` (`SaveData.cs:62`), и Unity назвала бы объект `GamePrefab(Clone)`; тогда `GameObject.Find("GamePrefab")` в `HitFXManager.cs:63-131` вернул бы `null`.
- **`EmptyAnimationEventFix`**: пустой `PlayFootstep()` (`EmptyAnimationEventFix.cs:7-10`). Модель тела игрока использует анимации с событием `PlayFootstep`; без метода-приёмника Unity пишет ошибку в Console.

### 3.12. Тестовые скрипты

**`Assets/Scripts/`** — `HelloWorld.cs`, `LogMessage.cs`: по одной строке `Debug.Log` в `Start`. Нигде не размещены. К игре отношения не имеют.

**`Assets/Tests/`** (пространство имён `DarkTreeFPS.Tests`; логика — только под `#if UNITY_EDITOR`, в сборке компоненты пустые):

| Файл | Что проверяет | Как запускается |
|---|---|---|
| `AnomalyZoneDebugTest.cs` | Подписывается на `Anomaly.PlayerTriggerObserved` (`:18`). Когда игрок **реально** входит в аномалию: урон ровно 20, есть состояние `CameraKick`, назначен звук → `PASS`/`FAIL` (`:38-68`). Если Play Mode закончился без входа — предупреждение `NO TRIGGER OBSERVED` (`:34-35`) | Компонент на объекте сцены |
| `RadioactiveZoneDebugTest.cs` | В `Start` шесть раз вызывает `RadioactiveZone.CalculateDamageToApply` и проверяет накопление дробей: 0.5+0.5 → 1 HP, 2.5 → 2 HP + остаток 0.5, 0 и −1 игнорируются (`:25-44`) | Компонент на объекте сцены, сам при старте |
| `WeaponAnimatorCompatibilityDebugTest.cs` | Читает контроллеры аниматоров: у Shotgun нет bool `Use`, у Barrett есть `Use` и состояние `Shot`, у `Weapon holder` нет `AimShot` (`:21-34`). Ищет оружие по `weaponName` (`"Shotgun"`, `"Barret"`) и объект `Weapon holder` по имени | Компонент на объекте сцены |
| `UnityConsoleFileCapture.cs` | Статический класс; до загрузки сцены подписывается на все сообщения Console и пишет их в `<проект>/Temp/UnityTestLogs/unity-console.log` (`:21-36`) | Автоматически, `[RuntimeInitializeOnLoadMethod]` |
| `Editor/DarkTreeFPS.EditModeTests.asmdef` | Описание сборки EditMode-тестов | Файлов тестов в папке `Assets/Tests/Editor/` нет |

Три компонента-теста на объект `Dummy` вешает редакторный скрипт `Scripts/Editor/AttachRadioactiveZoneTestToDummy.cs`: при открытии сцены ищет объект с именем `Dummy` и добавляет недостающие компоненты, помечая сцену изменённой, но не сохраняя (`:26-63`). Объект `Dummy` в `Demo Zone` есть; все три тестовых GUID в сцене упомянуты, т.е. сцена была сохранена с ними. Правило проекта: `Dummy` — постоянный тестовый объект, не удалять (см. [PROJECT_SYSTEMS_MAP.md](./PROJECT_SYSTEMS_MAP.md), шапка). Протокол чтения результатов — [UNITY_EVENT_TRACE.md](./UNITY_EVENT_TRACE.md).

---

## 4. Карта связей

```text
Игрок смотрит на предмет ──► UseObjects ──► SoundManager.Pickup
                                  │     └──► DTInventory.AddItem
                                  └── useState ──► Weapon (анимация "Use")

Двойной клик в инвентаре ──► DTInventory.UseItem ──► Item.onUseEvent
        ──► ConsumeEvents ──► PlayerStats.AddHealth/AddSatiety/AddHydration
                          └─► ActionWait ──► WeaponManager.HideWeapon
                                         ├─► FPSController.canMove
                                         └─► InventoryManager.showInventory

Anomaly (триггер) ─────► PlayerStats.ApplyDamage(20) + Animator "CameraKick" + звук
RadioactiveZone ───────► PlayerStats.health (напрямую)
WaterVolumeTrigger ────► FPSController.isSwimming / highestTriggerEdge
                    ├──► WeaponManager.HideWeapon / UnhideWeapon
                    └──► PostProcessVolume.profile

Weapon / NPC ──(попадание)──► HitFXManager (частицы, декали, рикошет)
Weapon / Grenade / Rocket ──► ObjectHealth.health ──► Destroy
HeliTrigger ──► Animator вертолёта;  HeliRotor ──► вращение
```

## 5. Сводный контракт (что опасно переименовывать)

- **Теги:** `Player`, `MainCamera`, `Item`, `LootBox` (`ProjectSettings/TagManager.asset`; `Player` и `MainCamera` — встроенные).
- **Имена объектов, которые ищут строкой:** `GamePrefab`, `Player`, `UseCursor`, `UseButton`, `Dummy`, `Weapon holder`.
- **Имена состояний/событий анимации:** `CameraKick`, `PlayFootstep`.
- **Статические поля:** `UseObjects.useState`, `FPSController.canMove`, `FPSController.isSwimming`, `FPSController.highestTriggerEdge`.
- **Публичные поля, которые пишут снаружи:** `ActionWait.actionTimer/actionText/audioSource`, `ObjectHealth.health`, все `obj…HitFX` и `…_pool` у `HitFXManager`.
- **Значения enum в префабах:** `ConsumeEvents.ConsumableEvents`.
- **Методы:** `PlayerStats.ApplyDamage/AddHealth/AddSatiety/AddHydration`, `WeaponManager.HideWeapon/UnhideWeapon`, `RadioactiveZone.CalculateDamageToApply`, событие `Anomaly.PlayerTriggerObserved`.

## 6. Наблюдения

### 6.1. Подтверждено кодом и ассетами

1. **`DistanceDisable` мигает вдали.** Когда объект уже выключен и игрок далеко, первое условие (`activeInHierarchy && …`) ложно → ветка `else` включает объект; на следующем кадре он снова выключается (`DistanceDisable.cs:19-26`). Итог по коду — объект включён через кадр. Скрипт стоит на каждом `Anomaly`.
2. **`ConsumeEvents` подписывается при каждом `OnEnable` и никогда не отписывается** (`ConsumeEvents.cs:36-57`, `OnDisable` нет). Предмет выключается при помещении в инвентарь (`DTInventory.cs:592`) и включается при выбрасывании (`DTInventory.cs:728`) — каждое включение добавляет ещё одного слушателя.
3. **`ActionWait` убирает оружие, но не возвращает.** `HideWeapon()` обнуляет `activeWeapon` (`WeaponManager.cs:291-298`); по окончании таймера `ActionWait` восстанавливает только `canMove` (`ActionWait.cs:47-58`).
4. **То же при выходе из воды.** `UnhideWeapon()` включает только `weaponToUnhide`, а его перед этим никто не задаёт (`WaterVolumeTrigger.cs:27, :39`; `WeaponManager.cs:301-311`) — проиграются звук и анимация `Unhide`, но оружие в руках не появится.
5. **Звук подбора играет до проверки места.** `soundManager.Pickup()` вызывается раньше `AddItem`, а результат `AddItem` (может быть `false` при заполненном инвентаре, `DTInventory.cs:617-620`) не проверяется (`UseObjects.cs:91-92`).
6. **`UseObjects` без проверок на `null`** для `useCursor`/`useText` в ветках `Item` и `LootBox` (`:78, :85, :112-113`), хотя в ветке «мимо» проверки есть (`:140-149`). Поле `weaponManager` заполняется, но не используется (`:27, :43`).
7. **`Anomaly` считает `cameraKickAvailable`, но не использует его как защиту**: `headBobAnimator.Play("CameraKick")` вызывается безусловно (`Anomaly.cs:33-37`). Урон 20 зашит в код (`:38`) и в тест (`AnomalyZoneDebugTest.cs:12`). Срабатывает только на вход, не за время нахождения внутри.
8. **Радиация: дробный урон теперь накапливается** (`RadioactiveZone.cs:76-88`), а не отбрасывается. Пункт 6.2 в [PLAYER_STATS_ANALYSIS.md](./PLAYER_STATS_ANALYSIS.md) («`(int)damage` может усекаться до нуля») описывает прежнюю версию кода и устарел. При значениях префаба: слабая зона — 1 HP раз в 4 с, сильная — 2 HP раз в 2 с.
9. **Радиация обходит `ApplyDamage`**: пишет `health` напрямую (`RadioactiveZone.cs:70`) — без звука боли и красного экрана (то же отмечено в PLAYER_STATS, раздел 3).
10. **Условие средней зоны избыточно**: `< lowRadDistance || > highRadDistance` в `else if` всегда истинно (`RadioactiveZone.cs:100`); дальняя зона затем перекрывает результат отдельным `if` (`:106-111`). Поведение корректное, запись запутанная.
11. **`HitFXManager.impactSound` никем не читается** (поиск по `.cs`: только объявление, `HitFXManager.cs:30`); звук удара ножа берётся из настроек оружия (`Weapon.cs:208, :514`).
12. **У каждого NPC с `HitFXManager` — собственные 80 декалей и 5 систем частиц** (`HitFXManager.cs:59-79`, пул 20 × 4), создаваемые в корне сцены. Кода их удаления при уничтожении NPC нет.
13. **`Weapon` ищет менеджер через `FindObjectOfType`** без уточнения владельца (`Weapon.cs:280, :697`), а в сцене менеджеры есть и у NPC.
14. **`ObjectHealth` не используется**: ни в одном префабе, ни в `Demo Zone`, ни в `Weapons.unity`. Весь код нанесения урона по нему сейчас «мёртвый». Условие строгое: объект с `health == 0` не разрушится (`ObjectHealth.cs:21`).
15. **`WaterVolumeTrigger` вычисляет верх воды как `position.y + localScale.y / 2`** (`:24`) — верно только для единичного `BoxCollider` без смещения центра.
16. **`HeliTrigger` одноразовый** по смыслу: повторный вход ничего не меняет (`HeliTrigger.cs:11`).
17. **Не размещены нигде:** `ObjectHealth`, `AddItemsOnStart`, `TestEquipmentCallback`, `HelloWorld`, `LogMessage`.
18. **`AddItemsOnStart` не проверяет результат `AddItem`**: при нехватке места созданная копия предмета останется в сцене (`AddItemsOnStart.cs:24-25`).
19. В `Assets/Tests/Editor/` есть только `.asmdef` без тестов; три «теста» — это обычные компоненты сцены в основной сборке `Assembly-CSharp` (иначе они не видели бы `internal`-члены `Anomaly` и `RadioactiveZone`).

### 6.2. Гипотезы, требуют проверки в Unity

1. **Умножение эффекта расходников** (из 6.1.2): подобрать → выбросить → подобрать → использовать аптечку может дать +100 вместо +50 и дважды запустить звук/таймер.
2. **Игрок остаётся без оружия** после еды/лечения и после выхода из воды (из 6.1.3–4), пока не нажмёт клавишу слота.
3. **Частицы аномалий мерцают** на расстоянии > 50 м (из 6.1.1); визуально может быть незаметно, но каждый кадр идёт включение/выключение `Particle System` на ~46 объектах.
4. **Оружие игрока может использовать `HitFXManager` одного из NPC** (из 6.1.13): тогда рикошеты звучат из точки NPC, а после его уничтожения возможны ошибки `MissingReferenceException`.
5. **`ActionWait` и звук «убрать оружие»**: `HideWeapon()` вызывается каждый кадр таймера, но звук играет только при ненулевом `activeWeapon` — ожидается один раз; проверить на слух.
6. **Аномалия без `Animator` на камере** даст `NullReferenceException` до нанесения урона (`Anomaly.cs:37`) — проверить, что у `Camera.main` аниматор есть всегда (в `GamePrefab` — есть).
7. **Тег `MainCamera` и вода**: смена профиля зависит от того, что коллайдер камеры пересечёт триггер отдельно от тела игрока; при быстром выходе профиль может «залипнуть».
8. **Что именно включает `HeliTrigger`** и на каком объекте стоит `DisableRendererOnStart` — из бинарной сцены не выяснено.
9. **Мобильный подбор**: обработчик кнопки перевешивается каждый кадр, пока игрок смотрит на предмет (`UseObjects.cs:99-100`), звука подбора нет.

### 6.3. Не найдено

- Объекты с `ObjectHealth` (разрушаемые ящики, бочки и т.п.).
- Вызовы `TestEquipmentCallback.TestCallBack` из кода или UnityEvent.
- Обработка «предмет лежит в аномалии/воде», урон NPC от аномалий и радиации — зоны действуют только на игрока.
- Сохранение состояния зон/вертолёта в `SaveData`.

## 7. Тест-план в Unity

Фиксировать до/после: health, оружие в руках, Console (файл `Temp/UnityTestLogs/unity-console.log`).

1. **Подбор.** Навести на предмет ≤ 2 м → курсор с названием. F → звук, предмет в инвентаре. Заполнить инвентарь, F → ожидается звук без подбора и `"Not enough space to pickup item"` (6.1.5).
2. **Расходник.** Двойной клик по Medkit при HP < 50: +50, панель `Healing :4…1`, движение заблокировано 4 с. После — записать, вернулось ли оружие (6.2.2).
3. **Дублирование эффекта.** Подобрать Medkit, выбросить, подобрать, использовать при HP = 10. Ожидание «нормы» — 60; если 100+ — гипотеза 6.2.1 подтверждена.
4. **Аномалия.** Войти в `Anomaly`: −20 HP, рывок камеры, звук, красный экран; в Console `[Anomaly] TriggerEnter …` и `[Anomaly test] PASS`. Постоять внутри — повторного урона нет.
5. **Радиация.** Подойти к `RadZone` на 5 м: слабый треск, −1 HP каждые 4 с. На 2 м: сильный треск, −2 HP каждые 2 с. В Console `[Radiation] … applied …` и при старте `[RadioactiveZone test] PASS`.
6. **Вода.** Войти в воду: плавание, оружие убрано; опустить камеру под воду — меняется картинка. Выйти — записать, вернулось ли оружие (6.1.4) и вернулся ли профиль.
7. **Попадания.** Стрелять в землю/дерево/бетон/металл/NPC: частицы и декаль нужного типа; после 20 выстрелов в бетон первая декаль переезжает. В Hierarchy посчитать объекты `decalsPool_*` в корне сцены и под `GamePrefab` (6.1.12). Убить всех NPC и выстрелить ещё раз — нет ли ошибок (6.2.4).
8. **`DistanceDisable`.** Отойти от аномалии на > 50 м, выделить её `Particle System` в Hierarchy: мигает ли флажок активности (6.1.1).
9. **Вертолёт.** Найти `HeliTrigger`, войти: записать, что происходит; `Rotor` вращается постоянно.
10. **Поиск неиспользуемого.** В Hierarchy: `t:ObjectHealth`, `t:AddItemsOnStart`, `t:TestEquipmentCallback`, `t:HelloWorld`, `t:LogMessage` — ожидается 0. `t:DisableRendererOnStart` — записать объект (6.2.8).
11. **Тесты на `Dummy`.** Выделить `Dummy`: три тестовых компонента; в Console при старте — `TEST START` от каждого и `[Weapon animator test] PASS/FAIL`.

## 8. Модульная граница

| Группа | Можно менять отдельно | Обязательно проверить соседей |
|---|---|---|
| Зоны: `Anomaly`, `RadioactiveZone` | Числа в префабах, звуки, частицы, внутреннюю логику расчёта | `PlayerStats` (урон, смерть); тесты на `Dummy` (ожидают урон 20 и сигнатуру `CalculateDamageToApply`) |
| Расходники: `ConsumeEvents`, `ActionWait` | Значения в префабах предметов (очки, таймер, текст, звук) | `DTInventory.UseItem`, `PlayerStats.Add*`, `WeaponManager.HideWeapon`, `FPSController.canMove`; порядок enum |
| Подбор: `UseObjects` | Дистанцию, текст курсора | `PickupItem` (второй луч подбора на той же камере), `Weapon` (анимация `Use`), мобильный UI |
| Вода | Профили пост-обработки | `FPSController` (плавание), `WeaponManager` |
| `HitFXManager` | Префабы частиц/декалей, размер пула, звуки | `Weapon.cs:691-821` и `NPC.cs:914-1040` обращаются к полям напрямую — переименование ломает оба |
| `ObjectHealth` | Всё (сейчас не используется) | `Weapon`, `Grenade`, `Rocket` пишут `health` |
| Декор: `Heli*`, `DistanceDisable`, `DisableRendererOnStart` | Всё внутри файла | Только сцена `Demo Zone` |
| Заплатки: `NameFix`, `EmptyAnimationEventFix` | **Не удалять** без замены | `HitFXManager`, `SaveData`, анимации тела игрока |
| Тесты `Assets/Tests`, `Assets/Scripts` | Свободно; `HelloWorld`/`LogMessage` — кандидаты на удаление по решению пользователя | `Dummy` и его компоненты не удалять; `AttachRadioactiveZoneTestToDummy` |

**Следующий практический шаг:** тесты 2, 3, 6, 8 — они проверяют четыре подтверждённые кодом странности (оружие после действия/воды, дублирование слушателей, мигание `DistanceDisable`). Исправлять — отдельными маленькими правками после подтверждения в Play Mode.

## Источники

`Scripts/Player/UseObjects.cs`, `ActionWait.cs`, `RadioactiveZone.cs`, `WaterVolumeTrigger.cs`, `WaterVolumePostProcessing.cs`, `FPSController.cs:57, 81-87, 153`; `Scripts/ConsumeEvents.cs`, `Anomaly.cs`, `HeliRotor.cs`, `HeliTrigger.cs`, `DistanceDisable.cs`, `DisableRendererOnStart.cs`, `AddItemsOnStart.cs`, `TestEquipmentCallback.cs`; `Scripts/Other/*.cs`; `Scripts/Weapon/HitFXManager.cs`, `ObjectHealth.cs`, `Weapon.cs:208, 280-283, 397, 514-518, 588-589, 691-821`, `WeaponManager.cs:291-325`, `Grenade.cs:60-62`, `Rocket.cs:75-77`; `Scripts/NPC/NPC.cs:21, 152, 441-442, 914-1040`; `Scripts/DT Inventory/PickupItem.cs`, `SaveData.cs:53-63`, `Inventory/DTInventory.cs:560-620, 728, 1226-1246`, `Inventory/InventoryItem.cs:110-122`, `Inventory/InventoryInteraction.cs:27-31`, `Inventory/Item.cs:13, 38`; `Scripts/Editor/AttachRadioactiveZoneTestToDummy.cs`; `Assets/Tests/*.cs`, `Assets/Scripts/*.cs`; префабы `GamePrefab`, `Anomaly`, `RadZone`, `AssaulterForest`, `Zombie`, `Items/Bottle|Conserve|Medkit|Stimpack`; `ProjectSettings/TagManager.asset`; сцены `Demo Zone.unity` (бинарный поиск), `Weapons.unity`.
