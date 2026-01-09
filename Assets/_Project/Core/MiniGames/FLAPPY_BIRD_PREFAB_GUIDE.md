# Инструкция по созданию префаба Flappy Bird

## Шаг 1: Создание структуры префаба

1. Создайте новый GameObject в сцене (или в папке Prefabs)
2. Назовите его `FlappyBirdGame`
3. Добавьте компоненты:
   - `Canvas` (Render Mode: Screen Space - Overlay)
   - `CanvasScaler` (UI Scale Mode: Scale With Screen Size, Reference Resolution: 400x600)
   - `GraphicRaycaster`
   - `FlappyBirdUI` (скрипт)
   - `FlappyBirdController` (скрипт)

## Шаг 2: Настройка Canvas

- Canvas должен заполнять весь экран
- RectTransform: Anchor Min (0,0), Anchor Max (1,1), Size Delta (0,0)

## Шаг 3: Создание UI элементов

### 3.1 Background (фон)
- Создайте дочерний GameObject `Background`
- Добавьте `Image` компонент
- Цвет: (102, 204, 255) или любой другой
- RectTransform: Anchor Min (0,0), Anchor Max (1,1), Size Delta (0,0)

### 3.2 Bird (птица)
- Создайте дочерний GameObject `Bird`
- Добавьте `Image` компонент (цвет желтый)
- RectTransform:
  - Size Delta: (40, 40)
  - Anchor: Center (0.5, 0.5)
  - Pivot: (0.5, 0.5)
  - Anchored Position: (-150, 0)

### 3.3 PipesContainer (контейнер для труб)
- Создайте дочерний GameObject `PipesContainer`
- Это просто пустой Transform для спавна труб
- RectTransform: Anchor Min (0,0), Anchor Max (1,1), Size Delta (0,0)

### 3.4 ScoreText (текст счета)
- Создайте дочерний GameObject `ScoreText`
- Добавьте `TextMeshProUGUI` компонент
- Текст: "Score: 0"
- Font Size: 24
- Color: White
- Alignment: Top Center
- RectTransform:
  - Anchor: Top Center (0.5, 1)
  - Anchored Position: (0, -30)
  - Size Delta: (200, 50)

### 3.5 GameOverText (текст Game Over)
- Создайте дочерний GameObject `GameOverText`
- Добавьте `TextMeshProUGUI` компонент
- Текст: "Game Over!"
- Font Size: 32
- Color: Red
- Alignment: Center
- RectTransform:
  - Anchor: Center (0.5, 0.5)
  - Anchored Position: (0, 0)
  - Size Delta: (300, 100)
- **Важно:** Сделайте его неактивным по умолчанию (снимите галочку Active)

### 3.6 FinishButton (кнопка завершения)
- Создайте дочерний GameObject `FinishButton`
- Добавьте `Button` компонент
- Добавьте `Image` компонент (цвет: зеленый)
- RectTransform:
  - Anchor: Center (0.5, 0.5)
  - Anchored Position: (0, -100)
  - Size Delta: (150, 50)
- Создайте дочерний GameObject `Text` внутри кнопки
- Добавьте `TextMeshProUGUI` с текстом "Finish"
- **Важно:** Сделайте кнопку неактивной по умолчанию

## Шаг 4: Настройка FlappyBirdUI компонента

На компоненте `FlappyBirdUI` назначьте все ссылки:

- **Bird** → перетащите GameObject `Bird`
- **Pipes Container** → перетащите GameObject `PipesContainer`
- **Score Text** → перетащите GameObject `ScoreText`
- **Game Over Text** → перетащите GameObject `GameOverText`
- **Finish Button** → перетащите GameObject `FinishButton`
- **Background** → перетащите GameObject `Background` (опционально)

## Шаг 5: Сохранение префаба

1. Перетащите `FlappyBirdGame` из Hierarchy в папку `Assets/Resources/MiniGames/`
2. Назовите префаб `FlappyBirdGame.prefab`
3. Удалите GameObject из сцены (если создавали в сцене)

## Структура префаба:

```
FlappyBirdGame (GameObject)
├── Canvas
├── CanvasScaler
├── GraphicRaycaster
├── FlappyBirdUI (компонент)
├── FlappyBirdController (компонент)
├── Background (Image)
├── Bird (Image) ← назначить в FlappyBirdUI.Bird
├── PipesContainer (Transform) ← назначить в FlappyBirdUI.PipesContainer
├── ScoreText (TextMeshProUGUI) ← назначить в FlappyBirdUI.ScoreText
├── GameOverText (TextMeshProUGUI) ← назначить в FlappyBirdUI.GameOverText
└── FinishButton (Button) ← назначить в FlappyBirdUI.FinishButton
    └── Text (TextMeshProUGUI)
```

## Важные моменты:

1. ✅ Префаб должен быть в `Resources/MiniGames/FlappyBirdGame.prefab`
2. ✅ Все ссылки в `FlappyBirdUI` должны быть назначены
3. ✅ `GameOverText` и `FinishButton` должны быть неактивны по умолчанию
4. ✅ Canvas должен быть настроен правильно (Screen Space - Overlay)

## После создания префаба:

Игра автоматически загрузит префаб при запуске. Вы сможете редактировать UI элементы прямо в префабе, и изменения будут применяться в игре!


