# S06. Строительство и размещение объектов — разбор

**Статус: статический разбор; Play Mode не проверялся. Код не изменялся.**

Общий контекст: [PROJECT_SYSTEMS_MAP.md](./PROJECT_SYSTEMS_MAP.md) (строка S06). Соседние разборы: [WEAPON_SYSTEM.md](./WEAPON_SYSTEM.md) (WeaponManager), [INPUT_SYSTEM.md](./INPUT_SYSTEM.md) (ввод).
Все пути скриптов — относительно `Assets/DarkTree FPS/`. Ссылки вида `ObjectPlacement.cs:42` — файл и номер строки.

Как проверялось использование компонентов: GUID скрипта берётся из его `.cs.meta` и ищется в `.prefab`/`.asset` (текстовый YAML). Сцена `Demo Zone.unity` сохранена в **бинарном** виде (55 МБ), поэтому в ней искался тот же GUID в двоичной записи: это показывает только факт «ассет упомянут в сцене», без имён объектов и количества.

---

## 1. Назначение простыми словами

Игрок открывает инвентарь, видит панель «Building» со списком построек, нажимает кнопку постройки. Если в инвентаре есть нужные предметы («рецепт»), инвентарь закрывается, перед игроком появляется полупрозрачный «призрак» постройки (preview — предварительный вид объекта до установки), который следует за взглядом. Колесо мыши вращает его, левая кнопка ставит, правая отменяет. При установке предметы-ресурсы удаляются из инвентаря.

В проекте есть две постройки: костёр (`FireCamp`) и укрытие (`Shelter`).

## 2. Состав

### Скрипты

| Файл | Роль | Где висит (подтверждено поиском GUID) |
|---|---|---|
| `Scripts/Building/BuildingScriptableObjects.cs` | `ScriptableObject` (ассет-данные без объекта в сцене) — описание одной постройки: имя, иконка, префаб, список ресурсов и их количество | Ассеты `Buildings/FireCamp.asset`, `Buildings/Shelter.asset` |
| `Scripts/Building/BuildingManager.cs` | UI-каталог: на старте создаёт по кнопке на каждую постройку; по нажатию проверяет ресурсы и запускает размещение | `Buildings/BuildingPanel.prefab`, корневой объект `Building` (`BuildingPanel.prefab:507-524`). Сам `BuildingPanel` вложен в `Prefabs/GamePrefab.prefab` как дочерний объект `Inventory Canvas` (`GamePrefab.prefab:70364-70374`, source `8a66c1ad…`) |
| `Scripts/Building/ObjectPlacement.cs` | Режим размещения: двигает призрак, красит его, обрабатывает ЛКМ/ПКМ/колесо, списывает предметы | `GamePrefab.prefab`, объект `Player/Camera Holder/Lean Controller/MainCamera - Recoil - HeadBob` (главная камера) |
| `Scripts/Building/ObjectPlaceUtility.cs` | Висит на самом призраке; лучом вниз проверяет, стоит ли он на поверхности с нужным тегом | `Buildings/FireCamp_holder.prefab` (2 шт.: корень и дочерний `FireCamp`), `Buildings/Shelter_holder.prefab` (корень) |
| `Scripts/DT Inventory/DTFPSInventroyExtended.cs` | Статический класс `DTFPSInventoryExtended` (имя файла с опечаткой `Inventroy`, имя класса — без): поиск предметов рецепта в инвентаре | Не компонент; вызывается из `BuildingManager` |

### Ассеты в `Assets/DarkTree FPS/Buildings/`

| Ассет | Что это | Что записано |
|---|---|---|
| `FireCamp.asset` | Рецепт костра | `BuildingName: FireCamp`; `Description: Simple firecamp. Used for cooking and for heat`; префаб → `FireCamp_holder.prefab`; иконка → `Content/Icons/b_campfire_UI.png`; ресурсы: 1 вид — `Prefabs/Items/Branch.prefab`; количество `01000000` = `[1]` |
| `Shelter.asset` | Рецепт укрытия | `BuildingName: Shelter`; `Description: Shelter`; префаб → `Shelter_holder.prefab`; иконка → `Content/Icons/b_palatka_UI.png`; ресурсы: `Branch.prefab` и `Prefabs/Items/Cloth.prefab`; количество `0400000004000000` = `[4, 4]` |
| `FireCamp_holder.prefab` | Префаб костра | Корень `FireCamp_holder`: слой 2 (`Ignore Raycast`), `BoxCollider` (не триггер), `ObjectPlaceUtility` с `tagForObjectPlace: Untagged` (`:45-49`). Дочерний `FireCamp`: меш + ещё один `ObjectPlaceUtility` с `tagForObjectPlace: Terrain` (`:150-154`) |
| `Shelter_holder.prefab` | Префаб укрытия | Корень `Shelter_holder`: слой 2, `ObjectPlaceUtility` с `tagForObjectPlace: Untagged` (`:136-140`). Дочерний `default`: меш + выпуклый `MeshCollider` (`:81-92`) |
| `BuildingPanel.prefab` | UI-панель | Заголовок «Building», `Scroll View` → `Viewport` → `Content`; `BuildingManager` с `buildings: [FireCamp, Shelter]`, `buttonPrefab: BuildingButton`, `contentHolder: Content` (`:519-524`) |
| `BuildingButton.prefab` | Шаблон кнопки | `Image` + `Button` + дочерний `Text` (по умолчанию «Button») |
| `FireCamp.fbx`, `Branches.fbx`, `Cloth.fbx`, `Shelter/Shelter.obj`, `Materials/` | Модели и материалы | Модели костра/укрытия и предметов-ресурсов |

Числа в `.asset` записаны в шестнадцатеричном виде little-endian по 4 байта: `01000000` — это одно число `1`, `0400000004000000` — два числа `4`.

Предметы-ресурсы: `Branch` (`title: Branch`, `id: 405070629`, стекуемый, `maxStackSize: 5`, `Branch.prefab:125-134`) и `Cloth` (`title: Cloth`, `id: 1237662995`, стекуемый, `maxStackSize: 5`, `Cloth.prefab:205-214`). «Стекуемый» (stackable) — несколько одинаковых предметов могут лежать в одной ячейке со счётчиком.

## 3. Как работает

### 3.1. Жизненный цикл

1. **Старт сцены.** `BuildingManager.Start()` для каждого рецепта из `buildings` создаёт кнопку из `buttonPrefab`, подписывает на клик вызов `Build(...)`, ставит имя и иконку, кладёт кнопку в `contentHolder` (`BuildingManager.cs:22-29`). Затем находит в сцене инвентарь и `ObjectPlacement` через `FindObjectOfType` (`BuildingManager.cs:31-32`).
2. `ObjectPlacement.Start()` находит инвентарь и `WeaponManager` (`ObjectPlacement.cs:31-32`).
3. **Каждый кадр** `ObjectPlacement.Update()` ничего не делает, пока `objectToPlace == null` (`ObjectPlacement.cs:37`).
4. **Каждый кадр на призраке** `ObjectPlaceUtility.Update()` пускает луч длиной 0.2 м вниз от точки привязки объекта и выставляет `canPlaceObject = true`, только если луч попал в коллайдер с тегом `tagForObjectPlace` (`ObjectPlaceUtility.cs:17-29`).

### 3.2. Сценарий «игрок строит костёр»

1. Игрок открывает инвентарь (Tab — `Inventory: 9` в `GamePrefab.prefab`, компонент `InputManager`). Панель «Building» лежит внутри `Inventory Canvas`, поэтому видна вместе с инвентарём.
2. Клик по кнопке «FireCamp» → `BuildingManager.Build(buildingCostItems, builingCostItemsAmont, BuildingGameObject)` (`BuildingManager.cs:25`, `:35`).
3. `Build` вызывает `DTFPSInventoryExtended.SearchItemsForBuilding(...)` (`BuildingManager.cs:37`). Тот для каждого вида ресурса вызывает `SearchItemWithCount`, который перебирает `inventory.inventoryItems`, сравнивает `item.id` с `id` компонента `Item` на префабе ресурса и возвращает список, как только набрал нужное число **записей** инвентаря (`DTFPSInventroyExtended.cs:14-20`); иначе `null` (`:23`). Если хватает всех видов — возвращается общий список найденных записей (`:36-45`), иначе `null` (`:48`).
4. Если ресурсов не хватает — `Build` молча ничего не делает (нет ветки `else`, `BuildingManager.cs:37-43`).
5. Если хватает — поиск выполняется второй раз (`:39`), список сохраняется в `objectPlacement.itemsToRemove` (`:40`), создаётся экземпляр префаба постройки и записывается в `objectPlacement.objectToPlace` (`:41`), инвентарь закрывается: `InventoryManager.showInventory = false` (`:42`).
6. Теперь `ObjectPlacement.Update()` каждый кадр:
   - вызывает `weaponManager.HideAll()` (`ObjectPlacement.cs:39`) — оружие выключается, проигрывается анимация `Hide` (`WeaponManager.cs:315-325`);
   - один раз запоминает исходный материал первого `MeshRenderer` призрака в `std_mat` (`:41-42`) и ссылку на `ObjectPlaceUtility` **на корне** призрака (`:44-45`);
   - красит все `MeshRenderer` призрака материалом `canBuild` или `cantBuild` в зависимости от `utility.canPlaceObject` (`:47-62`);
   - **ПКМ** (`KeyCode.Mouse1`): возвращает материал, уничтожает призрак, выходит из режима; предметы не списываются (`:66-78`);
   - пускает луч от камеры вперёд на 5 м (`:80`); если попал, разрешено ставить и зажата **ЛКМ** (`Input.GetMouseButton(0)`): возвращает исходный материал, ставит объект в точку попадания, обнуляет состояние и удаляет все записи из `itemsToRemove` через `inventory.RemoveItem` (`:82-99`);
   - **колесо мыши** меняет угол поворота на `rotationAddAngle` (10°) (`:104-112`);
   - двигает призрак в точку попадания +0.1 м по Y и плавно доворачивает по нормали поверхности и накопленному углу (`:114-121`).
7. После установки объект остаётся в сцене как обычный экземпляр префаба; `ObjectPlacement` снова «спит».

## 4. Данные и сериализованные поля

«Сериализованное поле» — публичное поле компонента, значение которого хранится в префабе/сцене и видно в Inspector.

**`BuildingScriptableObjects`** (`BuildingScriptableObjects.cs:8-17`): `BuildingName`, `Description`, `BuildingGameObject`, `BuildingIcon`, `buildingCostItems` (массив префабов предметов), `builingCostItemsAmont` (массив количеств; опечатки в имени — часть формата ассета). Создаётся через меню `Create → Building Data` (`:5`). Поле `Description` в коде нигде не читается (поиск `.Description` по `Scripts` — не найдено).

**`BuildingManager`** (`BuildingManager.cs:11-15`): `buildings`, `buttonPrefab`, `contentHolder`.

**`ObjectPlacement`** (`ObjectPlacement.cs:12-24`), значения в `GamePrefab.prefab`: `maxPlacementDistance: 5`, `rotationAddAngle: 10`, `std_mat: пусто`, `canBuild` → материал `bff37f8b…`, `cantBuild` → материал `72ccf7d7…`. Поля `objectToPlace` и `itemsToRemove` скрыты от Inspector (`[HideInInspector]`), но публичные — их пишет `BuildingManager`.

**`ObjectPlaceUtility`** (`ObjectPlaceUtility.cs:7-9`): `canPlaceObject`, `tagForObjectPlace` (по умолчанию в коде `"Terrain"`, в префабах на корне — `Untagged`).

Теги проекта (`ProjectSettings/TagManager.asset`): `Item, Target, Wood, Concrete, Dirt, Fire, Metal, Crosshair, NPC, Flesh, Cover, Terrain, Collision, LootBox`. Слой 2 — `Ignore Raycast`.

## 5. Связи с другими системами

| Кто | Кого вызывает | Чем |
|---|---|---|
| `BuildingManager` | S05 Инвентарь | `DTFPSInventoryExtended.SearchItemsForBuilding(inventory, GameObject[], int[])` (`BuildingManager.cs:37,39`); запись в статическое `InventoryManager.showInventory` (`:42`) |
| `BuildingManager` | `ObjectPlacement` | прямая запись полей `itemsToRemove`, `objectToPlace` (`:40-41`) |
| `ObjectPlacement` | S03 Оружие | `WeaponManager.HideAll()` (`ObjectPlacement.cs:39`). Это **единственный** вызов `HideAll` в проекте |
| `ObjectPlacement` | S05 Инвентарь | `DTInventory.RemoveItem(InventoryItem)` (`:96`; реализация `DTInventory.cs:772-789` — удаляет запись, UI-ячейку и сам объект предмета) |
| `ObjectPlacement` | Ввод | напрямую `Input.GetKeyDown(KeyCode.Mouse1)`, `Input.GetMouseButton(0)`, `Input.GetAxis("Mouse ScrollWheel")` — мимо `InputManager` (`:66, :83, :104, :109`) |
| `ObjectPlacement`, `ObjectPlaceUtility` | Физика | `Physics.Raycast` без маски слоёв (`ObjectPlacement.cs:80`, `ObjectPlaceUtility.cs:17`) |

Обратных вызовов нет: `SaveData` строительство не упоминает (поиск `Building|ObjectPlace` в `SaveData.cs` — не найдено).

## 6. Контракт (что опасно переименовывать)

- Имена полей `BuildingScriptableObjects` (включая опечатки `builingCostItemsAmont`) — на них завязаны оба `.asset`. Переименование без `[FormerlySerializedAs]` обнулит рецепты.
- Публичные поля `ObjectPlacement.objectToPlace`, `itemsToRemove` и метод `BuildingManager.Build` — используются между скриптами.
- Сигнатуры `DTFPSInventoryExtended.SearchItemWithCount` / `SearchItemsForBuilding`.
- `WeaponManager.HideAll()` и имя состояния аниматора `"Hide"` (`WeaponManager.cs:323`).
- На корне префаба постройки обязан быть `ObjectPlaceUtility` (иначе `utility == null` → `NullReferenceException` в `ObjectPlacement.cs:47`) и хотя бы один `MeshRenderer` в иерархии (`:42`).
- На префабе ресурса обязан быть компонент `Item` (`DTFPSInventroyExtended.cs:16`); совпадение определяется по `Item.id`.
- Слой 2 (`Ignore Raycast`) у префабов построек: благодаря ему лучи размещения не попадают в сам призрак.
- GUID ассетов (`.meta`) — на них ссылаются `BuildingPanel.prefab` и `GamePrefab.prefab`.

## 7. Наблюдения

### 7.1. Подтверждено кодом и ассетами

1. **Считаются записи инвентаря, а не количество в стопке.** `SearchItemWithCount` добавляет в список по одной записи на каждый `InventoryItem` с нужным `id` и сравнивает `items.Count` с требуемым числом (`DTFPSInventroyExtended.cs:14-20`), `stackSize` не читается. Для «Shelter» нужно 4 отдельные записи `Branch` и 4 отдельные записи `Cloth`; одна стопка из 5 веток считается как 1. При установке каждая найденная запись удаляется целиком (`ObjectPlacement.cs:94-97`) — стопка пропадает вся.
2. **`maxPlacementDistance` не используется**: дальность луча задана литералом `5f` (`ObjectPlacement.cs:14` — объявление, `:80` — использование числа).
3. **Проверка «можно ставить» берётся с корня префаба**, где `tagForObjectPlace: Untagged` (`ObjectPlacement.cs:45`; `FireCamp_holder.prefab:49`, `Shelter_holder.prefab:140`). Второй `ObjectPlaceUtility` на дочернем `FireCamp` с тегом `Terrain` на решение не влияет (но свой луч каждый кадр пускает).
4. **Если луч от камеры никуда не попал**, `hit` остаётся пустым, и призрак всё равно переносится в `hit.point + (0, 0.1, 0)` = точку (0, 0.1, 0) мира (`ObjectPlacement.cs:80`, `:114`).
5. **`HideAll()` вызывается каждый кадр** режима размещения (`ObjectPlacement.cs:39`): каждый кадр печатает `"Hide weapon works"` в Console и заново запускает анимацию `Hide` (`WeaponManager.cs:317-323`).
6. **Обратного «показать оружие» нет.** `HideAll` выключает объект оружия, но не обнуляет `activeWeapon` (`WeaponManager.cs:319-323`); после установки/отмены `ObjectPlacement` ничего не вызывает (`:66-78`, `:83-99`).
7. **Повторное нажатие кнопки постройки во время размещения** перезаписывает `objectToPlace` новым экземпляром, старый призрак не уничтожается (`BuildingManager.cs:41`; проверки на незавершённое размещение нет).
8. **При нехватке ресурсов игрок не получает сообщения** (`BuildingManager.cs:37-43`).
9. **Установленный объект остаётся на слое 2 и с работающим `ObjectPlaceUtility`** — код установки слой и компоненты не меняет (`ObjectPlacement.cs:85-91`).
10. **Постройки не сохраняются**: в `SaveData.cs` упоминаний нет.
11. **При установке объект ставится ровно в `hit.point`** (`:87`), без смещения +0.1 и без финального выравнивания поворота (поворот остаётся таким, до какого дошёл `Lerp`).
12. `Build` вызывает поиск ресурсов дважды подряд (`BuildingManager.cs:37`, `:39`) — лишняя работа, на результат не влияет.
13. Материал возвращается только первому `MeshRenderer` в иерархии (`ObjectPlacement.cs:68`, `:85`), тогда как перекрашиваются все (`:49-53`). В существующих двух префабах `MeshRenderer` один, так что сейчас это не проявляется.

### 7.2. Гипотезы, требуют проверки в Unity

1. **Где реально можно строить.** Из 7.1.3 следует: ставить можно на коллайдеры с тегом `Untagged`, а на коллайдер с тегом `Terrain` — нельзя. Какой тег у земли в `Demo Zone` — не выяснено (сцена бинарная). Проверить: призрак зелёный на земле или красный?
2. **Оружие после строительства.** Из 7.1.6: оружие может остаться скрытым; нажатие клавиши того же слота, вероятно, не вернёт его, так как `SlotInput` выходит, если `activeWeapon.currentItem` совпадает с предметом слота (`WeaponManager.cs:204-207`). Смена на другой слот должна сработать.
3. **Рассинхрон списка на списание.** `itemsToRemove` собирается в момент клика по кнопке. Если до установки игрок снова откроет инвентарь и выбросит/израсходует ресурс, `RemoveItem` может обратиться к уже уничтоженному объекту (`DTInventory.cs:783-784`).
4. **Поведение `RemoveItem` для экипированных/лутбокс-предметов** — `inventoryItems` перебирается без фильтра; попадают ли туда предметы из открытого ящика, не проверялось.
5. **Пули и лучи сквозь постройки**: слой `Ignore Raycast` у установленного объекта может делать его «прозрачным» для попаданий, выполняемых через `Physics.Raycast` без маски.
6. **Мобильный ввод**: размещение читает только мышь; на тач-устройстве режим, вероятно, недоступен.
7. **Строительство на ходу при `Time.timeScale`/смерти игрока** — отдельных проверок в коде нет.

### 7.3. Не найдено

- Какой-либо эффект построек (костёр «для готовки и тепла» из описания): скриптов, реагирующих на построенный костёр или укрытие, нет.
- Вывод `Description` в UI.
- Разборка/удаление построек игроком.

## 8. Тест-план в Unity

Перед каждым шагом фиксировать содержимое инвентаря и Console (см. [UNITY_EVENT_TRACE.md](./UNITY_EVENT_TRACE.md)).

1. **Каталог.** Play → Tab. Ожидание: панель «Building» с двумя кнопками, имена `FireCamp`, `Shelter`, иконки.
2. **Нет ресурсов.** Пустой инвентарь → клик `FireCamp`. Ожидание по коду: ничего не происходит, инвентарь остаётся открытым.
3. **Костёр.** Подобрать 1 ветку → клик `FireCamp`. Ожидание: инвентарь закрылся, оружие спрятано, призрак следует за взглядом. Записать цвет призрака на земле, на камне, на дереве (проверка гипотезы 7.2.1).
4. **Поворот/отмена.** Колесо — призрак вращается шагами по 10°. ПКМ — призрак исчез, ветка осталась.
5. **Установка.** ЛКМ при «зелёном» призраке. Ожидание: объект остался, ветка исчезла из инвентаря.
6. **Стопки.** Собрать 4 ветки и 4 ткани отдельными ячейками → `Shelter` доступен. Затем сложить ветки в одну стопку → по коду `Shelter` перестанет запускаться (7.1.1).
7. **Взгляд в небо** в режиме размещения: записать, куда уходит призрак (7.1.4).
8. **Оружие после.** С оружием в руках построить костёр; проверить, вернулось ли оружие, работает ли клавиша слота, работает ли смена слота (7.2.2).
9. **Двойной клик по кнопке** постройки: проверить, не остаётся ли «висящий» призрак (7.1.7).
10. **Console.** Посчитать сообщения `Hide weapon works` за время размещения (7.1.5).
11. **F5 → F9** после постройки: по коду постройка не восстановится (7.1.10).

## 9. Модульная граница

**Можно менять отдельно (не трогая соседей):**
- значения в `FireCamp.asset` / `Shelter.asset` (имя, иконка, количество), добавление нового рецепта-ассета и строки в `BuildingPanel.prefab → buildings`;
- материалы `canBuild` / `cantBuild`, `rotationAddAngle`;
- внутреннюю логику `ObjectPlaceUtility` и `ObjectPlacement.Update`, пока сохраняются публичные поля из раздела 6.

**Требует проверки соседей:**
- любое изменение подсчёта ресурсов → S05 Инвентарь (`DTFPSInventoryExtended`, `DTInventory.RemoveItem`, стопки);
- изменение работы с оружием (`HideAll`/возврат) → S03 Оружие, см. [WEAPON_SYSTEM.md](./WEAPON_SYSTEM.md), раздел 10;
- смена слоя/тегов построек → физика, попадания оружия, NPC;
- сохранение построек → S07 SaveData (сейчас интеграции нет).

**Следующий практический шаг:** выполнить тесты 3, 6, 8 — они покрывают три главных неочевидных поведения (тег поверхности, счёт по стопкам, скрытое оружие). До этого код не менять.

## Источники

`Scripts/Building/BuildingManager.cs`, `BuildingScriptableObjects.cs`, `ObjectPlacement.cs`, `ObjectPlaceUtility.cs`; `Scripts/DT Inventory/DTFPSInventroyExtended.cs`; `Scripts/DT Inventory/Inventory/DTInventory.cs:772-789`; `Scripts/Weapon/WeaponManager.cs:177-230, 291-325`; `Buildings/*.asset`, `Buildings/*.prefab`; `Prefabs/GamePrefab.prefab`; `Prefabs/Items/Branch.prefab`, `Cloth.prefab`; `ProjectSettings/TagManager.asset`.
