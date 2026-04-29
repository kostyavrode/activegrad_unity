using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ActiveGrad.MiniGames;

public static class MiniGamePrefabCreator
{
    private const string OutputPath = "Assets/Resources/MiniGames";

    private static readonly Color ColBg       = new Color(0.07f, 0.08f, 0.15f);
    private static readonly Color ColCard     = new Color(0.10f, 0.12f, 0.22f, 0.97f);
    private static readonly Color ColDivider  = new Color(1f, 1f, 1f, 0.08f);
    private static readonly Color ColGround   = new Color(0.18f, 0.42f, 0.20f);
    private static readonly Color ColGrass    = new Color(0.28f, 0.58f, 0.26f);
    private static readonly Color ColPlayer   = new Color(0.28f, 0.48f, 0.86f);
    private static readonly Color ColSkin     = new Color(0.90f, 0.72f, 0.54f);
    private static readonly Color ColSliderBg = new Color(0.13f, 0.15f, 0.25f);
    private static readonly Color ColGold     = new Color(1.00f, 0.80f, 0.26f);
    private static readonly Color ColTextSub  = new Color(0.67f, 0.69f, 0.78f);
    private static readonly Color ColBtnGreen = new Color(0.20f, 0.68f, 0.38f);
    private static readonly Color ColBtnRed   = new Color(0.68f, 0.22f, 0.26f);
    private static readonly Color ColBtnBlue  = new Color(0.20f, 0.48f, 0.78f);
    private static readonly Color ColOverlay  = new Color(0f, 0f, 0f, 0.72f);
    private static readonly Color ColTrackBg  = new Color(0.17f, 0.19f, 0.30f);

    [MenuItem("ActiveGrad/MiniGames/Create Jump Game Prefab")]
    public static void CreateJumpGamePrefab()
    {
        EnsureDirectory(OutputPath);

        var root = CreateCanvas("JumpGame");
        var ui = root.AddComponent<JumpUI>();
        var so = new SerializedObject(ui);

        SetProp(so, "_background", CreateStretchImage("Background", root.transform, ColBg).GetComponent<Image>());

        var startScreen = BuildJumpStartScreen(root.transform, so);
        SetProp(so, "_startScreen", startScreen);

        var gameScreen = BuildJumpGameScreen(root.transform, so);
        SetProp(so, "_gameScreen", gameScreen);

        var endScreen = BuildJumpEndScreen(root.transform, so);
        SetProp(so, "_endScreen", endScreen);

        so.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, OutputPath + "/JumpGame.prefab");
        Object.DestroyImmediate(root);

        Debug.Log("[MiniGamePrefabCreator] JumpGame.prefab создан.");
    }

    [MenuItem("ActiveGrad/MiniGames/Create Train Path Game Prefab")]
    public static void CreateTrainPathGamePrefab()
    {
        EnsureDirectory(OutputPath);

        var root = CreateCanvas("TrainPathGame");
        var ui = root.AddComponent<TrainPathUI>();
        var so = new SerializedObject(ui);

        SetProp(so, "_background", CreateStretchImage("Background", root.transform, new Color(0.09f, 0.10f, 0.18f)).GetComponent<Image>());

        var startScreen = BuildTrainStartScreen(root.transform, so);
        SetProp(so, "_startScreen", startScreen);

        var gameScreen = BuildTrainGameScreen(root.transform, so);
        SetProp(so, "_gameScreen", gameScreen);

        var endScreen = BuildTrainEndScreen(root.transform, so);
        SetProp(so, "_endScreen", endScreen);

        so.ApplyModifiedPropertiesWithoutUndo();
        SavePrefab(root, OutputPath + "/TrainPathGame.prefab");
        Object.DestroyImmediate(root);

        Debug.Log("[MiniGamePrefabCreator] TrainPathGame.prefab создан.");
    }

    #region Jump Game Screens

    private static GameObject BuildJumpStartScreen(Transform parent, SerializedObject so)
    {
        var screen = CreateStretch("StartScreen", parent);

        var card = CreatePanel("Card", screen.transform,
            Vector2.one * 0.5f, new Vector2(400, 440), Vector2.zero, ColCard);

        var title = CreateTMP("Title", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -36), new Vector2(360, 52),
            "ПРЫЖКИ", 36, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        SetProp(so, "_titleText", title);

        CreatePanel("Divider", card.transform,
            new Vector2(0.5f, 1f), new Vector2(320, 2), new Vector2(0, -74), ColDivider);

        var rules = CreateTMP("Rules", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 34), new Vector2(340, 140),
            "Выполните 3 прыжка!\n\nНажмите на экран или Space,\nкогда индикатор окажется\nв зелёной зоне —\nдля максимального результата.",
            15, TextAlignmentOptions.Center, ColTextSub);
        SetProp(so, "_rulesText", rules);

        var skill = CreateTMP("SkillText", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, -60), new Vector2(260, 32),
            "Ловкость: 1", 18, TextAlignmentOptions.Center, ColGold);
        SetProp(so, "_skillText", skill);

        var startBtn = CreateButton("StartButton", card.transform,
            new Vector2(0.5f, 0f), new Vector2(0, 48), new Vector2(230, 54),
            ColBtnGreen, "Начать игру", 22);
        SetProp(so, "_startButton", startBtn.GetComponent<Button>());

        var closeBtn = CreateButton("CloseButton", screen.transform,
            new Vector2(1f, 1f), new Vector2(-36, -30), new Vector2(52, 36),
            ColBtnRed, "✕", 20);
        SetProp(so, "_closeButton", closeBtn.GetComponent<Button>());

        return screen;
    }

    private static GameObject BuildJumpGameScreen(Transform parent, SerializedObject so)
    {
        var screen = CreateStretch("GameScreen", parent);
        screen.SetActive(false);

        CreateStretchAnchors("BottomBar", screen.transform,
            new Vector2(0f, 0f), new Vector2(1f, 0.13f), new Color(0.05f, 0.06f, 0.12f));
        CreateStretchAnchors("Ground", screen.transform,
            new Vector2(0f, 0.13f), new Vector2(1f, 0.30f), ColGround);
        CreateStretchAnchors("GrassStrip", screen.transform,
            new Vector2(0f, 0.29f), new Vector2(1f, 0.32f), ColGrass);

        var player = new GameObject("Player");
        player.transform.SetParent(screen.transform, false);
        player.AddComponent<Image>().color = ColPlayer;
        var pRect = player.GetComponent<RectTransform>();
        pRect.anchorMin = new Vector2(0.22f, 0.32f);
        pRect.anchorMax = new Vector2(0.22f, 0.32f);
        pRect.pivot = new Vector2(0.5f, 0f);
        pRect.sizeDelta = new Vector2(36, 48);
        pRect.anchoredPosition = Vector2.zero;
        SetProp(so, "_player", pRect);

        var head = new GameObject("PlayerHead");
        head.transform.SetParent(player.transform, false);
        head.AddComponent<Image>().color = ColSkin;
        var hRect = head.GetComponent<RectTransform>();
        hRect.anchorMin = new Vector2(0.5f, 1f);
        hRect.anchorMax = new Vector2(0.5f, 1f);
        hRect.pivot = new Vector2(0.5f, 0f);
        hRect.sizeDelta = new Vector2(28, 28);
        hRect.anchoredPosition = new Vector2(0, 2);
        SetProp(so, "_playerHead", hRect);

        var sliderContainer = new GameObject("SliderContainer");
        sliderContainer.transform.SetParent(screen.transform, false);
        var scRect = sliderContainer.AddComponent<RectTransform>();
        scRect.anchorMin = new Vector2(0.5f, 0f);
        scRect.anchorMax = new Vector2(0.5f, 0f);
        scRect.sizeDelta = new Vector2(420, 46);
        scRect.anchoredPosition = new Vector2(0, 23);

        var sliderBg = new GameObject("SliderBackground");
        sliderBg.transform.SetParent(sliderContainer.transform, false);
        sliderBg.AddComponent<Image>().color = ColSliderBg;
        var sbRect = sliderBg.GetComponent<RectTransform>();
        sbRect.anchorMin = new Vector2(0.5f, 0.5f);
        sbRect.anchorMax = new Vector2(0.5f, 0.5f);
        sbRect.sizeDelta = new Vector2(420, 46);
        sbRect.anchoredPosition = Vector2.zero;
        sliderBg.transform.SetAsFirstSibling();
        SetProp(so, "_sliderBackground", sbRect);

        var zones = new GameObject("ZonesContainer");
        zones.transform.SetParent(sliderContainer.transform, false);
        var zRect = zones.AddComponent<RectTransform>();
        zRect.anchorMin = Vector2.zero;
        zRect.anchorMax = Vector2.one;
        zRect.sizeDelta = Vector2.zero;
        zones.transform.SetSiblingIndex(1);
        SetProp(so, "_zonesContainer", zRect);

        var indicator = new GameObject("SliderIndicator");
        indicator.transform.SetParent(sliderContainer.transform, false);
        indicator.AddComponent<Image>().color = Color.white;
        var iRect = indicator.GetComponent<RectTransform>();
        iRect.anchorMin = new Vector2(0.5f, 0.5f);
        iRect.anchorMax = new Vector2(0.5f, 0.5f);
        iRect.sizeDelta = new Vector2(6, 54);
        iRect.anchoredPosition = Vector2.zero;
        indicator.transform.SetAsLastSibling();
        SetProp(so, "_sliderIndicator", iRect);

        var jumpCounter = CreateTMP("JumpCounterText", screen.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -24), new Vector2(220, 34),
            "Прыжок: 1/3", 24, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        SetProp(so, "_jumpCounterText", jumpCounter);

        var score = CreateTMP("ScoreText", screen.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -58), new Vector2(200, 28),
            "Очки: 0", 20, TextAlignmentOptions.Center, ColGold);
        SetProp(so, "_scoreText", score);

        var instr = CreateTMP("InstructionText", screen.transform,
            new Vector2(0.5f, 0.13f), new Vector2(0, 10), new Vector2(420, 30),
            "Нажмите, когда индикатор в зелёной зоне!", 15, TextAlignmentOptions.Center, Color.white);
        SetProp(so, "_instructionText", instr);

        return screen;
    }

    private static GameObject BuildJumpEndScreen(Transform parent, SerializedObject so)
    {
        var screen = CreateStretch("EndScreen", parent);
        screen.SetActive(false);
        screen.AddComponent<Image>().color = ColOverlay;

        var card = CreatePanel("Card", screen.transform,
            Vector2.one * 0.5f, new Vector2(440, 480), Vector2.zero, ColCard);

        var result = CreateTMP("ResultText", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(400, 46),
            "Результат", 30, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        SetProp(so, "_resultText", result);

        CreatePanel("Divider", card.transform,
            new Vector2(0.5f, 1f), new Vector2(360, 2), new Vector2(0, -62), ColDivider);

        var jumpResults = CreateTMP("JumpResults", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -96), new Vector2(320, 82),
            "", 16, TextAlignmentOptions.Center, ColTextSub);
        SetProp(so, "_jumpResultsText", jumpResults);

        var totalScore = CreateTMP("TotalScoreText", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 42), new Vector2(320, 44),
            "Итого: 0", 26, TextAlignmentOptions.Center, ColGold, FontStyles.Bold);
        SetProp(so, "_totalScoreText", totalScore);

        var bonusContainer = new GameObject("BonusSliderContainer");
        bonusContainer.transform.SetParent(card.transform, false);
        var bcRect = bonusContainer.AddComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(0.5f, 0.5f);
        bcRect.anchorMax = new Vector2(0.5f, 0.5f);
        bcRect.sizeDelta = new Vector2(380, 58);
        bcRect.anchoredPosition = new Vector2(0, -14);

        var track = new GameObject("BonusTrack");
        track.transform.SetParent(bonusContainer.transform, false);
        track.AddComponent<Image>().color = ColTrackBg;
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0.5f);
        trackRect.anchorMax = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(340, 24);
        trackRect.anchoredPosition = Vector2.zero;

        var leftLabel = CreateTMP("LeftLabel", bonusContainer.transform,
            new Vector2(0f, 0.5f), new Vector2(-190, -20), new Vector2(150, 18),
            "Ловкость", 11, TextAlignmentOptions.Left, ColTextSub);

        var rightLabel = CreateTMP("RightLabel", bonusContainer.transform,
            new Vector2(1f, 0.5f), new Vector2(190, -20), new Vector2(150, 18),
            "% задания", 11, TextAlignmentOptions.Right, ColTextSub);

        var indGo = new GameObject("BonusIndicator");
        indGo.transform.SetParent(track.transform, false);
        indGo.AddComponent<Image>().color = ColGold;
        var indRect = indGo.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.5f, 0.5f);
        indRect.anchorMax = new Vector2(0.5f, 0.5f);
        indRect.sizeDelta = new Vector2(8, 32);
        indRect.anchoredPosition = Vector2.zero;

        var buttonsContainer = new GameObject("EndScreenButtonsContainer");
        buttonsContainer.transform.SetParent(card.transform, false);
        var btnCRect = buttonsContainer.AddComponent<RectTransform>();
        btnCRect.anchorMin = new Vector2(0.5f, 0f);
        btnCRect.anchorMax = new Vector2(0.5f, 0f);
        btnCRect.sizeDelta = new Vector2(230, 54);
        btnCRect.anchoredPosition = new Vector2(0, 40);
        buttonsContainer.SetActive(false);
        SetProp(so, "_endScreenButtonsContainer", buttonsContainer);

        var finishBtn = CreateButton("FinishButton", buttonsContainer.transform,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(230, 54),
            ColBtnGreen, "Завершить", 21);
        SetProp(so, "_finishButton", finishBtn.GetComponent<Button>());

        var bonusSlider = bonusContainer.AddComponent<BonusSliderComponent>();
        var bso = new SerializedObject(bonusSlider);
        SetProp(bso, "_sliderTrack", trackRect);
        SetProp(bso, "_indicator", indRect);
        SetProp(bso, "_leftLabel", leftLabel);
        SetProp(bso, "_rightLabel", rightLabel);
        SetProp(bso, "_finishButtonContainer", buttonsContainer);
        bso.ApplyModifiedPropertiesWithoutUndo();
        SetProp(so, "_bonusSliderComponent", bonusSlider);

        return screen;
    }

    #endregion

    #region Train Path Game Screens

    private static GameObject BuildTrainStartScreen(Transform parent, SerializedObject so)
    {
        var screen = CreateStretch("StartScreen", parent);

        var card = CreatePanel("Card", screen.transform,
            Vector2.one * 0.5f, new Vector2(440, 420), Vector2.zero, ColCard);

        CreateTMP("Title", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -34), new Vector2(400, 52),
            "ПУТЬ ПОЕЗДА", 34, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);

        CreatePanel("Divider", card.transform,
            new Vector2(0.5f, 1f), new Vector2(360, 2), new Vector2(0, -72), ColDivider);

        CreateTMP("Rules", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 30), new Vector2(380, 130),
            "Найдите оптимальный маршрут!\n\nПостройте кратчайший путь\nот начальной до конечной станции,\nпосещая все промежуточные.",
            15, TextAlignmentOptions.Center, ColTextSub);

        var skill = CreateTMP("SkillText", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, -58), new Vector2(260, 32),
            "Интеллект: 1", 18, TextAlignmentOptions.Center, ColGold);
        SetProp(so, "_skillText", skill);

        var startBtn = CreateButton("StartButton", card.transform,
            new Vector2(0.5f, 0f), new Vector2(0, 44), new Vector2(230, 54),
            ColBtnGreen, "Начать игру", 22);
        SetProp(so, "_startButton", startBtn.GetComponent<Button>());

        var closeBtn = CreateButton("CloseButton", screen.transform,
            new Vector2(1f, 1f), new Vector2(-36, -30), new Vector2(52, 36),
            ColBtnRed, "✕", 20);
        SetProp(so, "_closeButton", closeBtn.GetComponent<Button>());

        return screen;
    }

    private static GameObject BuildTrainGameScreen(Transform parent, SerializedObject so)
    {
        var screen = CreateStretch("GameScreen", parent);
        screen.SetActive(false);

        var map = new GameObject("MapContainer");
        map.transform.SetParent(screen.transform, false);
        var mapRect = map.AddComponent<RectTransform>();
        mapRect.anchorMin = new Vector2(0.5f, 0.5f);
        mapRect.anchorMax = new Vector2(0.5f, 0.5f);
        mapRect.sizeDelta = new Vector2(800, 500);
        mapRect.anchoredPosition = Vector2.zero;
        SetProp(so, "_mapContainer", mapRect);

        var train = new GameObject("Train");
        train.transform.SetParent(screen.transform, false);
        train.AddComponent<Image>().color = new Color(0.85f, 0.52f, 0.18f);
        var trainRect = train.GetComponent<RectTransform>();
        trainRect.anchorMin = new Vector2(0.5f, 0.5f);
        trainRect.anchorMax = new Vector2(0.5f, 0.5f);
        trainRect.sizeDelta = new Vector2(32, 32);
        trainRect.anchoredPosition = Vector2.zero;
        SetProp(so, "_train", trainRect);

        var timeText = CreateTMP("TimeText", screen.transform,
            new Vector2(0f, 1f), new Vector2(16, -18), new Vector2(180, 32),
            "Время: 0.0с", 20, TextAlignmentOptions.TopLeft, Color.white);
        SetProp(so, "_timeText", timeText);

        var pathInfo = CreateTMP("PathInfoText", screen.transform,
            new Vector2(1f, 1f), new Vector2(-16, -18), new Vector2(180, 32),
            "Станций: 0/0", 20, TextAlignmentOptions.TopRight, Color.white);
        SetProp(so, "_pathInfoText", pathInfo);

        return screen;
    }

    private static GameObject BuildTrainEndScreen(Transform parent, SerializedObject so)
    {
        var screen = CreateStretch("EndScreen", parent);
        screen.SetActive(false);
        screen.AddComponent<Image>().color = ColOverlay;

        var card = CreatePanel("Card", screen.transform,
            Vector2.one * 0.5f, new Vector2(480, 500), Vector2.zero, ColCard);

        var result = CreateTMP("ResultText", card.transform,
            new Vector2(0.5f, 1f), new Vector2(0, -30), new Vector2(440, 46),
            "Результат", 30, TextAlignmentOptions.Center, Color.white, FontStyles.Bold);
        SetProp(so, "_resultText", result);

        CreatePanel("Divider", card.transform,
            new Vector2(0.5f, 1f), new Vector2(400, 2), new Vector2(0, -62), ColDivider);

        var playerTime = CreateTMP("PlayerTimeText", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 80), new Vector2(380, 38),
            "Ваше время: 0.0с", 22, TextAlignmentOptions.Center, Color.white);
        SetProp(so, "_playerTimeText", playerTime);

        var optimalTime = CreateTMP("OptimalTimeText", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, 38), new Vector2(380, 30),
            "Оптимальное: 0.0с", 18, TextAlignmentOptions.Center, ColTextSub);
        SetProp(so, "_optimalTimeText", optimalTime);

        var scoreText = CreateTMP("ScoreText", card.transform,
            new Vector2(0.5f, 0.5f), new Vector2(0, -4), new Vector2(320, 44),
            "Очки: 0", 26, TextAlignmentOptions.Center, ColGold, FontStyles.Bold);
        SetProp(so, "_scoreText", scoreText);

        var bonusContainer = new GameObject("BonusSliderContainer");
        bonusContainer.transform.SetParent(card.transform, false);
        var bcRect = bonusContainer.AddComponent<RectTransform>();
        bcRect.anchorMin = new Vector2(0.5f, 0.5f);
        bcRect.anchorMax = new Vector2(0.5f, 0.5f);
        bcRect.sizeDelta = new Vector2(420, 58);
        bcRect.anchoredPosition = new Vector2(0, -70);

        var track = new GameObject("BonusTrack");
        track.transform.SetParent(bonusContainer.transform, false);
        track.AddComponent<Image>().color = ColTrackBg;
        var trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0.5f, 0.5f);
        trackRect.anchorMax = new Vector2(0.5f, 0.5f);
        trackRect.sizeDelta = new Vector2(380, 24);
        trackRect.anchoredPosition = Vector2.zero;
        SetProp(so, "_bonusSliderTrack", trackRect);

        var leftLabel = CreateTMP("LeftLabel", bonusContainer.transform,
            new Vector2(0f, 0.5f), new Vector2(-210, -20), new Vector2(180, 18),
            "Интеллект", 11, TextAlignmentOptions.Left, ColTextSub);
        SetProp(so, "_bonusLeftLabel", leftLabel);

        var rightLabel = CreateTMP("RightLabel", bonusContainer.transform,
            new Vector2(1f, 0.5f), new Vector2(210, -20), new Vector2(180, 18),
            "% задания", 11, TextAlignmentOptions.Right, ColTextSub);
        SetProp(so, "_bonusRightLabel", rightLabel);

        var indGo = new GameObject("BonusIndicator");
        indGo.transform.SetParent(track.transform, false);
        indGo.AddComponent<Image>().color = ColGold;
        var indRect = indGo.GetComponent<RectTransform>();
        indRect.anchorMin = new Vector2(0.5f, 0.5f);
        indRect.anchorMax = new Vector2(0.5f, 0.5f);
        indRect.sizeDelta = new Vector2(8, 32);
        indRect.anchoredPosition = Vector2.zero;
        SetProp(so, "_bonusSliderIndicator", indRect);

        var buttonsContainer = new GameObject("EndScreenButtonsContainer");
        buttonsContainer.transform.SetParent(card.transform, false);
        var btnCRect = buttonsContainer.AddComponent<RectTransform>();
        btnCRect.anchorMin = new Vector2(0.5f, 0f);
        btnCRect.anchorMax = new Vector2(0.5f, 0f);
        btnCRect.sizeDelta = new Vector2(360, 54);
        btnCRect.anchoredPosition = new Vector2(0, 38);
        buttonsContainer.SetActive(false);
        SetProp(so, "_endScreenButtonsContainer", buttonsContainer);

        var restartBtn = CreateButton("RestartButton", buttonsContainer.transform,
            new Vector2(0.5f, 0.5f), new Vector2(-90, 0), new Vector2(168, 54),
            ColBtnBlue, "Заново", 20);
        SetProp(so, "_restartButton", restartBtn.GetComponent<Button>());

        var finishBtn = CreateButton("FinishButton", buttonsContainer.transform,
            new Vector2(0.5f, 0.5f), new Vector2(90, 0), new Vector2(168, 54),
            ColBtnGreen, "Завершить", 20);
        SetProp(so, "_finishButton", finishBtn.GetComponent<Button>());

        var bonusSlider = bonusContainer.AddComponent<BonusSliderComponent>();
        var bso = new SerializedObject(bonusSlider);
        SetProp(bso, "_sliderTrack", trackRect);
        SetProp(bso, "_indicator", indRect);
        SetProp(bso, "_leftLabel", leftLabel);
        SetProp(bso, "_rightLabel", rightLabel);
        SetProp(bso, "_finishButtonContainer", buttonsContainer);
        bso.ApplyModifiedPropertiesWithoutUndo();
        SetProp(so, "_bonusSliderComponent", bonusSlider);

        return screen;
    }

    #endregion

    #region Helpers

    private static GameObject CreateCanvas(string name)
    {
        var go = new GameObject(name);
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(800, 600);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return go;
    }

    private static GameObject CreateStretch(string name, Transform parent)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rect = go.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        return go;
    }

    private static GameObject CreateStretchImage(string name, Transform parent, Color color)
    {
        var go = CreateStretch(name, parent);
        go.AddComponent<Image>().color = color;
        return go;
    }

    private static void CreateStretchAnchors(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.sizeDelta = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 anchor, Vector2 size, Vector2 pos, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return go;
    }

    private static TMP_Text CreateTMP(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size,
        string text, float fontSize, TextAlignmentOptions alignment, Color color, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.fontStyle = style;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;
        return tmp;
    }

    private static GameObject CreateButton(string name, Transform parent, Vector2 anchor, Vector2 pos, Vector2 size, Color color, string label, int fontSize)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<Image>().color = color;
        var btn = go.AddComponent<Button>();
        var cb = btn.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(0.88f, 0.88f, 0.88f);
        cb.pressedColor = new Color(0.72f, 0.72f, 0.72f);
        cb.selectedColor = Color.white;
        btn.colors = cb;
        var rect = go.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.sizeDelta = size;
        rect.anchoredPosition = pos;

        var textGo = new GameObject("Text");
        textGo.transform.SetParent(go.transform, false);
        var tmp = textGo.AddComponent<TextMeshProUGUI>();
        tmp.text = label;
        tmp.fontSize = fontSize;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontStyle = FontStyles.Bold;
        var tRect = textGo.GetComponent<RectTransform>();
        tRect.anchorMin = Vector2.zero;
        tRect.anchorMax = Vector2.one;
        tRect.sizeDelta = Vector2.zero;
        return go;
    }

    private static void SetProp(SerializedObject so, string propName, Object value)
    {
        var prop = so.FindProperty(propName);
        if (prop != null)
            prop.objectReferenceValue = value;
        else
            Debug.LogWarning($"[MiniGamePrefabCreator] Property not found: {propName}");
    }

    private static void SavePrefab(GameObject go, string path)
    {
        PrefabUtility.SaveAsPrefabAsset(go, path);
        AssetDatabase.Refresh();
    }

    private static void EnsureDirectory(string path)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            AssetDatabase.Refresh();
        }
    }

    #endregion
}
