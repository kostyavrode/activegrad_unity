using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Zenject;
using ActiveGrad.MiniGames;

public class JumpGameEvent : BaseGameEvent
{
    private GameObject _gameRoot;
    private JumpController _controller;

    [Inject] private UserDataService _userDataService;

    protected override void OnStartGame()
    {
        CreateGameUI();
    }

    private void CreateGameUI()
    {
        _gameRoot = new GameObject("JumpGame");
        _gameRoot.transform.SetParent(_parentContainer, false);

        Canvas canvas = _gameRoot.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = _gameRoot.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.matchWidthOrHeight = 0.5f;

        _gameRoot.AddComponent<GraphicRaycaster>();

        JumpUI ui = _gameRoot.AddComponent<JumpUI>();
        _controller = _gameRoot.AddComponent<JumpController>();

        CreateBackground(_gameRoot.transform, ui);
        ui.SetStartScreen(CreateStartScreen(_gameRoot.transform, ui));
        ui.SetGameScreen(CreateGameScreen(_gameRoot.transform, ui));
        ui.SetEndScreen(CreateEndScreen(_gameRoot.transform, ui));

        _controller.Initialize(this, ui, _userDataService);
    }

    private void CreateBackground(Transform parent, JumpUI ui)
    {
        GameObject bg = new GameObject("Background");
        bg.transform.SetParent(parent, false);

        Image image = bg.AddComponent<Image>();
        image.color = new Color(0.07f, 0.08f, 0.15f);

        RectTransform rect = bg.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        ui.SetBackground(image);
    }

    #region Start Screen

    private GameObject CreateStartScreen(Transform parent, JumpUI ui)
    {
        GameObject screen = new GameObject("StartScreen");
        screen.transform.SetParent(parent, false);

        RectTransform rect = screen.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        // Card panel
        GameObject panel = CreatePanel("StartPanel", screen.transform,
            new Vector2(0.5f, 0.5f), new Vector2(380, 360),
            Vector2.zero, new Color(0.11f, 0.13f, 0.24f, 0.96f));

        // Title
        GameObject titleObj = CreateText("Title", panel.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -28), new Vector2(340, 48),
            "Прыжки", 34, TextAlignmentOptions.Center);
        TMP_Text titleTmp = titleObj.GetComponent<TMP_Text>();
        titleTmp.fontStyle = FontStyles.Bold;
        ui.SetTitleText(titleTmp);

        // Divider line
        CreatePanel("Divider", panel.transform,
            new Vector2(0.5f, 1f), new Vector2(300, 2),
            new Vector2(0, -62), new Color(1f, 1f, 1f, 0.1f));

        // Rules
        GameObject rulesObj = CreateText("Rules", panel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(330, 100),
            "Выполните 3 прыжка!\n\nНажмите на экран или пробел,\nкогда индикатор в зелёной зоне\nдля максимальных очков.",
            15, TextAlignmentOptions.Center);
        TMP_Text rulesTmp = rulesObj.GetComponent<TMP_Text>();
        rulesTmp.color = new Color(0.68f, 0.70f, 0.78f);
        ui.SetRulesText(rulesTmp);

        // Skill badge
        GameObject skillObj = CreateText("SkillText", panel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, -48), new Vector2(240, 28),
            "Ловкость: 1", 17, TextAlignmentOptions.Center);
        skillObj.GetComponent<TMP_Text>().color = new Color(1f, 0.82f, 0.28f);
        ui.SetSkillText(skillObj.GetComponent<TMP_Text>());

        // Start button
        GameObject startBtn = CreateButton("StartButton", panel.transform,
            new Vector2(0.5f, 0f), new Vector2(0, 45), new Vector2(210, 52),
            new Color(0.22f, 0.70f, 0.40f), "Начать игру", 22);
        ui.SetStartButton(startBtn.GetComponent<Button>());

        // Close button (top-right corner of screen)
        GameObject closeBtn = CreateButton("CloseButton", screen.transform,
            new Vector2(1f, 1f), new Vector2(-40, -28), new Vector2(56, 36),
            new Color(0.70f, 0.24f, 0.28f), "✕", 20);
        ui.SetCloseButton(closeBtn.GetComponent<Button>());

        return screen;
    }

    #endregion

    #region Game Screen

    private GameObject CreateGameScreen(Transform parent, JumpUI ui)
    {
        GameObject screen = new GameObject("GameScreen");
        screen.transform.SetParent(parent, false);
        screen.SetActive(false);

        RectTransform screenRect = screen.AddComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.sizeDelta = Vector2.zero;

        // === Bottom dark bar for slider ===
        CreatePanel("BottomBar", screen.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.14f),
            new Color(0.05f, 0.06f, 0.11f));

        // === Ground ===
        CreatePanel("Ground", screen.transform,
            new Vector2(0f, 0.14f), new Vector2(1f, 0.30f),
            new Color(0.20f, 0.46f, 0.22f));

        // Grass strip on top of ground
        CreatePanel("GrassStrip", screen.transform,
            new Vector2(0f, 0.29f), new Vector2(1f, 0.32f),
            new Color(0.30f, 0.62f, 0.28f));

        // === Player body ===
        GameObject player = new GameObject("Player");
        player.transform.SetParent(screen.transform, false);
        Image playerImg = player.AddComponent<Image>();
        playerImg.color = new Color(0.30f, 0.50f, 0.88f);
        RectTransform playerRect = player.GetComponent<RectTransform>();
        playerRect.anchorMin = new Vector2(0.22f, 0.32f);
        playerRect.anchorMax = new Vector2(0.22f, 0.32f);
        playerRect.pivot = new Vector2(0.5f, 0f);
        playerRect.sizeDelta = new Vector2(36, 48);
        playerRect.anchoredPosition = Vector2.zero;
        ui.SetPlayer(playerRect);

        // Player head (child of player, follows automatically)
        GameObject head = new GameObject("PlayerHead");
        head.transform.SetParent(player.transform, false);
        Image headImg = head.AddComponent<Image>();
        headImg.color = new Color(0.90f, 0.72f, 0.54f);
        RectTransform headRect = head.GetComponent<RectTransform>();
        headRect.anchorMin = new Vector2(0.5f, 1f);
        headRect.anchorMax = new Vector2(0.5f, 1f);
        headRect.pivot = new Vector2(0.5f, 0f);
        headRect.sizeDelta = new Vector2(26, 26);
        headRect.anchoredPosition = new Vector2(0, 2);
        ui.SetPlayerHead(headRect);

        // === Slider ===
        CreateSlider(screen.transform, ui);

        // === HUD texts ===
        GameObject jumpCounterObj = CreateText("JumpCounterText", screen.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -22), new Vector2(200, 30),
            "Прыжок: 1/3", 22, TextAlignmentOptions.Center);
        jumpCounterObj.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        ui.SetJumpCounterText(jumpCounterObj.GetComponent<TMP_Text>());

        GameObject scoreObj = CreateText("ScoreText", screen.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -52), new Vector2(200, 26),
            "Очки: 0", 19, TextAlignmentOptions.Center);
        scoreObj.GetComponent<TMP_Text>().color = new Color(1f, 0.82f, 0.28f);
        ui.SetScoreText(scoreObj.GetComponent<TMP_Text>());

        // Instruction above slider bar
        GameObject instrObj = CreateText("InstructionText", screen.transform,
            new Vector2(0.5f, 0.14f), new Vector2(0, 8), new Vector2(400, 28),
            "Нажмите когда индикатор в зелёной зоне!", 15, TextAlignmentOptions.Center);
        instrObj.GetComponent<TMP_Text>().color = Color.white;
        ui.SetInstructionText(instrObj.GetComponent<TMP_Text>());

        return screen;
    }

    private void CreateSlider(Transform screenTransform, JumpUI ui)
    {
        GameObject container = new GameObject("SliderContainer");
        container.transform.SetParent(screenTransform, false);
        RectTransform cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0f);
        cRect.anchorMax = new Vector2(0.5f, 0f);
        cRect.sizeDelta = new Vector2(400, 44);
        cRect.anchoredPosition = new Vector2(0, 22);

        // Background — uses explicit sizeDelta so JumpController can read the width
        GameObject bg = new GameObject("SliderBackground");
        bg.transform.SetParent(container.transform, false);
        Image bgImg = bg.AddComponent<Image>();
        bgImg.color = new Color(0.14f, 0.16f, 0.26f);
        RectTransform bgRect = bg.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.sizeDelta = new Vector2(400, 44);
        bgRect.anchoredPosition = Vector2.zero;
        bg.transform.SetAsFirstSibling();
        ui.SetSliderBackground(bgRect);

        // Zones container
        GameObject zones = new GameObject("ZonesContainer");
        zones.transform.SetParent(container.transform, false);
        RectTransform zonesRect = zones.AddComponent<RectTransform>();
        zonesRect.anchorMin = Vector2.zero;
        zonesRect.anchorMax = Vector2.one;
        zonesRect.sizeDelta = Vector2.zero;
        zones.transform.SetSiblingIndex(1);
        ui.SetZonesContainer(zonesRect);

        // Indicator (thin white line, slightly taller than the bar)
        GameObject indicator = new GameObject("SliderIndicator");
        indicator.transform.SetParent(container.transform, false);
        Image indImg = indicator.AddComponent<Image>();
        indImg.color = Color.white;
        RectTransform indRect = indicator.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.5f, 0.5f);
        indRect.anchorMax = new Vector2(0.5f, 0.5f);
        indRect.sizeDelta = new Vector2(7, 52);
        indRect.anchoredPosition = Vector2.zero;
        indicator.transform.SetAsLastSibling();
        ui.SetSliderIndicator(indRect);
    }

    #endregion

    #region End Screen

    private GameObject CreateEndScreen(Transform parent, JumpUI ui)
    {
        GameObject screen = new GameObject("EndScreen");
        screen.transform.SetParent(parent, false);
        screen.SetActive(false);

        RectTransform screenRect = screen.AddComponent<RectTransform>();
        screenRect.anchorMin = Vector2.zero;
        screenRect.anchorMax = Vector2.one;
        screenRect.sizeDelta = Vector2.zero;

        // Dark overlay
        Image overlay = screen.AddComponent<Image>();
        overlay.color = new Color(0f, 0f, 0f, 0.72f);

        // Card panel
        GameObject panel = CreatePanel("EndPanel", screen.transform,
            new Vector2(0.5f, 0.5f), new Vector2(420, 400),
            Vector2.zero, new Color(0.11f, 0.13f, 0.24f, 0.98f));

        // Result title
        GameObject resultObj = CreateText("ResultText", panel.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(380, 40),
            "Результат", 28, TextAlignmentOptions.Center);
        resultObj.GetComponent<TMP_Text>().fontStyle = FontStyles.Bold;
        ui.SetResultText(resultObj.GetComponent<TMP_Text>());

        // Divider
        CreatePanel("Divider", panel.transform,
            new Vector2(0.5f, 1f), new Vector2(340, 2),
            new Vector2(0, -54), new Color(1f, 1f, 1f, 0.1f));

        // Individual jump scores
        GameObject jumpResultsObj = CreateText("JumpResults", panel.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -80), new Vector2(300, 70),
            "", 16, TextAlignmentOptions.Center);
        jumpResultsObj.GetComponent<TMP_Text>().color = new Color(0.68f, 0.70f, 0.78f);
        ui.SetJumpResultsText(jumpResultsObj.GetComponent<TMP_Text>());

        // Total score
        GameObject totalScoreObj = CreateText("TotalScoreText", panel.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 28), new Vector2(300, 38),
            "Итого: 0", 25, TextAlignmentOptions.Center);
        TMP_Text totalTmp = totalScoreObj.GetComponent<TMP_Text>();
        totalTmp.color = new Color(1f, 0.82f, 0.28f);
        totalTmp.fontStyle = FontStyles.Bold;
        ui.SetTotalScoreText(totalTmp);

        // Bonus slider
        CreateBonusSlider(panel.transform, ui);

        return screen;
    }

    private void CreateBonusSlider(Transform panelTransform, JumpUI ui)
    {
        GameObject container = new GameObject("BonusSliderContainer");
        container.transform.SetParent(panelTransform, false);
        RectTransform cRect = container.AddComponent<RectTransform>();
        cRect.anchorMin = new Vector2(0.5f, 0.5f);
        cRect.anchorMax = new Vector2(0.5f, 0.5f);
        cRect.sizeDelta = new Vector2(380, 50);
        cRect.anchoredPosition = new Vector2(0, -18);

        // Track
        GameObject track = new GameObject("BonusTrack");
        track.transform.SetParent(container.transform, false);
        Image trackImg = track.AddComponent<Image>();
        trackImg.color = new Color(0.18f, 0.20f, 0.32f);
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0.5f);
        trackRect.anchorMax = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(340, 22);
        trackRect.anchoredPosition = Vector2.zero;

        // Left label
        GameObject leftLabelObj = new GameObject("LeftLabel");
        leftLabelObj.transform.SetParent(container.transform, false);
        TMP_Text leftLabel = leftLabelObj.AddComponent<TextMeshProUGUI>();
        leftLabel.text = "Ловкость";
        leftLabel.fontSize = 11;
        leftLabel.color = new Color(0.60f, 0.62f, 0.70f);
        leftLabel.alignment = TextAlignmentOptions.Left;
        RectTransform leftRect = leftLabelObj.GetComponent<RectTransform>();
        leftRect.anchorMin = new Vector2(0f, 0.5f);
        leftRect.anchorMax = new Vector2(0f, 0.5f);
        leftRect.sizeDelta = new Vector2(150, 18);
        leftRect.anchoredPosition = new Vector2(-190, -18);

        // Right label
        GameObject rightLabelObj = new GameObject("RightLabel");
        rightLabelObj.transform.SetParent(container.transform, false);
        TMP_Text rightLabel = rightLabelObj.AddComponent<TextMeshProUGUI>();
        rightLabel.text = "% задания";
        rightLabel.fontSize = 11;
        rightLabel.color = new Color(0.60f, 0.62f, 0.70f);
        rightLabel.alignment = TextAlignmentOptions.Right;
        RectTransform rightRect = rightLabelObj.GetComponent<RectTransform>();
        rightRect.anchorMin = new Vector2(1f, 0.5f);
        rightRect.anchorMax = new Vector2(1f, 0.5f);
        rightRect.sizeDelta = new Vector2(150, 18);
        rightRect.anchoredPosition = new Vector2(190, -18);

        // Indicator
        GameObject indicator = new GameObject("BonusIndicator");
        indicator.transform.SetParent(track.transform, false);
        Image indImg = indicator.AddComponent<Image>();
        indImg.color = new Color(1f, 0.82f, 0.28f);
        RectTransform indRect = indicator.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.5f, 0.5f);
        indRect.anchorMax = new Vector2(0.5f, 0.5f);
        indRect.sizeDelta = new Vector2(8, 28);
        indRect.anchoredPosition = Vector2.zero;

        BonusSliderComponent bonusSlider = container.AddComponent<BonusSliderComponent>();

        // Buttons container (hidden until bonus slider finishes)
        GameObject buttonsContainer = new GameObject("EndScreenButtonsContainer");
        buttonsContainer.transform.SetParent(panelTransform, false);
        RectTransform buttonsRect = buttonsContainer.AddComponent<RectTransform>();
        buttonsRect.anchorMin = new Vector2(0.5f, 0f);
        buttonsRect.anchorMax = new Vector2(0.5f, 0f);
        buttonsRect.sizeDelta = new Vector2(200, 52);
        buttonsRect.anchoredPosition = new Vector2(0, 35);

        GameObject finishBtn = CreateButton("FinishButton", buttonsContainer.transform,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 52),
            new Color(0.22f, 0.70f, 0.40f), "Завершить", 21);
        ui.SetFinishButton(finishBtn.GetComponent<Button>());

        buttonsContainer.SetActive(false);
        bonusSlider.Setup(trackRect, indRect, leftLabel, rightLabel, buttonsContainer);

        ui.SetBonusSliderComponent(bonusSlider);
        ui.SetEndScreenButtonsContainer(buttonsContainer);
    }

    #endregion

    #region UI Helpers

    /// <summary>
    /// Creates a panel with center-anchor positioning.
    /// </summary>
    private GameObject CreatePanel(string name, Transform parent,
        Vector2 anchor, Vector2 size, Vector2 position, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;

        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchor;
        r.anchorMax = anchor;
        r.sizeDelta = size;
        r.anchoredPosition = position;

        return obj;
    }

    /// <summary>
    /// Creates a panel that stretches between two anchor points.
    /// </summary>
    private GameObject CreatePanel(string name, Transform parent,
        Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image img = obj.AddComponent<Image>();
        img.color = color;

        RectTransform r = obj.GetComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.sizeDelta = Vector2.zero;
        r.anchoredPosition = Vector2.zero;

        return obj;
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
        Vector2 position, Vector2 size, Color color, string text, int fontSize = 20)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        Image image = obj.AddComponent<Image>();
        image.color = color;

        Button button = obj.AddComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        cb.selectedColor = Color.white;
        button.colors = cb;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = position;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        TMP_Text tmpText = textObj.AddComponent<TextMeshProUGUI>();
        tmpText.text = text;
        tmpText.fontSize = fontSize;
        tmpText.color = Color.white;
        tmpText.alignment = TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return obj;
    }

    #endregion

    protected override void OnCleanup()
    {
        if (_gameRoot != null)
            Object.Destroy(_gameRoot);
    }

    public void OnGameEnded(int totalScore)
    {
        FinishGame(true, totalScore);
    }

    public void CloseGame()
    {
        FinishGame(false, 0);
    }
}
