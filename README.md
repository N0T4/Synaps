# Synaps — базова архітектура Unity

Стартовий desktop-прототип для циклу **пошук → осмислення → збереження → зв’язування → повторне відкриття**.
Пошук і AI працюють на локальних mock-даних. VR-шолом, Meta SDK, OpenXR, мережа й API-ключі для прототипу не потрібні.

## Запуск

1. У Unity Hub вибери **Add → Add project from disk** і вкажи папку `Synaps`, де міститься цей README.
2. Відкрий проєкт у **Unity 6.0**. Файл `ProjectVersion.txt` задає базову версію `6000.0.60f1`;
   інша встановлена версія Unity 6 може запропонувати оновлення проєкту.
3. Дочекайся імпорту пакетів. Перше відкриття потребує доступу до Unity Package Registry.
4. Відкрий `Assets/Synaps/Scenes/DesktopPrototype.unity` або меню **Synaps → Open Desktop Prototype**.
5. Натисни **Play**, перейди до **Game** і натисни **Search**. Підійде будь-який непорожній запит.

У сцені вже є `SynapsBootstrap`. Камера, world-space Canvas, картки й EventSystem створюються під час Play;
ручне налаштування Inspector не потрібне. Початкове поле містить `best VR text input methods`.

### Перший робочий цикл

- **Search** або **Enter** у полі запиту показує п’ять карток: FingerType, BiTipText, H4VR, TapGazer, PinchType.
- **Summary** створює позначений mock-summary; якщо вузол уже збережений, його summary теж оновлюється.
- **Save node** додає вузол у граф. Повторне збереження того самого результату вибирає наявний вузол.
- Клік на вузол вибирає його. Перетягування мишкою змінює позицію.
- Вибери початковий вузол, введи тип зв’язку, натисни **Connect**, потім вибери інший вузол.
  Стрілка спрямована від першого вузла до другого. **Cancel** скасовує вибір цілі.
- **Delete node** видаляє вибраний вузол і його вхідні та вихідні зв’язки.
- Зміни зберігаються автоматично через 0,6 секунди після останньої зміни; **Save graph** зберігає одразу.
- Зупини Play і запусти знову: вузли, зв’язки, summary та позиції завантажаться.

Картки є тестовими даними, а не перевіреним описом наукових робіт. **Open** вимкнений для mock-карток,
бо їм не приписано вигаданих URL. У справжнього провайдера HTTP/HTTPS-посилання відкриваються у браузері.

## Структура

```text
Assets/Synaps/
  Core/                    Моделі та спільні команди взаємодії
  Search/                  ISearchProvider, MockSearchProvider, SearchController
  KnowledgeGraph/          Сервіс графа, репозиторний контракт, workspace-команди, валідація
  AI/                      IAIService та детермінований MockAIService
  UI/                      World-space desktop-інтерфейс, картки, вузли та стрілки
  XR/                      Межа для майбутніх XR-адаптерів; SDK ще не підключено
  Input/Contracts/         ITextInput
  Input/Desktop/           Текст із клавіатури, вибір і переміщення мишкою
  Data/                    FileGraphRepository та контракт серіалізації
  Data/Unity/              JSON-адаптер через JsonUtility
  Bootstrap/               Збирання сервісів і desktop-адаптерів
  Editor/                  Меню відкриття сцени
  Prefabs/                 Місце для майбутніх авторських prefab; зараз runtime-фабрики
  Scenes/                  DesktopPrototype.unity
  Tests/EditMode/          Поведінкові тести та Unity JSON-тести
Docs/
  ARCHITECTURE_UA.md        Залежності, пояснення рішень, точки розширення
  VERIFICATION_UA.md        Що перевірено й що потребує Unity Editor
Tools/
  Verify-Core.ps1          Перевірка незалежного ядра без Unity на Windows
```

Кожен модуль має окремий `.asmdef`. Для доменних збірок задано `noEngineReferences: true`:
Unity, UI та XR API не можна випадково протягнути в ядро.

## Локальні дані

Файл: `Application.persistentDataPath/Synaps/graph.json`.
На Windows із поточними `companyName`/`productName` це зазвичай
`%USERPROFILE%/AppData/LocalLow/Synaps/Synaps/Synaps/graph.json`.
Точний шлях виводиться в Unity Console при запуску.

Запис спершу йде у тимчасовий файл, перевіряється, а потім замінює основний файл.
Попередня версія зберігається як `graph.json.bak`. Пошкоджений основний файл можна відновити з валідної копії;
під час такого відновлення оригінал зберігається в `graph.json.corrupt-...`.
Якщо обидва файли невалідні, запуск блокує редагування та показує помилку, зберігаючи оригінали.
Невідома версія схеми потребує міграції й не замінюється старішою резервною копією.

Зараз це невеликий локальний граф з одним власником запису. Для багатьох одночасних процесів,
великих графів, синхронізації або іншої платформи додай інший `IGraphRepository`.

## Перевірки

У Unity: **Window → General → Test Runner → EditMode → Run All**.

Без Unity на Windows із .NET Framework:

```powershell
./Tools/Verify-Core.ps1
```

Команда компілює шість доменних збірок без Unity-посилань і запускає 21 поведінковий тест.
JSON-адаптер та scene/UI-перевірки потребують Unity Editor. Деталі — у `Docs/VERIFICATION_UA.md`.

## Наступне підключення XR

Інструкції — у `Assets/Synaps/XR/README.md` та `Docs/ARCHITECTURE_UA.md`.
Майбутній XR composition root замінює desktop-камеру й адаптери вводу, а доменні сервіси залишаються тими самими.

Довідка Unity: [збірки та модулі](https://docs.unity3d.com/6000.0/Documentation/Manual/assembly-definition-files.html),
[Unity UI](https://docs.unity3d.com/6000.0/Documentation/Manual/com.unity.ugui.html),
[Input System UI module](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.17/api/UnityEngine.InputSystem.UI.InputSystemUIInputModule.html).
