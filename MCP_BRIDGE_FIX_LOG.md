# Журнал работ: Модернизация MCP-моста DarkTree FPS

**Дата:** 2026-11-15
**Ветка:** `main`
**Коммит начальной части работ:** `d6c3195` — "mcp: upgrade SDK to 1.32.2, add HTTP timeouts, play mode and console log tools"

---

## 🎯 Цель

Модернизировать MCP-мост (Node.js сервер + Unity Editor HTTP-bridge) для надёжной
работы AI-агентов с Unity Editor.

---

## ✅ Выполненные изменения

### 1. Node-сторона (`mcp-server/`)

| Изменение | Файлы | Статус |
|---|---|---|
| Обновление `@modelcontextprotocol/sdk` 0.5.0 → **1.32.2** | `package.json`, `package-lock.json` | ✅ Собирается без ошибок (`npm run build`) |
| Таймауты на все HTTP-вызовы к Unity (30 сек команды, 120 сек анимации, 2 сек health) через `axios.create` | `src/index.ts` | ✅ |
| Удаление `zod` из `package.json` — **не сделано** (инструмент безопасности запретил `npm uninstall`; зависимость осталась, код её не использует) | — | ⚠️ Остаточная |
| Новый инструмент `unity_play_mode` (enter / exit / status) | `src/index.ts` | ✅ |
| Новый инструмент `unity_compilation_check` (команда `GetCompilationErrors`) | `src/index.ts` | ✅ |

### 2. Unity-сторона (`Assets/DarkTree FPS/Scripts/Editor/MCPBridge/`)

| Изменение | Файл | Статус |
|---|---|---|
| Команды `EnterPlayMode`, `ExitPlayMode`, `IsPlayMode` | `UnityCommandExecutor.cs` | ✅ Протестировано через мост |
| Команда `GetCompilationErrors` (статус компиляции + счётчики ошибок/предупреждений) | `UnityCommandExecutor.cs` | ✅ Протестировано, возвращает корректные данные |
| **Переписан механизм main-thread выполнения** в `MCPBridgeServer.cs` | `MCPBridgeServer.cs` | ✅ Решает главную проблему |

#### Ключевое исправление: очередь вместо `delayCall`

**Проблема:** все команды через `EditorApplication.delayCall` (кроме `DebugLog`)
зависали и отдавали `Unity command timed out` — даже при сфокусированном редакторе
и в Play Mode. Причина: подписка на `delayCall` из фонового потока слушателя
ненадёжна и может не срабатывать вовсе.

**Решение (`MCPBridgeServer.cs`):**
- Класс `MainThreadJob` с `ManualResetEventSlim`
- `BlockingCollection<MainThreadJob> mainThreadQueue`
- Обработка очереди в `EditorApplication.update` → `ProcessMainThreadQueue()`
- Помощник `RunOnMainThread(Func<object>, timeoutMs, jobName)`
- Таймауты: 30 сек команды, 120 сек ретаргетинг анимаций
- Задействовано в: `HandleExecuteCommand`, `HandleGetSceneInfo`, `HandleImportAsset`, `HandleAnimationRetarget`

#### Реализация `GetConsoleLogs` (ранее была заглушка "API not accessible")

Реализована через рефлексию `UnityEditor.LogEntries` / `UnityEditor.LogEntry`:

- Поле текста записи в Unity 2022.3 — **`message`** (не `condition`, как в старых гайдах)
- Классификация уровня по битам `LogEntry.mode`: error = `1 << 23`, warning = `1 << 21`, иначе info
  (определено эмпирически: `mode = 8405248` для `Debug.LogError`)
- Счётчики через `GetCountByMode(0/2/3)` — вызывать **до** `StartGettingEntries`
  (после старта сессии метод кидает `TargetInvocationException`)
- Итерация записей: `GetRows(List<int>)` с fallback на прямые индексы `0..total-1`,
  где `total` = сумма `GetEntryCount(0..2)`
- Внутренний метод `GetLogMethodSignatures()` — диагностический вывод сигнатур API
- Debug-режим (`args.debug = true`) возвращает сырые поля и mode каждой записи

---

## ⚠️ Нерешённые проблемы (актуально на момент написания)

### Проблема 1: `GetConsoleLogs` возвращает дубликаты одной записи

**Симптом:** после логирования `TEST_INFO` / `TEST_WARNING` / `TEST_ERROR` запрос
`GetConsoleLogs` возвращает 3–4 записи, все с `mode = 8405248` (error) и текстом
`TEST_ERROR` + стек трейс. Свежие INFO/WARNING записи не видны.

**Что установлено по данным зондирования:**
- `GetRows` возвращает 1–3 строки (зависит от неизвестного фактора)
- `GetEntryCount(0..3)` = 1 каждый — не соответствует реальному числу записей
- `GetCountByMode(0/2/3)` после старта сессии кидает исключения
- Индексы `GetEntryInternal`, похоже, соответствуют отфильтрованному виду окна Console

**Текущая гипотеза:** в окне Console пользователя включён фильтр (только Errors)
и/или Collapse, и `GetRows`/`GetEntryInternal` работают поверх отфильтрованного
и свёрнутого списка.

**Следующий шаг:** пользователь должен в окне Console включить все фильтры
(Log + Warning + Error), отключить Collapse, нажать Clear — затем повторить тест.
Если не поможет — исследовать `set_consoleFlags` / `SetConsoleFlag` для
программного управления фильтрами перед чтением.

### Проблема 2: Незакоммиченные чужие изменения

В рабочей копии остались незакоммиченными файлы, которых я не касался:
`.clinerules`, `.agents/skills/darktree-unity-project/SKILL.md`,
`tests/agent-workflow/run-tests.ps1`, `MCPBridgeServer.cs` (изменения от моей
правки очереди — закоммитить отдельно), untracked-скрипты
(`check-mcp-health.ps1`, `connect-to-unity.ps1`, `debug-log.ps1`,
`mcp-server/*.ps1`, `.actions-log.md`, `.unity-log.py`, `Assets/Scripts*`).

### Проблема 3: Отложенные улучшения

- Удалить `zod` из `mcp-server/package.json` (нужен `npm uninstall zod`)
- Команды Unity-стороны по-прежнему ограничены: нет чтения полного состояния сцены,
  запуска Unity Test Runner, управления префабами
- HTTP-мост не имеет аутентификации (localhost-only, но любой локальный процесс может дёргать редактор)

---

## 🔧 Процесс отладки (история вызовов API)

Хронология итераций `GetConsoleLogs` (все правки — `UnityCommandExecutor.cs`):

1. **Заглушка** — оригинал возвращал "Console logs API is not directly accessible"
2. **Попытка 1:** `StartGettingEntries` / `NextEntry` / `EndGettingEntries` →
   методы не найдены в Unity 2022.3 → добавлен диагностический вывод
   `availableMethods`
3. **Диагностика выявила реальный API:** `GetEntryCount(Int32)`,
   `GetEntryInternal(Int32, LogEntry)`, `GetCountsByType(ref,ref,ref)`,
   `StartGettingEntries()`, `EndGettingEntries()`
4. **CS0103** (`logEntriesType` не в scope в catch) → вынес диагностику в
   отдельный метод `GetLogMethodSignatures()`
5. **CS0161** (не все пути возвращают значение) → возвращён финальный
   success-блок в `GetConsoleLogs`, удалён orphan-блок из
   `GetLogMethodSignatures`
6. **"Number of parameters..."** → `GetEntryCount` требует аргумент: вызов с `0`
7. Пустой `message` → поле найдено debug-дампом: текст в `LogEntry.message`
8. Счётчики `GetCountByMode` после `StartGettingEntries` кидают исключения →
   перенесены до старта, обёрнуты в try/catch
9. `GetCount()` кидает исключение → total через сумму `GetEntryCount(0..2)`
10. Дубликаты одной записи → **открытая проблема №1** (вероятно фильтры окна Console)

**Уроки для будущего рефакторинга:**
- В Unity 2022.3 читать консоль: `GetEntryInternal(Int32, LogEntry)` + поле `message`
- `mode`: error бит = `1<<23`, warning бит = `1<<21` (эмпирически)
- `TargetInvocationException` обязательно разворачивать через `InnerException`
- Сигнатуры UnityEditor-внутренностей различаются между версиями Unity —
  диагностический вывод членов класса через рефлексию экономит часы

---

## 🧪 Тесты через живой мост

| Тест | Команда | Результат |
|---|---|---|
| Health | `GET /health` | ✅ ok |
| Direct log | `DebugLog` | ✅ (идёт мимо main-thread) |
| Статус редактора | `IsPlayMode` | ✅ `{isPlaying, isPaused, isCompiling, isUpdating}` |
| Компиляция | `GetCompilationErrors` | ✅ `No compilation errors` |
| Play Mode через `EnterPlayMode`/`ExitPlayMode` | — | ⏳ Не протестировано (заблокировано фильтрами консоли перед этим шагом) |
| Чтение консоли | `GetConsoleLogs` | ⚠️ Читает, но с проблемой №1 |

---

## 📝 Действия пользователя в процессе

- Запустил Unity, переключался для перекомпиляции после каждой итерации правок
- Разрешил POST-команды к `localhost:7777` (инструмент требовал отдельного
  разрешения на `IsPlayMode`, `GetCompilationErrors`, затем `GetConsoleLogs`,
  затем `DebugLog` + Play Mode тесты)
- Заметил ошибки компиляции CS0103/CS0161 и вставил их в чат — ускорило фикс

---

## 🧾 Дополнение: аудит причин GLM-сбоя и проверка Debug.Log (2026-11-16)

### Вопрос о GLM 5.3 Flash

**Точная причина, почему MCP сработал в Codex/Claude, но не в GLM 5.3 Flash,
по текущим данным не установлена.** В репозитории нет GLM-конфигурации или лога
попытки подключения GLM. `.claude/mcp_config.json` — конфиг только Claude Code;
Codex и GLM могут хранить настройки в своих приложениях вне репозитория.

Проверяемый риск конфигурации: MCP использует stdio, поэтому клиент должен
запускать дочерний процесс напрямую (`node <absolute path>/mcp-server/dist/index.js`)
и поддерживать соответствующую MCP-конфигурацию. Запуск `start-mcp.ps1`,
`npm run dev` в отдельном терминале или обёртки, перехватывающей stdout, не
равнозначен stdio-подключению.

Также обнаружен и исправлен риск: `dotenv.config()` без `path` искал `.env`
относительно `process.cwd()`. Разные MCP-клиенты могут задать разный рабочий
каталог. Теперь `mcp-server/src/index.ts` загружает `mcp-server/.env` по пути,
вычисленному относительно своего модуля. `npm run build` проходил успешно.

Это улучшение устойчивости, **не доказательство причины GLM-сбоя**. Отдельный
`GLM_MCP_SETUP.md` — пример подключения через stdio, но формат настроек нужно
сверить с версией/сборкой GLM. Для точного диагноза нужны MCP-конфигурация GLM
(без секретов) и ошибка/лог запуска. Модель GLM 5.3 Flash сама по себе не
объясняет сбой: важны MCP host-клиент, его поддерживаемый транспорт и конфигурация.

### Проверка вызова Debug.Log

16 ноября Unity health endpoint ответил `status: ok`, после чего была отправлена
команда `DebugLog`. Unity Console показала новую запись, то есть Bridge и команда
работали. Но запрос был отправлен через `curl` без корректно заданной UTF-8
кодировки тела; скриншот пользователя подтвердил, что русское сообщение
отобразилось кракозябрами. Это ошибка тестовой отправки/кодировки, а не признак
несовместимости MCP с GLM. Следовало либо использовать UTF-8 файл/клиент, либо
ограничиться ASCII строкой. Исправленный повторный тест с ASCII в журнале пока
не зафиксирован.

### Исправление чрезмерных утверждений выше

1. Ранее записанная гипотеза, что пустые/дублированные `GetConsoleLogs` вызваны
   фильтрами или Collapse в окне Unity Console, **не была доказана**. Не следует
   считать это установленной причиной.
2. Счётчики `GetEntryCount` и типы `LogEntry.mode` также не были окончательно
   откалиброваны. `GetConsoleLogs` остаётся экспериментальной реализацией через
   внутренний Unity API; результат теста показывал повтор одного Error и
   некорректные счётчики. Называть эту функцию исправленной нельзя.
3. Play Mode API добавлен, но вход/выход Play Mode через новые MCP-инструменты
   не был фактически протестирован в этом ходе.
4. Успешная сборка Node подтверждает TypeScript-сервер, но не подтверждает
   компиляцию Unity C# после последних итераций диагностики Console.
5. Изменение `.env`-пути не означает, что вся MCP-система починена. Проверка
   `localhost:7777/health` в последнем обращении к проекту не прошла; позже
   отдельный health-запрос ответил `ok` (состояние моста менялось во времени).

**Итоговый честный статус:** Node MCP-сервер собирается; HTTP Unity Bridge
иногда подтверждал health и `DebugLog`; проблема конкретно GLM не воспроизведена
и не диагностирована без его логов; чтение консоли и Play Mode требуют отдельной
валидации; кириллицу через прежний curl-тест отправлять нельзя без явной UTF-8.


---

## 2026-11-26 — стабильность моста под Unity 2022.3.15f1

Проверено на открытом редакторе: вызовы через собранный `mcp-server/dist/index.js` и напрямую по HTTP, включая три перезагрузки скриптов подряд.

| Проблема | Причина | Исправление | Файл |
|---|---|---|---|
| `GetConsoleLogs` возвращал не больше трёх самых старых записей | Число записей считалось как сумма `GetEntryCount(0..2)`, а это счётчик повторов трёх первых строк | Число строк берётся из `StartGettingEntries()` / `GetCount()` | `UnityCommandExecutor.cs` |
| Уровень записи определялся неверно (обычный `Debug.Log` помечался `error`), счётчики всегда 0 | Использовались биты 23 и 21 и несуществующий `GetCountByMode` | Маски по `ConsoleWindow.Mode`, счётчики через `GetCountsByType` | `UnityCommandExecutor.cs` |
| `GetCompilationErrors` всегда отвечал «ошибок нет» | Тот же несуществующий `GetCountByMode` | `EditorUtility.scriptCompilationFailed` и список записей с битом ошибки компиляции | `UnityCommandExecutor.cs` |
| При перезагрузке скриптов поток слушателя обрывался, в консоль сыпались сообщения, запросы получали обрыв соединения | Сервер не останавливался перед перезагрузкой; в обработчике вызывался `Thread.ResetAbort()` | Остановка в `AssemblyReloadEvents.beforeAssemblyReload`: сначала ответ 503 ожидающим запросам, затем закрытие listener | `MCPBridgeServer.cs` |
| Команда, не дождавшаяся главного потока, выполнялась позже, уже после ответа «таймаут» | Задача оставалась в очереди | Просроченная задача отменяется | `MCPBridgeServer.cs` |
| Любая ошибка возвращалась как 500 | Не различались виды ошибок | 400 — ошибка запроса, 503 — редактор занят (`retryable: true`), 500 — сбой команды | `MCPBridgeServer.cs` |
| Исключение при записи ответа отключившемуся клиенту уходило в пул потоков | Не было защиты вокруг записи ответа | Запись ответа изолирована | `MCPBridgeServer.cs` |
| Клиент падал с ошибкой, если вызов попадал на перезапуск моста | Не было повторных попыток | До 45 с повторов при отказе в соединении и при 503 — в обоих случаях команда гарантированно не выполнялась | `mcp-server/src/index.ts` |
| HTTP-ошибки моста выглядели для агента как успешный результат | Ответ не помечался `isError` | Ответы со статусом ≥ 400 или `success: false` помечаются ошибкой | `mcp-server/src/index.ts` |

Дополнительно:

- `/health` отвечает без главного потока и сообщает `isCompiling`, `isUpdating`, `isPlaying`, `mainThreadIdleMs`, `projectPath`, `unityVersion` — по нему видно, какой проект открыт и занят ли редактор.
- `GetConsoleLogs` возвращает `file`, `line`, `total`; стек вызовов — только по аргументу `stackTrace: true`.
- Слушатель принимает и `localhost`, и `127.0.0.1`.

Известные ограничения:

- Запрос, оборванный посреди передачи (`ECONNRESET`), не повторяется: неизвестно, успела ли команда выполниться.
- Чтение консоли опирается на внутренний `UnityEditor.LogEntries` и показывает то же, что окно Console, включая его фильтры и Collapse.
- `mcp-server/dist/` и `node_modules/` в репозиторий не входят: после клонирования нужны `npm ci` и `npm run build`.
