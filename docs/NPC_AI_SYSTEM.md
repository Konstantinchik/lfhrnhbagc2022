# S04 «NPC и AI» — разбор системы

**Статус: статический разбор; Play Mode не проверялся. Код не изменялся.**

Источники: все файлы `Assets/DarkTree FPS/Scripts/NPC/*.cs` прочитаны целиком; соседние системы — по местам вызовов; префабы — по YAML-тексту. Сцена `Demo Zone.unity` сохранена в **бинарном** формате, поэтому по ней доступен только поиск байтовых последовательностей (GUID префабов и имена объектов), а значения инспектора, переопределённые в сцене, прочитать нельзя — это оговорено везде, где важно.

Ссылки вида `NPC.cs:123` — номера строк в файлах из `Assets/DarkTree FPS/Scripts/...` на момент разбора.

---

## 1. Назначение

Система даёт игре компьютерных противников (NPC — non-player character, персонаж, которым управляет программа). Есть два независимых вида:

| | Стрелок (`NPC.cs`) | Зомби (`ZombieNPC.cs`) |
|---|---|---|
| Префаб | `Prefabs/AssaulterForest.prefab` | `Prefabs/Zombie.prefab` |
| Атака | Стреляет лучом (raycast) из точки на оружии | Ближний бой: анимация удара вызывает `FightHit()` |
| Обнаружение игрока | Зрение (`NPCVision`): угол обзора, дальность, прямая видимость, таймер «узнавания», слух, звук выстрела | Только расстояние до игрока < 25 м или флаг `isWorried`; зрение не используется |
| Поведение в бою | Ищет укрытие (`CoverList`/`Cover`), приседает, выходит проверить цель, перемещается по случайным точкам вне видимости цели | Бежит прямо на игрока |
| Поведение в покое | Idle / Patrol / FollowToTarget / Wander | Случайное блуждание |
| Фракции (свой/чужой) | Есть поля `myFraction` / `targetFraction` | Нет |
| Смерть | Ragdoll (тело-«тряпичная кукла» на физике), труп можно обыскать как LootBox | Ragdoll, компонент `ZombieNPC` уничтожается |
| Сохранение | Имя в базе + позиция + поворот + `lookPosition` | Позиция + поворот + `isWorried` |

Оба вида двигаются одинаково: `NavMeshAgent` (компонент Unity, который прокладывает путь по NavMesh — заранее запечённой «карте проходимости» уровня) выдаёт желаемую скорость, а скрипт-контроллер, скопированный из Unity Standard Assets `ThirdPersonCharacter`, превращает её в параметры анимации; само смещение даёт root motion (движение, записанное в анимационном клипе).

Автор ассета прямо пишет в коде, что система «WIP» и логика «very basic» (`NPC.cs:13-20`). Упомянутого там файла `NPCStates.cs` в проекте нет — вся логика состояний находится в `NPC.cs`.

---

## 2. Состав

| Файл | Роль | Где стоит (по префабам) |
|---|---|---|
| `NPC.cs` (1252 строки) | Главный скрипт стрелка: машина состояний, стрельба, получение урона, смерть, друзья, контроллер движения/анимации | Корень `AssaulterForest.prefab` (слой `NPC`, тег `Flesh`) |
| `ZombieNPC.cs` (468) | Главный скрипт зомби: погоня, удар, блуждание, смерть, контроллер движения | Корень `Zombie.prefab` |
| `NPCVision.cs` (262) | Зрение и слух: списки целей, таймер обнаружения, выбор ближайшей цели | Дочерний объект `NPC Vision` у стрелка (слой `Target`); дочерний `Vision` у зомби (там фактически не используется) |
| `AIControl.cs` (77) | Мост между `NavMeshAgent` и методом `Move()` персонажа | Корень обоих префабов; у зомби `usedByZombie = 1` |
| `CoverList.cs` (74) | Реестр укрытий сцены + статический список живых стрелков + оповещение о выстреле игрока | `Prefabs/NPC&CoverList.prefab` (один объект на сцену) |
| `Cover.cs` (14) | Точка укрытия: флаг `occupied` и ссылка на свой коллайдер | `Prefabs/Cover.prefab`, 4 шт. внутри `Prefabs/CoverHolder.prefab` (слой `Cover`, `BoxCollider` 0.3×0.3×0.3, не триггер) |
| `AimIKLite.cs` (31) | Доворот цепочки костей так, чтобы ствол смотрел на цель | Корень `AssaulterForest` (3 кости, `aimTransform` = объект `Shot point`) |
| `IK.cs` (45) | IK левой руки (inverse kinematics — подтягивание кисти к точке на оружии) | Корень `AssaulterForest` (`ikActive = 1`, цель — `IK_Target`) |
| `FirePointNPC.cs` (18) | Пустой компонент-маркер: по нему `NPC` находит точку выстрела | Объект `Shot point` в `AssaulterForest` |
| `PlayerHitTarget.cs` (13) | Маркер «в эту часть тела может попасть игрок»: ссылка на `NPC` и на `LootBox` | 11 костей `AssaulterForest` (Hips, Spine2, Head, руки, ноги), все с тегом `Flesh`, слой `Default` |
| `VisionUI.cs` (21) | Индикатор «меня замечают» (глаз + заполнение) в HUD игрока | Объект `VisionDetector` в `GamePrefab.prefab` (слой `UI`) |
| `DrawRay.cs` (11) | Отладочная отрисовка луча вперёд | Не найден ни в одном префабе/сцене проекта |

Связанные ассеты:

- Animator Controller стрелка: `Content/NPC/ThirdPersonAnimatorController.controller`; зомби: `Standard Assets/Characters/ThirdPersonCharacter/Animator/ThirdPersonAnimatorControllerZombie.controller`.
- Animation Event `FightHit` — в `Content/Zombie/zombie attack.fbx.meta`; события `PlayFootstep` — в анимациях `Content/NPC`.
- NavMesh сцены: `Assets/DarkTree FPS/Demo Zone/NavMesh.asset`.
- База префабов для загрузки сейва: `Prefabs/SaveManagerInstance.prefab` → `AssetsDatabase.NPCs = [AssaulterForest]`, `zombie = Zombie`.

Что видно в сцене `Demo Zone.unity` (бинарный поиск): ссылки на префабы `AssaulterForest`, `Zombie`, `NPC&CoverList`, `Cover`, `GamePrefab` присутствуют, `CoverHolder` — нет. Имена объектов: `AssaulterForest`, `AssaulterForest (1)…(3)` (4–5 экземпляров), `Zombie`, `Zombie (1)…(7)` (8), `Cover`, `Cover (1)…(18)` под родителем `Covers`, один `NPC&CoverList`. В `Weapons.unity` NPC нет. В Build Settings включена только `Demo Zone.unity`.

---

## 3. Как это работает

### 3.1. Жизненный цикл стрелка (`NPC.cs`)

`[RequireComponent]` требует на том же объекте: `IK`, `HitFXManager`, `AIControl`, `FootstepSound`, `GetCollisionTag`, `AimIKLite`, `Rigidbody`, `NavMeshAgent`, `AudioSource`, `Animator` (`NPC.cs:21-24`).

**`Start()`** (`NPC.cs:144-205`):

1. Берёт ссылки на компоненты. `vision` ищется в дочерних объектах; `shootPoint` **перезаписывается** трансформом объекта с `FirePointNPC` (`:153`), `fireFX` — первым найденным в детях `ParticleSystem` (`:154`). То есть значения этих двух полей из инспектора игнорируются.
2. Создаёт в корне сцены служебный объект `"<имя> visibilityCheckObject"` с выключенным `SphereCollider` радиуса 0.3 (`:156-159`) — «щуп» для проверки, видна ли точка со стороны цели.
3. `playerHealth = FindObjectOfType<PlayerStats>()` (`:161`).
4. Все `Rigidbody` в иерархии делает кинематическими (ragdoll «спит»), корневой — обратно динамическим, с заморозкой вращения (`:163-168`, `:183`).
5. Запускает корутину `CheckFriends()` (`:172`) — раз в 0.5 с пересобирает список союзников.
6. `desiredPosition = transform.position` (`:175`), `coverList = FindObjectOfType<CoverList>()` (`:186`), `timerToLeaveCover = Random.Range(4, 10)` — выбирается один раз на всю жизнь NPC (`:188`).
7. Всем своим `PlayerHitTarget` проставляет `npc = this` и `lootBox = GetComponent<LootBox>()` (`:196-202`).
8. Регистрируется в `CoverList.sceneActiveNPC` (`:204`).

**`Update()`** (`NPC.cs:209-267`), каждый кадр, в таком порядке:

1. Если агент не стоит на NavMesh — `Destroy(gameObject)` (`:211-214`).
2. `aiControl.SetTarget(desiredPosition)` — куда идти (`:216`).
3. `health <= 0` → `Death()` (`:218-221`).
4. Если `debug == true` — рисует луч ствола; **если `debug == false` — вызывает `TargetMachine()`** (`:223-227`). Значение по умолчанию в коде `debug = true` (`:29`), в префабе `debug: 0`.
5. Передаёт `curretTarget` в `AimIKLite.target` (`:229-232`).
6. Выставляет bool аниматора `Crouch` (`:234-243`).
7. Плавно поворачивает корпус: без цели — к `desiredPosition`, с целью — к цели (`:245-264`).
8. `Attack()` — если зрение даёт видимую цель, вызывает `Fire()` (`:266`, `:269-275`).

Корутины и отложенные вызовы: `CheckFriends` (бесконечная), `Invoke("Shot")`, `Invoke("AddAmmo", 3)`, `Invoke("GoToIdle", …)`.

`OnAnimatorMove()` (`:1215-1227`) переносит root motion в скорость `Rigidbody`.

### 3.2. Машина состояний стрелка (`TargetMachine`, `NPC.cs:512-616`)

Явного `enum` состояний нет. Состояние задаётся комбинацией полей: `curretTarget` (текущая цель, написание с опечаткой — так в коде), `idle`, `cover`, `lookingForTarget`, `destinationReached`, `m_Crouching`. Ниже состояния названы условно.

```
                       ┌───────────────────────────────────────────────┐
                       │ ПОКОЙ  (curretTarget == null && idle)         │
                       │ вес слоя аниматора 1 → 0, lookAtTarget=false  │
                       │ подрежим по defaultBehavior:                  │
                       │   Idle | Patrol | FollowToTarget | Wander     │
                       └───────────────┬───────────────────────────────┘
        зрение вернуло видимую цель    │      союзник вызвал Shout() и
        (NPC.cs:531-541)               │      подарил свою цель (NPC.cs:784-796)
                                       ▼
                       ┌───────────────────────────────────────────────┐
                       │ БОЙ (curretTarget != null), вес слоя 1 → 1    │
                       └───────────────┬───────────────────────────────┘
                                       │ cover == null → CoverList.FindClosestCover()
              укрытие найдено          │                    укрытие не найдено
        ┌──────────────────────────────┴───────────────┐
        ▼                                              ▼
┌──────────────────────┐   до цели < 10 м     ┌──────────────────────────────┐
│ ИДЁТ В УКРЫТИЕ       │ ───────────────────► │ ТАКТИКА (TacticalBehavior)   │
│ occupied=true        │  (укрытие            │ speed=1, стоя; идёт к        │
│ speed=2, stop=0.2    │   освобождается,     │ случайной точке в радиусе    │
└─────────┬────────────┘   NPC.cs:595-605)    │ 20 м, не видимой от цели     │
          │ дистанция до укрытия < 0.2        └──────────────────────────────┘
          ▼                                              ▲
┌──────────────────────┐                                 │ цель снова видна
│ В УКРЫТИИ            │                                 │ (lookingForTarget=false)
│ Crouch=true          │   цель не видна дольше          │
│ timer = Time.time    │   timerToLeaveCover (4–10 с)    │
└─────────┬────────────┘   (NPC.cs:521-529)              │
          └────────────────────────────►┌────────────────┴─────────────┐
                                        │ ПОИСК (lookingForTarget)     │
                                        │ встал, освободил укрытие,    │
                                        │ идёт в позицию цели на       │
                                        │ момент выхода (NPC.cs:830)   │
                                        └──────────────────────────────┘

Из любого состояния: health <= 0 → СМЕРТЬ (Death, NPC.cs:455-492)
Возврат в ПОКОЙ: только если curretTarget стал null → Invoke("GoToIdle") (NPC.cs:514-519)
```

Условия по блокам кода:

- **Блок «успокоиться»** (`:514-519`): `curretTarget == null && !idle` → `Invoke("GoToIdle", calmDownTimer)`, `lookingForTarget = false`. `GoToIdle()` освобождает укрытие и ставит `idle = true` (`:501-510`).
- **Блок «выйти из укрытия»** (`:521-529`): видимой цели нет, есть укрытие, есть `curretTarget`, не в поиске → через `timerToLeaveCover` секунд после последнего обновления `timer` вызывается `ExitCoverToCheckTarget()` (`:830-846`): встать, освободить укрытие, `desiredPosition = curretTarget.position`, `lookingForTarget = true`.
- **Блок «вижу цель»** (`:531-541`): сбрасывает таймер, отменяет `GoToIdle`, встаёт, запоминает `curretTarget`, `lookAtTarget = true`, `lookingForTarget = false`, `idle = false`.
- **Основной блок** (`:543-614`) выполняется только когда `!lookingForTarget`:
  - ПОКОЙ (`:545-567`) — выбор подрежима;
  - иначе БОЙ (`:568-613`): поиск укрытия; приседание ближе 1.5 м к укрытию (`:592-593`); если до цели < 10 м — встать, освободить укрытие, `TacticalBehavior()` (`:595-605`); по приходу в укрытие — присесть и запомнить время (`:607-612`).

**`TacticalBehavior()`** (`:625-644`): если есть цель и предыдущая точка достигнута (или путь частичный) — `GetRandomPositonOutsidePlayerFOV(transform.position, 20)`. Тот берёт случайную точку в сфере 20 м, проецирует на NavMesh, ставит туда «щуп» и пускает луч от цели к щупу (`CheckTargetTransformVisibility`, `:804-826`); если первым попался не щуп — точка считается безопасной и становится `desiredPosition` (`:848-876`). В `ProjectSettings/DynamicsManager.asset` включён `m_AutoSyncTransforms: 1`, поэтому перенос щупа и луч в одном кадре согласованы.

**Подрежимы покоя:**

- `Idle` — ничего не делает (`:557-559`).
- `Patrol` (`:687-708`) — идёт по `patrolPoints` по кругу, `speed = patrolSpeed`, `stoppingDistance = 1`.
- `Wander` (`:712-731`) — раз в 5–9 с, дойдя до точки, выбирает новую случайную в радиусе `radiusToTakeRandomPosition`.
- `FollowToTarget` (`:733-759`) — идёт за `targetToFollow`; если объект называется `"Player"`, скорость удваивается, когда игрок бежит (`InputManager.IsRunning()`).

В префабе стоит `defaultBehavior: 3` (Wander), `enemyTactics: 0`.

### 3.3. Зрение и обнаружение (`NPCVision.cs`)

`Start()` (`:51-56`): находит объект по имени `"Player"` и коллайдер объекта с тегом `MainCamera` (`playerCollider`).

`Update()` каждый кадр:

1. **Слух** (`:62-67`): если до игрока ближе `hearDistance` (10 м), игрок движется (`FPSController.CheckMovement()`) и не присел (`FPSController.crouch`) — коллайдер объекта `Player` добавляется в `visibleTargets` и `potentialTargets`, `playerDetected = true`.
2. **Таймер узнавания** (`:69-82`): пока `seePlayer`, `detectionTimer` растёт со скоростью `2 × (20 / дистанция)` в секунду (вдвое медленнее, если игрок присел); иначе убывает. Порог — `timeToDetect` (3 с).
3. **Индикатор** (`:84-87`): пока игрок не обнаружен, наибольший таймер пишется в статическое `VisionUI.visionAmount`.
4. **Скан** раз в `visibilityCheckInterval` (`:89-192`): три списка создаются заново; `Physics.OverlapSphere(detectionRange, layerMaskForOverlap)`; для каждого коллайдера:
   - тег `MainCamera` и `!playerIsFriendly` → проверка угла (`FOV/2`) и луч с маской `layerMaskForVision`; если луч упёрся именно в этот коллайдер — `seePlayer = true`, а при `detectionTimer > timeToDetect` **или** уже выставленном `playerDetected` (и живом игроке) цель попадает в списки (`:105-147`);
   - иначе, если на объекте коллайдера есть `NPC` с `myFraction == targetFraction` → такая же проверка угла и луча, цель добавляется без таймера (`:149-176`);
   - объекты на слое `Dead` убираются (`:183-189`).
5. Если игрок мёртв — его коллайдер убирается из `potentialTargets` (`:194-197`).

`SelectBestTarget(bool visibleOnly)` (`:201-251`) возвращает ближайший (до 100 м) коллайдер из `visibleTargets`.

Дополнительные входы обнаружения:

- **Выстрел игрока**: `Weapon.cs:453-454` → `CoverList.NotifyNPCAboutShot(позиция, дистанция)` → для стрелков в радиусе `NPC.ListenToPlayerShot()` (`NPC.cs:765-772`): `playerDetected = true`.
- **Попадание**: `NPC.GetHit()` (`NPC.cs:367-409`) тоже ставит `playerDetected = true`.
- **Крик союзникам**: `Shout()` (`NPC.cs:775-802`) — союзникам без цели передаётся `curretTarget`, `playerDetected`, `lookAtTarget`.

Значения в префабе стрелка: `visibilityCheckInterval 0.5`, `FOV 130`, `detectionRange 100`, `timeToDetect 3`, `hearDistance 10`, `targetFraction 1`, `layerMaskForVision` = все слои, `layerMaskForOverlap` = слои `Weapon`, `Player`, `NPC`, `Target`.

### 3.4. Фракции

- `NPC.myFraction` (`NPC.cs:31`) — номер своей фракции; `NPCVision.targetFraction` (`NPCVision.cs:27`) — номер фракции, которую этот NPC считает врагом; `NPCVision.playerIsFriendly` (`:33`) — не считать игрока целью по зрению.
- Союзники («друзья») — все `PlayerHitTarget` в радиусе 50 м, у которых `npc.myFraction == myFraction` (`NPC.cs:887-910`).
- `GetHit()` различает отправителя урона: NPC чужой фракции → ветка без пометки игрока (`:369-386`); всё остальное (оружие игрока, взрыв, NPC своей фракции) → ветка «виноват игрок» (`:387-408`).
- В префабе: `myFraction 0`, `targetFraction 1`, `playerIsFriendly 0`. Значения экземпляров в сцене прочитать не удалось.

### 3.5. Укрытия (`CoverList` / `Cover`)

- `CoverList.Awake()` создаёт статический список `sceneActiveNPC` (`CoverList.cs:16-19`); `Start()` добавляет в `covers` все `Cover` сцены (`:34-37`).
- `FindClosestCover(myPos, enemyPos)` (`:39-73`): для каждого укрытия пускает луч от врага к укрытию. Укрытие годится, если луч упёрся во что-то, чьё **имя коллайдера** отличается от имени коллайдера укрытия (то есть между врагом и точкой есть преграда), укрытие дальше 5 м от врага и не занято. Из годных выбирается ближайшее к NPC.
- Занятость — простой флаг `Cover.occupied`; ставит его NPC (`NPC.cs:579`), снимают `GoToIdle`, `GetHit`, `ExitCoverToCheckTarget` и правило «цель ближе 10 м».

### 3.6. Навигация и движение

- `AIControl.Start()` отключает у агента поворот (`updateRotation = false`), оставляя перенос позиции (`AIControl.cs:36-37`).
- `AIControl.Update()` (`:40-70`): если `target != Vector3.zero` — `agent.SetDestination(target)`; затем `Move(agent.desiredVelocity)` либо `Move(Vector3.zero)`, если цель достигнута. Весь вызов обёрнут в пустой `try/catch`.
- `NPC.Move()` (`NPC.cs:1084-1119`): пишет в аниматор `LookAtTarget`, считает `m_ForwardAmount`, проверяет землю лучом вниз (`CheckGroundStatus`, `:1228-1249`), обновляет параметры аниматора (`UpdateAnimator`, `:1146-1181`).
- Скорости агента (`0.5`, `1`, `2`) фактически служат входом в blend tree анимации (`Forward`), а не метрами в секунду.
- У зомби `AIControl.target` никто не задаёт: `ZombieNPC` сам вызывает `agent.SetDestination()` (`ZombieNPC.cs:144`, `:212-218`), а `AIControl` только транслирует `desiredVelocity` в `ZombieNPC.Move()`.

### 3.7. Стрельба и урон по игроку

`Fire()` (`NPC.cs:279-324`), вызывается каждый кадр, пока есть видимая цель:

1. `Shout()` — оповестить союзников.
2. Если есть патроны и нет перезарядки и прошёл `fireRate` — по `fireMode` планируется 1, 2 или 3 вызова `Shot` через `Invoke` с шагом 0.1 с, списываются патроны.
3. Иначе — `Reload()`: `animator.Play("Reload")`, через 3 с `AddAmmo()` ставит `ammo = 30` (`:352-364`).
4. При `useRandomRateAndMode` на каждом вызове `fireMode = Random.Range(1, 3)` (то есть 1 или 2), `fireRate = Random.Range(0.5f, 1f)` (`:319-323`).

`Shot()` (`:328-350`): звук, `animator.Play("Shot", 1)`, вспышка, луч из `shootPoint` вперёд с разбросом `±shootingAccuracySpread` по каждой оси, маска `shotLayerMask`.

`ApplyHit()` (`:417-451`):

- попали в коллайдер с тегом `MainCamera` → `PlayerStats.ApplyDamage(Random.Range(damage / 2, damage))` — при `damage = 10` это 5…9;
- иначе — рикошет и частицы по тегу поверхности (`Wood`/`Concrete`/`Dirt`/`Metal`/`Flesh`/`Player`), затем: если цель ближе 8 м и на её объекте есть `NPC` — урон ей напрямую; иначе урон `NPC` на объекте, в который попал луч.

По префабам: у игрока тег `MainCamera` стоит на объекте `MainCamera - Recoil - HeadBob` (слой `Weapon`, `SphereCollider` радиуса 0.3), а объект `Player` с `CharacterController` — на слое `Ignore Raycast`. Маска `shotLayerMask` в префабе стрелка = `Default`, `Weapon`, `Player`, `NPC`. Следовательно, урон игроку проходит только при попадании луча в сферу радиусом 0.3 на камере.

Направление ствола задаёт `AimIKLite`: в `LateUpdate` он 4 итерациями доворачивает 3 кости так, чтобы ось `(0,0,1)` объекта `Shot point` смотрела на `target.position` (`AimIKLite.cs:13-30`).

### 3.8. Получение урона и смерть стрелка

Кто наносит урон — см. раздел 5. `GetHit(int damage, Transform damageSender)` (`NPC.cs:367-409`): встать и освободить укрытие, `health -= damage`, звук боли, `Shout()`.

`Death()` (`NPC.cs:455-492`):

1. Убирает себя из `CoverList.sceneActiveNPC`, отменяет все `Invoke`.
2. Отключает `Animator`, `IK`, `AimIKLite`, `AIControl`, `NavMeshAgent`.
3. Всем дочерним коллайдерам: включить, слой `Default`, тег `Flesh`; всем `Rigidbody` — снять кинематику (ragdoll).
4. Если задан `dropWeaponItem` — отцепляет оружие и включает ему физику (в префабе поле пустое).
5. Корню ставит слой `Dead`, отключает сам компонент `NPC`.

**Лут.** На корне префаба висит `DTInventory.LootBox` (`spawnRandomItems 1`, `randomItems` = 5.45 Ammo, Bottle, Conserve, `randomItemsCount 0`, `lootBoxItems` пуст). Игрок обыскивает тело через `PickupItem.cs:91-97`: луч из камеры попал в коллайдер с тегом `Flesh` и `PlayerHitTarget` → `inventory.SearchLootBox(hitTarget.lootBox)`.

### 3.9. Зомби (`ZombieNPC.cs`)

- `OnEnable()`/`Start()` (`:85-130`): ссылки, корутина `CheckFriends`; **все** дочерние коллайдеры выключаются, все `Rigidbody` — кинематические; затем корневой `Rigidbody` динамический, корневой `CapsuleCollider` включается (в префабе он выключен, включает его код, `:120`).
- `Update()` (`:132-162`):
  - `health > 0` и (до игрока < 25 м **или** `isWorried`): если `isAgressive` — `Shout()`, `Roar()` (рёв один раз), `agent.speed = runSpeed`, цель агента = позиция игрока; ближе `attackDistance` — `animator.Play("Attack")`;
  - `health > 0`, игрок далеко и зомби не встревожен — `notRoared = true`, `Wandering()` (`:205-219`);
  - `health <= 0` — `Death()` (`:173-190`): включить все коллайдеры, ragdoll, выключить корневой коллайдер, `AIControl`, агента, аниматор, `Destroy(this)`.
- `FightHit()` (`:226-235`) вызывается событием анимации удара: если игрок ближе `attackDistance` — анимация камеры `CameraKick` и `PlayerStats.ApplyDamage(Random.Range(1, maxDamage))`.
- `ApplyHit(int damage)` (`:260-265`) — вход урона: `isWorried = true`, `Shout()`, `health -= damage`.

Значения в префабе: `health 45`, `maxDamage 30`, `isAgressive 1`, `attackDistance 2`, `runSpeed 5`, `wanderingSpeed 0.5`, `wanderingTimer 10`, `wanderingRadius 20`.

### 3.10. IK

- `IK.OnAnimatorIK()` (`IK.cs:22-44`): при `ikActive` и заданном `leftHandObj` ставит позицию и поворот левой руки с весом 1; иначе обнуляет веса. Работает только если у слоя аниматора включён IK Pass (в контроллере стрелка включён на обоих слоях).
- `AimIKLite` — см. 3.7. Цель задаёт `NPC.Update` (`NPC.cs:229-232`); обнуления цели в коде нет.
- При смерти оба компонента отключаются (`NPC.cs:461-462`).

---

## 4. Данные

### 4.1. Сериализованные поля `NPC`

| Поле | Код по умолчанию | Префаб | Смысл |
|---|---|---|---|
| `debug` | `true` | `0` | При `true` машина состояний **не выполняется** (`NPC.cs:226-227`) |
| `myFraction` | 0 | 0 | Номер своей фракции |
| `NPCNameInDatabase` | `"Assaulter"` | `AssaulterForest` | Ключ для `AssetsDatabase.FindNPC` при загрузке |
| `health` | 100 | 100 | Здоровье |
| `ammo` | 30 | 30 | Патроны до перезарядки |
| `damage` | 10 | 10 | Верхняя граница урона по игроку; полный урон по NPC |
| `shootingAccuracySpread` | 0.1 | 0.01 | Разброс луча |
| `fireMode` / `fireRate` | 3 / 0.5 | 1 / 0.07 | Перезаписываются случайными при `useRandomRateAndMode` |
| `useRandomRateAndMode` | `true` | 1 | См. 3.7 |
| `shotLayerMask` | — | Default, Weapon, Player, NPC | Во что может попасть выстрел |
| `defaultBehavior` | Idle | Wander | Режим покоя |
| `patrolPoints`, `patrolSpeed` | —, 0.5 | пусто, 0.5 | Patrol |
| `targetToFollow`, `followSpeed` | —, 0.5 | пусто, 0.5 | FollowToTarget |
| `radiusToTakeRandomPosition`, `wanderingSpeed` | 15, 0.5 | 15, 0.5 | Wander |
| `dropWeaponItem` | — | пусто | Оружие, выпадающее при смерти |
| `m_GroundCheckDistance` | 0.1 | 0.3 | Длина луча проверки земли |
| `friendsList`, `curretTarget`, `destinationReached` | — | — | Публичные рабочие поля, попадают в сериализацию |

Поля, которые объявлены, но **нигде не читаются** (проверено поиском по `Scripts`): `timeToCalmDown` (`:46`), `aimOffset` (`:53`), `projectile`, `projectilePoolSize` (`:69-70`), `DeadBody` (`:453`), `enemyTactics` (`:662`), `seekerStopingDistance` (`:682`), `turnSpeed` (`:277`), `chest` (`:85`, только присваивается). `lookPosition` (`:496`) только записывается; читает его лишь `SaveData`.

### 4.2. Прочие компоненты

- `PlayerHitTarget`: `npc`, `lootBox` — заполняются в `NPC.Start`; `isNPC` и `damageMultiplayer` нигде не читаются (в префабе у головы стоит `damageMultiplayer: 4`, у остальных 1).
- `CoverList`: `covers`; `maxCoverFindRange` (10) не используется.
- `AIControl`: `usedByZombie`, `target`.
- `AimIKLite`: `bones`, `aimTransform`, `axis`, `target`, `iterations`.
- `VisionUI`: статическое `visionAmount`, ссылки `image`, `eye`.

### 4.3. Теги и слои, от которых зависит код

Слои проекта (`ProjectSettings/TagManager.asset`): 20 `Weapon`, 21 `Player`, 22 `NPC`, 23 `Cover`, 24 `Target`, 25 `Dead`.

| Строка | Где используется |
|---|---|
| тег `MainCamera` | `NPCVision.cs:55`, `:105`; `NPC.cs:419` |
| теги `Wood`, `Concrete`, `Dirt`, `Metal`, `Flesh`, `Player` | выбор частиц попадания, `NPC.cs:919-947` |
| тег `Flesh` | проставляется при смерти (`NPC.cs:470`); по нему работает обыск (`PickupItem.cs:91`) |
| слой `Default` | коллайдеры трупа (`NPC.cs:469`) |
| слой `Dead` | корень трупа (`NPC.cs:488`); фильтр в зрении (`NPCVision.cs:183`) |
| имя объекта `Player` | `NPCVision.cs:54`, `NPC.cs:735`, `Grenade.cs:52`, `Rocket.cs:67` |
| имя объекта `Camera Holder` | `SaveData.cs:60`, `:482` |

### 4.4. Строки Animator

Стрелок (контроллер `ThirdPersonAnimatorController`; параметры: `Forward`, `Horizontal`, `Turn`, `Crouch`, `OnGround`, `Jump`, `JumpLeg`, `LookAtTarget`, `Armed`; слои: `Base Layer`, `Arms mask`; состояния: `Crouching`, `Airborne`, `Armed Strafe` в базовом и `Aim`, `Shot`, `Empty` в слое `Arms mask`):

| Вызов в коде | Есть в контроллере? |
|---|---|
| `SetBool("Crouch")` `NPC.cs:236,241` | да |
| `SetBool("LookAtTarget")` `:1086` | да |
| `SetFloat("Forward"/"Turn"/"Horizontal"/"Jump"/"JumpLeg")`, `SetBool("OnGround")` `:1149-1167` | да |
| `Play("Shot", 1)` `:331` | да, состояние `Shot` в слое 1 |
| `SetLayerWeight(1, …)` `:550,571` | слой 1 = `Arms mask` |
| `Play("Reload")` `:355` | **состояния `Reload` нет** |
| `IsName("Grounded")` `:1197` | **состояния `Grounded` нет** (ветка прыжка всё равно не вызывается: `jump` всегда `false`) |
| параметр `Armed` | в коде NPC не выставляется |

Зомби (контроллер `ThirdPersonAnimatorControllerZombie`; параметры `Forward`, `Turn`, `Crouch`, `OnGround`, `Jump`, `JumpLeg`; слои `Base Layer`, `Attack`): `Play("Attack")` (`ZombieNPC.cs:147`) — есть; `IsName("Grounded")` (`:413`) — есть. На камере игрока: `Play("CameraKick")` (`:232`) — состояние есть в `Camera & Recoil.controller`.

Имена методов, вызываемых по строке: `Invoke("Shot")`, `Invoke("AddAmmo")`, `Invoke("GoToIdle")`, `CancelInvoke("GoToIdle")`; Animation Events `FightHit` (зомби) и `PlayFootstep` (`FootstepSound`).

---

## 5. Связи с другими системами

### 5.1. Кто вызывает NPC-систему

| Вызывающий | Что вызывает | Примечание |
|---|---|---|
| `Weapon.ApplyHit` `Weapon.cs:610-611` | `PlayerHitTarget.npc.GetHit(calculatedDamage, transform)` | Пуля игрока (через `BalisticProjectile.cs:38`) |
| `Weapon.ApplyHit` `Weapon.cs:613-614` | `ZombieNPC.ApplyHit(Random.Range(damageMin, damageMax))` | |
| `Weapon.MeleeHit` `Weapon.cs:520-524` | `ZombieNPC.ApplyHit(meleeDamagePoints)`, `PlayerHitTarget.npc.GetHit(...)` | Ближний бой |
| `Weapon` (выстрел) `Weapon.cs:453-454` | `CoverList.NotifyNPCAboutShot(pos, weaponShotNPCDetectionDistance)` | Дистанция из `WeaponSettingSO.cs:53` (по умолчанию 150; в ассетах встречаются 0, 150, 500) |
| `Grenade.Explosion` `Grenade.cs:50-58` | `NPC.GetHit((int)damage, Player)`, `ZombieNPC.ApplyHit((int)damage)` | Ищет компонент на самом коллайдере |
| `Rocket.Explosion` `Rocket.cs:65-73` | то же | |
| `PickupItem` `PickupItem.cs:91-97` | читает `PlayerHitTarget.lootBox` | Обыск тела |
| `SaveData` `SaveData.cs:42-43, 158-198, 487-522` | читает/пишет поля, уничтожает и создаёт NPC | См. раздел 6 |
| `AssetsDatabase.FindNPC` `AssetsDatabase.cs:31-42` | читает `NPC.NPCNameInDatabase` | |
| Animation Event в `zombie attack.fbx` | `ZombieNPC.FightHit()` | |

`NPCVision.TriggerPlayerShotVisibility()` (`NPCVision.cs:253-261`) и `ZombieNPC.IsVisible()` (`ZombieNPC.cs:221-224`) вызывающих в C# не имеют (подписка в `Weapon.cs:219` закомментирована).

### 5.2. Кого вызывает NPC-система

| Откуда | Что | Зачем |
|---|---|---|
| `NPC.ApplyHit` `NPC.cs:421` | `PlayerStats.ApplyDamage(int)` | Урон игроку от выстрела |
| `ZombieNPC.FightHit` `ZombieNPC.cs:233` | `PlayerStats.ApplyDamage(int)` | Урон игроку от удара |
| `NPCVision` `:122, :194` | `PlayerStats.isPlayerDead` (static) | Не целиться в мёртвого |
| `NPCVision` `:62, :71` | `FPSController.CheckMovement()`, `FPSController.crouch` (static) | Слух и скрытность |
| `NPC.FollowToTargetBehavior` `NPC.cs:737` | `InputManager.IsRunning()` | Скорость следования |
| `NPC` `:914-960` | `HitFXManager.ricochetSounds`, `obj*HitFX` | Звук и частицы попадания |
| `NPC.Start` `:201` | `DTInventory.LootBox` | Лут |
| `NPCVision` `:84-86` | `VisionUI.visionAmount` (static) | HUD |
| `ZombieNPC` `:93, :232` | `Camera.main` → `Animator.Play("CameraKick")` | Тряска камеры |
| `FootstepSound` (режим `usedByNPC`) | `NavMeshAgent.desiredVelocity`, `GetCollisionTag.contactTag` | Шаги (`FootstepSound.cs:36-40, 69-93`) |

`ObjectHealth` и `WeaponManager` с NPC-системой напрямую не связаны (вызовов не найдено).

---

## 6. Сохранение (`SaveData.cs`)

Файл: `Application.persistentDataPath/<имя сцены>_charactersData`, JSON через `JsonUtility` (`SaveData.cs:197-198`). Класс `CharactersData` (`:669-684`):

| Поле | Что пишется | Читается при загрузке |
|---|---|---|
| `npcName[]` | `NPC.NPCNameInDatabase` (`:170`) | да — ключ для `AssetsDatabase.FindNPC` (`:504`) |
| `npcPos[]`, `npcRot[]` | позиция и поворот корня (`:171-172`) | да; позиция ставится на 1 м выше (`:506-507`) |
| `npcLookAtTarget[]` | `NPC.lookPosition` (`:177`) | да (`:509`), но в логике NPC поле не читается |
| `npcCurrentTarget[]` | массив создаётся, но не заполняется (`:165`, запись закомментирована `:174-175`) | нет (`:508` закомментирована) |
| `zombiePos[]`, `zombieRot[]` | позиция и поворот (`:191-192`) | да (`:518-519`) |
| `zombieIsWorried[]` | `ZombieNPC.isWorried` (`:193`) | да (`:520`) |

Идентификация: стрелок — **только по строке `NPCNameInDatabase`** (тип префаба, а не конкретный экземпляр); зомби — без идентификатора, всегда один префаб `AssetsDatabase.zombie` (`AssetsDatabase.cs:44-47`).

Порядок загрузки (`:487-522`): все `NPC` и `ZombieNPC` сцены уничтожаются, затем для каждой записи создаётся новый экземпляр префаба; на время переноса `NavMeshAgent` выключается. После `Load()` статический список `CoverList.sceneActiveNPC` пересоздаётся пустым (`:42-43`) — новые NPC добавят себя в своём `Start`.

Что **не** сохраняется (в `CharactersData` таких полей нет): здоровье, патроны, признак смерти, текущая цель, занятое укрытие, состояние машины, фракция, `defaultBehavior`, `patrolPoints`, `targetToFollow` и любые другие значения, переопределённые у экземпляра в сцене.

`LootBox` тел сохраняется общим механизмом лутбоксов — по **имени объекта** (`:264-279`, `:616-648`).

---

## 7. Контракт: что нельзя менять без проверки соседей

1. Публичные методы: `NPC.GetHit(int, Transform)`, `NPC.ListenToPlayerShot()`, `ZombieNPC.ApplyHit(int)`, `ZombieNPC.FightHit()` (имя зашито в Animation Event), `CoverList.NotifyNPCAboutShot(Vector3, float)`, `CoverList.FindClosestCover`, `NPC.Move`/`ZombieNPC.Move(Vector3, bool, bool)` (их зовёт `AIControl`).
2. Имена методов для `Invoke`: `Shot`, `AddAmmo`, `GoToIdle`.
3. Статическое поле `CoverList.sceneActiveNPC` — его пересоздаёт `SaveData`.
4. Поля, читаемые снаружи: `NPC.NPCNameInDatabase`, `NPC.lookPosition`, `ZombieNPC.isWorried`, `PlayerHitTarget.npc`, `PlayerHitTarget.lootBox`, `NPC.myFraction`, `VisionUI.visionAmount`.
5. Сериализованные имена полей всех компонентов (переименование без `[FormerlySerializedAs]` обнулит значения в префабах и сцене), включая опечатки `curretTarget`, `damageMultiplayer`, `seekerStopingDistance`.
6. Класс `CharactersData` и имя файла `<сцена>_charactersData` — формат сейва.
7. Значение `NPCNameInDatabase` в префабе должно совпадать с записью в `AssetsDatabase.NPCs`.
8. Строки тегов, слоёв, имён объектов (`Player`, `Camera Holder`, `MainCamera`) и Animator из раздела 4.
9. Структура префаба стрелка: дочерние `NPCVision` и `FirePointNPC`, `PlayerHitTarget` на костях с коллайдерами и тегом `Flesh`, `LootBox` на корне.
10. Тег `MainCamera` и коллайдер на объекте камеры игрока — и зрение, и урон по игроку опираются на них.
11. В сцене с NPC обязателен объект с `CoverList`: без него `NPC.Start` упадёт на `CoverList.sceneActiveNPC.Add(this)` (`NPC.cs:204`), потому что статический список создаётся только в `CoverList.Awake`.
12. GUID скриптов (`.cs.meta`) — на них ссылаются префабы.

---

## 8. Наблюдения и возможные проблемы

### 8.1. Подтверждено кодом (следует из текста исходников)

1. **Успокоение не реализовано.** `curretTarget` нигде не присваивается `null` (присваивания только в `NPC.cs:536` и `:795`), поэтому условие возврата в покой (`:514`) срабатывает лишь если объект цели уничтожен. Поле `timeToCalmDown` не читается, а `calmDownTimer` (`:100`) никогда не задаётся и равен 0.
2. **`playerDetected` никогда не сбрасывается в `false`** (присваивания: `NPCVision.cs:66, 124, 259`; `NPC.cs:389, 770, 793`). После первого обнаружения, выстрела в зоне слышимости или попадания таймер узнавания для этого NPC больше не действует.
3. **`SelectBestTarget` игнорирует параметр и `potentialTargets`.** Список `visibleTargets` никогда не `null`, поэтому метод всегда возвращается из первой ветки (`NPCVision.cs:204-224`); код работы с `potentialTargets` (`:226-250`) недостижим. Кроме того, `potentialTargets` пересоздаётся при каждом скане (`:93`).
4. **Тактики `EnemyTactics` (Tactical/Seeker/Idle) не реализованы**: enum и поле объявлены (`NPC.cs:651, 662`), но нигде не читаются.
5. **`PlayerHitTarget.damageMultiplayer` не применяется** — попадание в голову наносит тот же урон (`Weapon.cs:610-611`).
6. **Слух не учитывает `playerIsFriendly` и смерть игрока**: ветка `NPCVision.cs:62-67` добавляет игрока в `visibleTargets` без этих проверок, а `NPC.Attack()` стреляет по любой видимой цели (`NPC.cs:269-275`).
7. **При `debug = true` ИИ выключен, но стрельба работает**: `TargetMachine()` не вызывается (`NPC.cs:226`), `Attack()` вызывается всегда (`:266`). Значение по умолчанию у нового компонента — `true`.
8. **Перезарядка вызывается каждый кадр**: пока `ammo <= 0` или `isReloading`, `Fire()` каждый кадр вызывает `Reload()` → `animator.Play("Reload")` и новый `Invoke("AddAmmo", 3f)` (`NPC.cs:313-317, 352-357`). После первой перезарядки отложенные `AddAmmo` продолжают срабатывать ещё около 3 с, каждый раз возвращая `ammo = 30`.
9. **`fireMode` 3 при случайном режиме недостижим**: `Random.Range(1, 3)` для `int` даёт 1 или 2 (`NPC.cs:321`). `fireMode = 0` (допускается `[Range(0, 3)]`) не стреляет вовсе — в `switch` нет такой ветки.
10. **В кадре смерти логика продолжает выполняться**: после `Death()` в том же `Update` идут `TargetMachine()` и `Attack()` (`NPC.cs:218-227, 266`). `TargetMachine` при `cover == null` и известной цели снова занимает укрытие (`:573-579`), и освободить его уже некому; `Fire()` может запланировать `Shot` уже после `CancelInvoke()`.
11. **`GetRandomPositonOutsidePlayerFOV` делает одну попытку, а не десять**: случайная точка вычисляется до цикла (`NPC.cs:850-851`), все 10 итераций проверяют её же.
12. **`GetRandomPosition` при неудаче возвращает `transform.forward`** — вектор направления, используемый как мировая позиция (`NPC.cs:1052`). Аналогично `ZombieNPC.RandomNavSphere` возвращает `Vector3.zero` (`ZombieNPC.cs:197`).
13. **Список друзей зомби всегда `null`**: `friendsList` — приватное несериализуемое поле без инициализации (`ZombieNPC.cs:42`); `GetFriendsAround` и `Shout` защищены проверкой на `null` и ничего не делают (`:238-249, 267-289`). Зомби не оповещают друг друга.
14. **`isWorried` у зомби не сбрасывается** (`ZombieNPC.cs:245, 262`) — встревоженный зомби преследует игрока на любом расстоянии. Проверки `PlayerStats.isPlayerDead` в `ZombieNPC` нет.
15. **Неагрессивный зомби рядом с игроком бездействует**: при `isAgressive == false` и дистанции < 25 м не выполняется ни погоня, ни блуждание (`ZombieNPC.cs:136-150`).
16. **`FightHit`: условие по углу относится только к звуку** (нет фигурных скобок, `ZombieNPC.cs:230-233`) и сравнивает угол между векторами позиций от начала координат, а не направление взгляда. Урон наносится независимо от него.
17. **Опечатка в фильтре мёртвых**: проверяется `potentialTargets.Contains`, а удаление идёт из `visibleTargets` (`NPCVision.cs:187-188`).
18. **Изменение списка внутри `foreach`**: `objectsInRange.Remove(obj)` в цикле по `objectsInRange`, после чего `obj` используется дальше (`NPCVision.cs:100-105`). Сработает исключением, если в списке окажется `null`.
19. **`CoverList.NotifyNPCAboutShot` обращается к `npc.transform` до проверки на `null`** и печатает строку в консоль на каждого NPC при каждом выстреле (`CoverList.cs:25-27`). NPC, уничтоженный из-за отсутствия NavMesh (`NPC.cs:211-214`), из списка не удаляется (удаление только в `Death`, `:457`). Вызов стоит в `Weapon.cs:454` перед установкой отдачи и `nextFireTime` (`:456-459`).
20. **`AIControl` глушит все исключения** пустым `catch` (`AIControl.cs:45-69`), включая ошибки внутри `Move()`.
21. **NPC добавляет в друзья сам себя**: сравнение `_collider.gameObject != this.gameObject` (`NPC.cs:903`) сравнивает кость с корнем и всегда истинно.
22. **Обыск не проверяет, что NPC мёртв** (`PickupItem.cs:91-97`): достаточно тега `Flesh` и `PlayerHitTarget`, а в префабе кости имеют этот тег изначально.
23. **На каждого стрелка `HitFXManager` создаёт пул декалей** (4 типа × `decalsPoolSizeForEachType`, в префабе 20), хотя `NPC.DecalManager` нигде не вызывается (вызов закомментирован, `NPC.cs:444-448`).
24. **Сейв не хранит здоровье и смерть** (раздел 6): после загрузки все NPC создаются из префаба с исходными значениями; настройки экземпляров сцены теряются.
25. **В `FindClosestCover`** проверка `enemyPos == null` для структуры всегда ложна; `maxCoverFindRange` не используется; сравнение идёт по именам коллайдеров (`CoverList.cs:42, 57`).
26. **Несоответствие строк Animator**: `Play("Reload")` и `IsName("Grounded")` обращаются к состояниям, которых нет в контроллере стрелка (раздел 4.4).
27. **Ссылка `DeadBody` в префабе указывает на GUID `5540305352069d8448c9d6356978443c`, которого нет в проекте.** Поле в коде не используется.
28. **Старт без проверок на `null`**: если в сцене нет `Player`, `PlayerStats`, `CoverList` или у NPC нет дочернего `FirePointNPC`, будет исключение — `NPC.Start`, `NPCVision.Start` и `ZombieNPC.OnEnable` разыменовывают результаты поиска без проверок (`NPC.cs:153, 204`; `NPCVision.cs:54-55`; `ZombieNPC.cs:90, 93`).

### 8.2. Гипотезы — требуют проверки в Unity

Это выводы из сочетания кода и данных префабов; значения в сцене могут отличаться.

1. **Взрывы не ранят стрелка.** `Grenade`/`Rocket` ищут `NPC` на самом коллайдере (`Grenade.cs:50`, `Rocket.cs:65`), а в `AssaulterForest.prefab` у корня с `NPC` коллайдера нет — коллайдеры стоят на костях. Зомби это не касается (корневая капсула).
2. **NPC не видят друг друга как врагов.** Ветка `NPCVision.cs:149` тоже требует `NPC` на объекте коллайдера, а маска `layerMaskForOverlap` в префабе не включает слой `Default`, на котором кости. Тогда не работают межфракционные бои, урон NPC→NPC (`NPC.cs:432-438`) и фильтр слоя `Dead`.
3. **Вблизи зрение может не «дозревать».** Таймер после превышения `timeToDetect + 0.1` сбрасывается ровно в `timeToDetect` (`NPCVision.cs:76`), а условие обнаружения строгое `>` (`:120`). Если прирост за кадр больше 0.1 (стоя: дистанция < 400·Δt, то есть ≈6.7 м при 60 fps и ≈13 м при 30 fps), таймер в момент скана всегда равен порогу. На практике рядом обычно срабатывает слух, поэтому эффект возможен для неподвижного или крадущегося игрока.
4. **Раненый из засады NPC не реагирует поворотом.** `GetHit` ставит `lookAtTarget` и `lookPosition`, но `lookPosition` не читается, `curretTarget` не задаётся, а в покое `lookAtTarget` сбрасывается (`NPC.cs:548`).
5. **В состоянии ПОИСК NPC может застыть**: дойдя до позиции цели и не увидев её, он остаётся с `lookingForTarget = true` — основной блок не выполняется (`NPC.cs:543`), укрытия нет, выхода из состояния кроме «снова увидел цель» нет.
6. **Ближе 10 м к цели укрытие занимается и освобождается каждый кадр** (`NPC.cs:573-605`), из-за чего `desiredPosition` может оставаться точкой укрытия, а приседание — сбрасываться.
7. **После загрузки мёртвые стрелки оживают.** `Death()` лишь отключает компонент; если `FindObjectsOfType<NPC>()` возвращает отключённые компоненты на активных объектах, труп попадёт в сейв и будет создан заново живым. Мёртвые зомби (`Destroy(this)`) в сейв не попадают, и тело после загрузки исчезнет.
8. **Лут тел после загрузки**: лутбоксы сопоставляются по имени объекта, а новые NPC получают имя `AssaulterForest(Clone)` — содержимое может не восстановиться либо задвоиться между одноимёнными.
9. **Корневой `Rigidbody` трупа** остаётся динамическим без собственного коллайдера, при выключенных агенте и аниматоре — проверить, не «проваливается» ли корень (и висящие на нём `NPC Vision`, звук) под уровень.
10. **Корутина `CheckFriends` и `NPCVision` продолжают работать у мёртвых** (отключение компонента корутину не останавливает; `NPCVision` — отдельный компонент) — возможная лишняя нагрузка.
11. **`Animator.Play("Reload")` каждый кадр** при отсутствии состояния может засыпать консоль предупреждениями.
12. **Некрупные `Cover` с не-триггерным `BoxCollider`** могут физически мешать персонажам — зависит от матрицы столкновений слоёв (не проверялась).
13. **`VisionUI`** общий на всех NPC (static): поведение индикатора при нескольких наблюдателях и после смерти наблюдателя стоит посмотреть вживую.

---

## 9. Тест-план в Unity

Перед началом: открыть `Demo Zone`, очистить Console, включить Gizmos. Для каждого шага записывать сообщения Console.

| № | Шаги | Ожидаемый результат (по коду) |
|---|---|---|
| 1 | Запустить Play, найти `AssaulterForest`, наблюдать издалека | NPC не удалён (стоит на NavMesh), бродит (Wander), слой `Arms mask` расслаблен |
| 2 | Медленно выйти в поле зрения на 30–40 м и стоять | Индикатор-глаз заполняется ~за несколько секунд, затем NPC поворачивается и стреляет; соседи тоже получают цель |
| 3 | То же, но в приседе | Заполнение примерно вдвое медленнее |
| 4 | Подойти со спины ближе 10 м шагом, затем повторить в приседе | Шагом — обнаружение по слуху; в приседе — нет (проверка гипотезы 8.2.3: встать неподвижно вплотную в поле зрения) |
| 5 | Выстрелить вдали от NPC, не показываясь, затем выйти в поле зрения | Обнаружение мгновенное, без заполнения индикатора; в Console строки `Notify npc call…` |
| 6 | В бою отойти за преграду | NPC идёт к `Cover`, приседает; через 4–10 с выходит к последней позиции игрока (`ExitCoverToCheckTarget <имя>` в Console) |
| 7 | После шага 6 не показываться 30+ с | Проверка 8.1.1 и 8.2.5: вернётся ли NPC к блужданию или застынет |
| 8 | Подойти к NPC ближе 10 м | Встаёт, покидает укрытие (проверка 8.2.6) |
| 9 | Дать NPC расстрелять магазин | Пауза ~3 с; проверить Console на предупреждения Animator (8.2.11) |
| 10 | Стоять под огнём | Здоровье падает на 5–9 за попадание, звук боли, экранный эффект; при 0 — смерть игрока, NPC прекращает огонь |
| 11 | Убить NPC выстрелами в корпус и в голову, считая попадания | Одинаковое число (8.1.5); ragdoll; в кадре смерти нет лишнего выстрела (8.1.10) |
| 12 | Нажать клавишу подбора, глядя на труп; затем на живого NPC | Открывается окно лута; проверить 8.1.22 |
| 13 | Убить NPC, сидевшего в укрытии; привести к тому же укрытию другого | Проверка 8.1.10: остаётся ли `Cover.occupied = true` (смотреть в Inspector) |
| 14 | Бросить гранату / выстрелить из РПГ рядом со стрелком и рядом с зомби | Зомби получает урон; стрелок — проверить гипотезу 8.2.1 |
| 15 | Подойти к зомби ближе 25 м | Рёв один раз, бежит, бьёт на 2 м, камера дёргается, урон 1–29 |
| 16 | Ранить одного зомби из группы издалека | Он бежит к игроку; соседи — проверить 8.1.13 |
| 17 | Убежать от встревоженного зомби дальше 25 м | Продолжает погоню (8.1.14) |
| 18 | Убить одного стрелка и одного зомби, сохранить, загрузить | Проверка 8.2.7/8.2.8: кто появился, с каким здоровьем, что в луте |
| 19 | Сохранить в разгар боя, загрузить | NPC на местах (на 1 м выше, затем приземляются), без цели; игра без исключений |
| 20 | Поставить в тестовой сцене NPC с `myFraction 0 / targetFraction 1` и второго наоборот | Проверка 8.2.2: воюют ли они между собой |
| 21 | Создать пустую сцену без `NPC&CoverList`, поставить стрелка | Ожидается `NullReferenceException` в `NPC.Start` (7.11) |

---

## 10. Модульная граница

**Можно менять изолированно (внутри `Scripts/NPC`, без правок соседей), сохраняя контракт раздела 7:**

- внутреннюю логику `TargetMachine`, `TacticalBehavior`, подрежимов покоя, таймеры;
- алгоритм зрения внутри `NPCVision.Update` и `SelectBestTarget` — при неизменных публичных полях и сигнатуре;
- логику `Fire`/`Reload`/`Shot` — при сохранении имён методов для `Invoke`;
- алгоритм `CoverList.FindClosestCover` и содержимое `Cover`;
- `AimIKLite`, `IK`, `DrawRay`, `VisionUI` (кроме статического `visionAmount`);
- внутреннюю логику `ZombieNPC` (погоня, блуждание, друзья);
- удаление неиспользуемых приватных полей и методов (`chest`, `GetRandomTimeForAction`, `CalculateRelativeDirection`).

**Требует согласованной правки с соседями:**

| Изменение | Кого затрагивает |
|---|---|
| Сигнатуры `GetHit` / `ApplyHit`, единый интерфейс урона, множитель по частям тела | `Weapon.cs`, `Grenade.cs`, `Rocket.cs` (S03) |
| Способ оповещения о выстреле, вынос `sceneActiveNPC` из `CoverList` | `Weapon.cs`, `SaveData.cs` |
| Сохранение здоровья/смерти/цели, идентификация экземпляров | `SaveData.cs`, `CharactersData`, `AssetsDatabase` (S07) — с учётом совместимости старых сейвов |
| Признак «игрок» (тег `MainCamera`, имя `Player`) | `GamePrefab.prefab`, `FPSController`, `PlayerStats` (S01/S02) |
| Имена параметров/состояний Animator, Animation Events | контроллеры и клипы (S08) |
| Переименование сериализованных полей, перенос компонентов в иерархии | префабы `AssaulterForest`, `Zombie`, `Cover`, `NPC&CoverList`, сцена |
| Лут с тел | `PickupItem.cs`, `LootBox`, `DTInventory` (S05) |
| Шаги и эффекты попаданий NPC | `FootstepSound`, `HitFXManager` (S09 / S03) |

Рекомендуемый порядок: сначала пройти тест-план (особенно шаги 7, 11, 13, 14, 18, 20), чтобы перевести гипотезы 8.2 в «подтверждено» или «снято», и только потом планировать правки — по одной изолированной теме за раз.
