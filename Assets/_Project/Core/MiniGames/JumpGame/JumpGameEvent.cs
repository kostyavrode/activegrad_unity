using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class JumpGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private JumpController _controller;

    protected override void OnStartGame()
    {
        CreateGameUI();
    }

    private void CreateGameUI()
    {
        // Создаем корневой объект
        _gameRoot = new GameObject("JumpGame");
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
        JumpUI ui = _gameRoot.AddComponent<JumpUI>();
        _controller = _gameRoot.AddComponent<JumpController>();
        
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
        _controller.Initialize(this, ui);
    }

    private GameObject CreateBackground(Transform parent)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);
        
        Image image = bg.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.2f);
        
        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
        return bg;
    }

    private GameObject CreateStartScreen(Transform parent, JumpUI ui)
    {
        GameObject screen = new GameObject("StartScreen");
        screen.transform.SetParent(parent, false);
        
        RectTransform rect = screen.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        
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

    private GameObject CreateGameScreen(Transform parent, JumpUI ui)
    {
        GameObject screen = new GameObject("GameScreen");
        screen.transform.SetParent(parent, false);
        screen.SetActive(false);
        
        RectTransform screenRect = screen.AddComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.sizeDelta = Vector2.zero;
        
        // Player (игрок для прыжка, вид сбоку)
        GameObject player = new GameObject("Player");
        player.transform.SetParent(screen.transform, false);
        
        Image playerImage = player.AddComponent<Image>();
        playerImage.color = new Color(0.8f, 0.3f, 0.3f);
        
        RectTransform playerRect = player.GetComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.2f, 0.3f);
        playerRect.anchorMax = new Vector2(0.2f, 0.3f);
        playerRect.sizeDelta = new Vector2(40, 60);
        playerRect.anchoredPosition = Vector2.zero;
        
        if (ui != null)
            ui.SetPlayer(playerRect);
        
        // Ground (земля)
        GameObject ground = new GameObject("Ground");
        ground.transform.SetParent(screen.transform, false);
        
        Image groundImage = ground.AddComponent<Image>();
        groundImage.color = new Color(0.4f, 0.3f, 0.2f);
        
        RectTransform groundRect = ground.GetComponent<RectTransform>();
        groundRect.anchorMin = new Vector2(0, 0);
        groundRect.anchorMax = new Vector2(1, 0.15f);
        groundRect.sizeDelta = Vector2.zero;
        groundRect.anchoredPosition = Vector2.zero;
        
        // Slider Container (контейнер для ползунка)
        GameObject sliderContainer = new GameObject("SliderContainer");
        sliderContainer.transform.SetParent(screen.transform, false);
        
        RectTransform sliderContainerRect = sliderContainer.AddComponent<RectTransform>();
        sliderContainerRect.anchorMin = new Vector2(0.5f, 0);
        sliderContainerRect.anchorMax = new Vector2(0.5f, 0);
        sliderContainerRect.sizeDelta = new Vector2(400, 80);
        sliderContainerRect.anchoredPosition = new Vector2(0, 50);
        
        // Slider Background
        GameObject sliderBg = new GameObject("SliderBackground");
        sliderBg.transform.SetParent(sliderContainer.transform, false);
        
        Image sliderBgImage = sliderBg.AddComponent<Image>();
        sliderBgImage.color = new Color(0.3f, 0.3f, 0.3f);
        
        RectTransform sliderBgRect = sliderBg.GetComponent<RectTransform>();
        sliderBgRect.anchorMin = Vector2.zero;
        sliderBgRect.anchorMax = Vector2.one;
        sliderBgRect.sizeDelta = Vector2.zero;
        
        // Фон должен быть первым (внизу)
        sliderBg.transform.SetAsFirstSibling();
        
        if (ui != null)
            ui.SetSliderBackground(sliderBgRect);
        
        // Zones Container
        GameObject zonesContainer = new GameObject("ZonesContainer");
        zonesContainer.transform.SetParent(sliderContainer.transform, false);
        
        RectTransform zonesRect = zonesContainer.AddComponent<RectTransform>();
        zonesRect.anchorMin = Vector2.zero;
        zonesRect.anchorMax = Vector2.one;
        zonesRect.sizeDelta = Vector2.zero;
        
        // Контейнер зон должен быть после фона
        zonesContainer.transform.SetSiblingIndex(1);
        
        if (ui != null)
            ui.SetZonesContainer(zonesRect);
        
        // Slider Indicator (индикатор на ползунке)
        GameObject indicator = new GameObject("SliderIndicator");
        indicator.transform.SetParent(sliderContainer.transform, false);
        
        Image indicatorImage = indicator.AddComponent<Image>();
        indicatorImage.color = new Color(1f, 1f, 1f);
        
        RectTransform indicatorRect = indicator.GetComponent<RectTransform>();
        indicatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        indicatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        indicatorRect.sizeDelta = new Vector2(10, 60);
        indicatorRect.anchoredPosition = Vector2.zero;
        
        // Индикатор должен быть поверх всего (последним)
        indicator.transform.SetAsLastSibling();
        
        if (ui != null)
            ui.SetSliderIndicator(indicatorRect);
        
        // Jump Counter Text
        GameObject jumpCounterObj = CreateText("JumpCounterText", screen.transform,
            new Vector2(0.5f, 1), new Vector2(0, -30), new Vector2(200, 30),
            "Прыжок: 1/3", 24, TextAlignmentOptions.Center);
        
        if (ui != null)
            ui.SetJumpCounterText(jumpCounterObj.GetComponent<TMP_Text>());
        
        // Score Text
        GameObject scoreObj = CreateText("ScoreText", screen.transform,
            new Vector2(0.5f, 1), new Vector2(0, -70), new Vector2(200, 30),
            "Очки: 0", 20, TextAlignmentOptions.Center);
        
        if (ui != null)
            ui.SetScoreText(scoreObj.GetComponent<TMP_Text>());
        
        // Instruction Text
        GameObject instructionObj = CreateText("InstructionText", screen.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 150), new Vector2(400, 40),
            "Нажмите когда индикатор в зеленой зоне!", 18, TextAlignmentOptions.Center);
        
        TMP_Text instructionText = instructionObj.GetComponent<TMP_Text>();
        instructionText.color = Color.yellow;
        if (ui != null)
            ui.SetInstructionText(instructionText);
        
        return screen;
    }

    private GameObject CreateEndScreen(Transform parent, JumpUI ui)
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
            "Игра завершена!", 32, TextAlignmentOptions.Center);
        
        if (ui != null)
            ui.SetResultText(resultObj.GetComponent<TMP_Text>());
        
        // Total Score Text
        GameObject totalScoreObj = CreateText("TotalScoreText", screen.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(300, 40),
            "Итого очков: 0", 28, TextAlignmentOptions.Center);
        
        TMP_Text totalScoreText = totalScoreObj.GetComponent<TMP_Text>();
        totalScoreText.color = Color.yellow;
        if (ui != null)
            ui.SetTotalScoreText(totalScoreText);
        
        // Finish Button
        GameObject finishBtn = CreateButton("FinishButton", screen.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, -100), new Vector2(150, 50),
            new Color(0.2f, 0.8f, 0.2f), "Завершить");
        
        if (ui != null)
            ui.SetFinishButton(finishBtn.GetComponent<Button>());
        
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

    public void OnGameEnded(int totalScore)
    {
        FinishGame(true, totalScore);
    }
}
