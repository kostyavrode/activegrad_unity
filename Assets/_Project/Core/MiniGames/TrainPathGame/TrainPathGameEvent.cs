using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using ActiveGrad.MiniGames;

public class TrainPathGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private TrainPathController _controller;

    [Inject] private UserDataService _userDataService;

    protected override void OnStartGame()
    {
        CreateGameUI();
    }

    private void CreateGameUI()
    {
        // Создаем корневой объект
        _gameRoot = new GameObject("TrainPathGame");
        _gameRoot.transform.SetParent(_parentContainer, false);
        
        // Добавляем Canvas
        Canvas canvas = _gameRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        
        CanvasScaler scaler = _gameRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        
        _gameRoot.AddComponent<GraphicRaycaster>();
        
        RectTransform rootRect = _gameRoot.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.sizeDelta = Vector2.zero;
        
        // Добавляем компоненты
        TrainPathUI ui = _gameRoot.AddComponent<TrainPathUI>();
        _controller = _gameRoot.AddComponent<TrainPathController>();
        
        // Создаем Background
        GameObject backgroundObj = CreateBackground(_gameRoot.transform);
        ui.SetBackground(backgroundObj.GetComponent<Image>());
        
        // Создаем Start Screen
        GameObject startScreen = CreateStartScreen(_gameRoot.transform, ui);
        ui.SetStartScreen(startScreen);
        
        // Создаем Game Screen
        GameObject gameScreen = CreateGameScreen(_gameRoot.transform, ui);
        ui.SetGameScreen(gameScreen);
        
        // Создаем End Screen
        GameObject endScreen = CreateEndScreen(_gameRoot.transform, ui);
        ui.SetEndScreen(endScreen);
        
        // Инициализируем контроллер
        _controller.Initialize(this, ui, _userDataService);
    }

    private GameObject CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);
        
        Image image = bg.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.2f);
        
        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        return bg;
    }

    private GameObject CreateStartScreen(Transform parent, TrainPathUI ui)
    {
        GameObject screen = new GameObject("StartScreen");
        screen.transform.SetParent(parent, false);
        
        RectTransform rect = screen.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        // Навык (Интеллект)
        GameObject skillObj = CreateText("SkillText", screen.transform,
            new Vector2(0.5f, 0.75f), Vector2.zero, new Vector2(250, 30),
            "Интеллект: 1", 20, TextAlignmentOptions.Center);
        if (ui != null)
            ui.SetSkillText(skillObj.GetComponent<TMP_Text>());
        
        // Кнопка закрытия
        GameObject closeBtn = CreateButton("CloseButton", screen.transform,
            new Vector2(1, 1), new Vector2(-60, -30), new Vector2(100, 40),
            new Color(0.6f, 0.2f, 0.2f), "Закрыть");
        if (ui != null)
            ui.SetCloseButton(closeBtn.GetComponent<Button>());
        
        // Кнопка "Начать игру"
        GameObject buttonObj = new GameObject("StartButton");
        buttonObj.transform.SetParent(screen.transform, false);
        
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.8f, 0.2f);
        
        Button button = buttonObj.AddComponent<Button>();
        
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(200, 60);
        buttonRect.anchoredPosition = Vector2.zero;
        
        // Текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        TMP_Text text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "Начать игру";
        text.fontSize = 24;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        if (ui != null)
            ui.SetStartButton(button);
        
        return screen;
    }

    private GameObject CreateGameScreen(Transform parent, TrainPathUI ui)
    {
        GameObject screen = new GameObject("GameScreen");
        screen.transform.SetParent(parent, false);
        screen.SetActive(false); // Начинаем со стартового экрана
        
        RectTransform screenRect = screen.AddComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.sizeDelta = Vector2.zero;
        
        // Map Container
        GameObject mapContainer = new GameObject("MapContainer");
        mapContainer.transform.SetParent(screen.transform, false);
        
        RectTransform mapRect = mapContainer.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = new Vector2(800, 600);
        mapRect.anchoredPosition = Vector2.zero;
        
        if (ui != null)
            ui.SetMapContainer(mapRect);
        
        // Train
        GameObject train = new GameObject("Train");
        train.transform.SetParent(screen.transform, false);
        
        Image trainImage = train.AddComponent<Image>();
        trainImage.color = new Color(0.8f, 0.5f, 0.2f);
        
        RectTransform trainRect = train.GetComponent<RectTransform>();
        trainRect.anchorMin = new Vector2(0.5f, 0.5f);
        trainRect.anchorMax = new Vector2(0.5f, 0.5f);
        trainRect.sizeDelta = new Vector2(30, 30);
        trainRect.anchoredPosition = Vector2.zero;
        
        if (ui != null)
            ui.SetTrain(trainRect);
        
        // Time Text
        GameObject timeTextObj = CreateText("TimeText", screen.transform, 
            new Vector2(0, 1), new Vector2(20, -20), new Vector2(200, 30),
            "Время: 0.0с", 20, TextAlignmentOptions.TopLeft);
        
        if (ui != null)
            ui.SetTimeText(timeTextObj.GetComponent<TMP_Text>());
        
        // Path Info Text
        GameObject pathInfoObj = CreateText("PathInfoText", screen.transform,
            new Vector2(1, 1), new Vector2(-20, -20), new Vector2(200, 30),
            "Станций: 0/0", 20, TextAlignmentOptions.TopRight);
        
        if (ui != null)
            ui.SetPathInfoText(pathInfoObj.GetComponent<TMP_Text>());
        
        return screen;
    }

    private GameObject CreateEndScreen(Transform parent, TrainPathUI ui)
    {
        GameObject screen = new GameObject("EndScreen");
        screen.transform.SetParent(parent, false);
        screen.SetActive(false);
        
        RectTransform screenRect = screen.AddComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.sizeDelta = Vector2.zero;
        
        Image bg = screen.AddComponent<Image>();
        bg.color = new Color(0, 0, 0, 0.7f);
        
        // Result Text
        GameObject resultObj = CreateText("ResultText", screen.transform,
            new Vector2(0.5f, 1), new Vector2(0, -50), new Vector2(400, 50),
            "Результат", 32, TextAlignmentOptions.Center);
        
        if (ui != null)
            ui.SetResultText(resultObj.GetComponent<TMP_Text>());
        
        // Player Time Text
        GameObject playerTimeObj = CreateText("PlayerTimeText", screen.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 50), new Vector2(300, 40),
            "Ваше время: 0.0с", 24, TextAlignmentOptions.Center);
        
        if (ui != null)
            ui.SetPlayerTimeText(playerTimeObj.GetComponent<TMP_Text>());
        
        // Optimal Time Text
        GameObject optimalTimeObj = CreateText("OptimalTimeText", screen.transform,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(300, 40),
            "Оптимальное время: 0.0с", 24, TextAlignmentOptions.Center);
        
        if (ui != null)
            ui.SetOptimalTimeText(optimalTimeObj.GetComponent<TMP_Text>());
        
        // Score Text
        GameObject scoreObj = CreateText("ScoreText", screen.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(300, 40),
            "Очки: 0", 28, TextAlignmentOptions.Center);
        
        TMP_Text scoreText = scoreObj.GetComponent<TMP_Text>();
        scoreText.color = Color.yellow;
        if (ui != null)
            ui.SetScoreText(scoreText);
        
        // Bonus Slider
        GameObject bonusSliderContainer = new GameObject("BonusSliderContainer");
        bonusSliderContainer.transform.SetParent(screen.transform, false);
        
        RectTransform bonusContainerRect = bonusSliderContainer.AddComponent<RectTransform>();
        bonusContainerRect.anchorMin = new Vector2(0.5f, 0.5f);
        bonusContainerRect.anchorMax = new Vector2(0.5f, 0.5f);
        bonusContainerRect.sizeDelta = new Vector2(450, 60);
        bonusContainerRect.anchoredPosition = new Vector2(0, -140);
        
        GameObject trackObj = new GameObject("BonusSliderTrack");
        trackObj.transform.SetParent(bonusSliderContainer.transform, false);
        Image trackImg = trackObj.AddComponent<Image>();
        trackImg.color = new Color(0.3f, 0.3f, 0.3f);
        RectTransform trackRect = trackObj.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0.5f);
        trackRect.anchorMax = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(400, 30);
        trackRect.anchoredPosition = Vector2.zero;
        
        GameObject leftLabelObj = new GameObject("BonusLeftLabel");
        leftLabelObj.transform.SetParent(bonusSliderContainer.transform, false);
        TMP_Text leftLabel = leftLabelObj.AddComponent<TextMeshProUGUI>();
        leftLabel.text = "Уровень профессии";
        leftLabel.fontSize = 12;
        leftLabel.color = Color.white;
        leftLabel.alignment = TextAlignmentOptions.Left;
        RectTransform leftRect = leftLabelObj.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0, 0.5f);
        leftRect.anchorMax = new Vector2(0, 0.5f);
        leftRect.sizeDelta = new Vector2(180, 20);
        leftRect.anchoredPosition = new Vector2(-210, -20);
        
        GameObject rightLabelObj = new GameObject("BonusRightLabel");
        rightLabelObj.transform.SetParent(bonusSliderContainer.transform, false);
        TMP_Text rightLabel = rightLabelObj.AddComponent<TextMeshProUGUI>();
        rightLabel.text = "% выполнения задания";
        rightLabel.fontSize = 12;
        rightLabel.color = Color.white;
        rightLabel.alignment = TextAlignmentOptions.Right;
        RectTransform rightRect = rightLabelObj.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1, 0.5f);
        rightRect.anchorMax = new Vector2(1, 0.5f);
        rightRect.sizeDelta = new Vector2(180, 20);
        rightRect.anchoredPosition = new Vector2(210, -20);
        
        GameObject indicatorObj = new GameObject("BonusSliderIndicator");
        indicatorObj.transform.SetParent(trackObj.transform, false);
        Image indicatorImg = indicatorObj.AddComponent<Image>();
        indicatorImg.color = Color.yellow;
        RectTransform indicatorRect = indicatorObj.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.sizeDelta = new Vector2(10, 36);
        indicatorRect.anchoredPosition = Vector2.zero;
        
        BonusSliderComponent bonusSlider = bonusSliderContainer.AddComponent<BonusSliderComponent>();
        bonusSlider.Setup(trackRect, indicatorRect, leftLabel, rightLabel, null);
        
        // Buttons Container (скрывается до остановки ползунка)
        GameObject buttonsContainer = new GameObject("EndScreenButtonsContainer");
        buttonsContainer.transform.SetParent(screen.transform, false);
        
        RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.5f, 0.5f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRect.sizeDelta = new Vector2(320, 60);
        buttonsRect.anchoredPosition = new Vector2(0, -120);
        
        GameObject restartBtn = CreateButton("RestartButton", buttonsContainer.transform,
            new Vector2(0.5f, 0.5f), new Vector2(-80, 0), new Vector2(150, 50),
            new Color(0.2f, 0.5f, 0.8f), "Заново");
        
        if (ui != null)
            ui.SetRestartButton(restartBtn.GetComponent<Button>());
        
        GameObject finishBtn = CreateButton("FinishButton", buttonsContainer.transform,
            new Vector2(0.5f, 0.5f), new Vector2(80, 0), new Vector2(150, 50),
            new Color(0.2f, 0.8f, 0.2f), "Завершить");
        
        if (ui != null)
            ui.SetFinishButton(finishBtn.GetComponent<Button>());
        
        buttonsContainer.SetActive(false);
        
        bonusSlider.Setup(trackRect, indicatorRect, leftLabel, rightLabel, buttonsContainer);
        
        if (ui != null)
        {
            ui.SetBonusSliderTrack(trackRect);
            ui.SetBonusSliderIndicator(indicatorRect);
            ui.SetBonusLeftLabel(leftLabel);
            ui.SetBonusRightLabel(rightLabel);
            ui.SetEndScreenButtonsContainer(buttonsContainer);
            ui.SetBonusSliderComponent(bonusSlider);
        }
        
        return screen;
    }

    private GameObject CreateText(string name, Transform parent, Vector2 anchor, 
        Vector2 position, Vector2 size, string text, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        
        TMP_Text tmpText = obj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.color = Color.white;
        tmpText.alignment = alignment;
        
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        return obj;
    }

    private GameObject CreateButton(string name, Transform parent, Vector2 anchor,
        Vector2 position, Vector2 size, Color color, string text)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        
        Image image = obj.AddComponent<Image>();
        image.color = color;
        
        Button button = obj.AddComponent<Button>();
        
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        
        // Текст кнопки
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);
        
        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = 20;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        return obj;
    }

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
        {
            Object.Destroy(_gameRoot);
        }
    }

    public void OnGameEnded(float playerTime, float optimalTime, bool isOptimal)
    {
        int score = CalculateScore(playerTime, optimalTime, isOptimal);
        FinishGame(true, score);
    }

    public void OnGameEndedWithFinalScore(int finalScore)
    {
        FinishGame(true, finalScore);
    }

    private int CalculateScore(float playerTime, float optimalTime, bool isOptimal)
    {
        if (isOptimal)
            return 100;
        
        float ratio = optimalTime / playerTime;
        return Mathf.RoundToInt(ratio * 100);
    }

    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
