using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using ActiveGrad.MiniGames;

public static class FlappyBirdPrefabBuilder
{
    private const string PrefabPath = "Assets/Resources/MiniGames/FlappyBirdGame.prefab";

    [MenuItem("Tools/Build FlappyBird Prefab")]
    public static void Build()
    {
        using var scope = new PrefabUtility.EditPrefabContentsScope(PrefabPath);
        var root = scope.prefabContentsRoot;

        while (root.transform.childCount > 0)
            Object.DestroyImmediate(root.transform.GetChild(0).gameObject);

        SetStretch(EnsureRect(root));
        var ui = GetOrAdd<FlappyBirdUI>(root);
        GetOrAdd<FlappyBirdController>(root);

        // ── Фон ─────────────────────────────────────────────────────
        var bgImg = CreateImageGO(root.transform, "Background", new Color(0.45f, 0.76f, 0.98f));
        SetStretch(bgImg.rectTransform);

        // ── Экраны ──────────────────────────────────────────────────
        var startGO = CreateStretchGO(root.transform, "StartScreen");
        var gameGO  = CreateStretchGO(root.transform, "GameScreen");
        var endGO   = CreateStretchGO(root.transform, "EndScreen");

        startGO.SetActive(true);
        gameGO.SetActive(false);
        endGO.SetActive(false);

        // ── Контент экранов ─────────────────────────────────────────
        var (startBtn, closeBtn, skillTxt)                   = BuildStartScreen(startGO.transform);
        var (pipesRT, birdRT, scoreTxt, countdownTxt)        = BuildGameScreen(gameGO.transform);
        var (resultTxt, finalTxt, bestTxt, medalTxt, slider,
             trackRT, indicatorRT, leftLbl, rightLbl,
             buttonsGO, restartBtn, finishBtn)               = BuildEndScreen(endGO.transform);

        buttonsGO.SetActive(false);

        // ── Flash overlay — поверх всего ────────────────────────────
        var flashImg = CreateImageGO(root.transform, "FlashOverlay", Color.clear);
        SetStretch(flashImg.rectTransform);
        flashImg.raycastTarget = false;

        // ── Прописываем ссылки FlappyBirdUI ─────────────────────────
        var so = new SerializedObject(ui);
        so.FindProperty("_startScreen").objectReferenceValue               = startGO;
        so.FindProperty("_gameScreen").objectReferenceValue                = gameGO;
        so.FindProperty("_endScreen").objectReferenceValue                 = endGO;
        so.FindProperty("_startButton").objectReferenceValue               = startBtn;
        so.FindProperty("_closeButton").objectReferenceValue               = closeBtn;
        so.FindProperty("_skillText").objectReferenceValue                 = skillTxt;
        so.FindProperty("_pipesContainer").objectReferenceValue            = pipesRT;
        so.FindProperty("_bird").objectReferenceValue                      = birdRT;
        so.FindProperty("_scoreText").objectReferenceValue                 = scoreTxt;
        so.FindProperty("_countdownText").objectReferenceValue             = countdownTxt;
        so.FindProperty("_resultText").objectReferenceValue                = resultTxt;
        so.FindProperty("_finalScoreText").objectReferenceValue            = finalTxt;
        so.FindProperty("_bestScoreText").objectReferenceValue             = bestTxt;
        so.FindProperty("_medalText").objectReferenceValue                 = medalTxt;
        so.FindProperty("_bonusSliderTrack").objectReferenceValue          = trackRT;
        so.FindProperty("_bonusSliderIndicator").objectReferenceValue      = indicatorRT;
        so.FindProperty("_bonusLeftLabel").objectReferenceValue            = leftLbl;
        so.FindProperty("_bonusRightLabel").objectReferenceValue           = rightLbl;
        so.FindProperty("_endScreenButtonsContainer").objectReferenceValue = buttonsGO;
        so.FindProperty("_bonusSliderComponent").objectReferenceValue      = slider;
        so.FindProperty("_restartButton").objectReferenceValue             = restartBtn;
        so.FindProperty("_finishButton").objectReferenceValue              = finishBtn;
        so.FindProperty("_background").objectReferenceValue                = bgImg;
        so.FindProperty("_flashOverlay").objectReferenceValue              = flashImg;
        so.ApplyModifiedProperties();

        // ── Прописываем ссылки BonusSliderComponent ─────────────────
        var ss = new SerializedObject(slider);
        ss.FindProperty("_sliderTrack").objectReferenceValue          = trackRT;
        ss.FindProperty("_indicator").objectReferenceValue            = indicatorRT;
        ss.FindProperty("_leftLabel").objectReferenceValue            = leftLbl;
        ss.FindProperty("_rightLabel").objectReferenceValue           = rightLbl;
        ss.FindProperty("_finishButtonContainer").objectReferenceValue = buttonsGO;
        ss.ApplyModifiedProperties();

        Debug.Log("[FlappyBirdPrefabBuilder] Префаб построен успешно ✅");
    }

    // ================================================================
    // START SCREEN
    // ================================================================
    static (Button start, Button close, TMP_Text skill) BuildStartScreen(Transform parent)
    {
        var card = CreateCard(parent, "Card", 370f, 480f, 0f, 0f);

        CreateLabel(card.transform, "Title",    "🐦 Flappy Bird",          36, FontStyles.Bold,   new Vector2(0f, 160f), new Vector2(340f, 50f));
        CreateLabel(card.transform, "Subtitle", "Нажимай — лети!\nИзбегай труб.", 20, FontStyles.Normal, new Vector2(0f, 90f),  new Vector2(310f, 60f));

        var skillTxt = CreateLabel(card.transform, "SkillText", "Ловкость: —", 22, FontStyles.Normal, new Vector2(0f, 20f),  new Vector2(310f, 36f));

        CreateDivider(card.transform, new Vector2(0f, -18f), new Vector2(300f, 2f));

        var startBtn = CreateButton(card.transform, "StartButton", "▶  Начать",  new Color(0.22f, 0.72f, 0.28f), new Vector2(0f, -80f),  new Vector2(280f, 58f));
        var closeBtn = CreateButton(card.transform, "CloseButton", "✕  Закрыть", new Color(0.55f, 0.55f, 0.60f), new Vector2(0f, -152f), new Vector2(280f, 58f));

        return (startBtn, closeBtn, skillTxt);
    }

    // ================================================================
    // GAME SCREEN
    // ================================================================
    static (RectTransform pipes, RectTransform bird, TMP_Text score, TMP_Text countdown)
        BuildGameScreen(Transform parent)
    {
        // Область игры с маской
        var areaGO  = new GameObject("PipesContainer");
        areaGO.transform.SetParent(parent, false);
        var areaImg = areaGO.AddComponent<Image>();
        areaImg.color = new Color(0f, 0f, 0f, 0f);
        areaGO.AddComponent<Mask>().showMaskGraphic = false;
        var areaRT  = areaGO.GetComponent<RectTransform>();
        areaRT.anchorMin = areaRT.anchorMax = new Vector2(0.5f, 0.5f);
        areaRT.pivot     = new Vector2(0.5f, 0.5f);
        areaRT.sizeDelta = new Vector2(430f, 750f);
        areaRT.anchoredPosition = Vector2.zero;

        // Птица — последний дочерний (рендерится поверх труб)
        var birdGO  = new GameObject("Bird");
        birdGO.transform.SetParent(areaGO.transform, false);
        var birdImg = birdGO.AddComponent<Image>();
        birdImg.color = Color.yellow;
        var birdRT  = birdGO.GetComponent<RectTransform>();
        birdRT.anchorMin = birdRT.anchorMax = new Vector2(0.5f, 0.5f);
        birdRT.pivot     = new Vector2(0.5f, 0.5f);
        birdRT.sizeDelta = new Vector2(50f, 50f);
        birdRT.anchoredPosition = new Vector2(-100f, 0f);
        birdGO.transform.SetAsLastSibling();

        // Счёт (вверху экрана, поверх области игры)
        var scoreGO  = new GameObject("ScoreText");
        scoreGO.transform.SetParent(parent, false);
        var scoreTxt = scoreGO.AddComponent<TextMeshProUGUI>();
        scoreTxt.text      = "🐦 0";
        scoreTxt.fontSize  = 42;
        scoreTxt.fontStyle = FontStyles.Bold;
        scoreTxt.alignment = TextAlignmentOptions.Center;
        scoreTxt.color     = Color.white;
        var scoreRT = scoreGO.GetComponent<RectTransform>();
        scoreRT.anchorMin = new Vector2(0f, 1f);
        scoreRT.anchorMax = new Vector2(1f, 1f);
        scoreRT.pivot     = new Vector2(0.5f, 1f);
        scoreRT.sizeDelta = new Vector2(0f, 60f);
        scoreRT.anchoredPosition = new Vector2(0f, -30f);

        // Обратный отсчёт — по центру, поверх всего GameScreen
        var cdGO  = new GameObject("CountdownText");
        cdGO.transform.SetParent(parent, false);
        var cdTxt = cdGO.AddComponent<TextMeshProUGUI>();
        cdTxt.text      = "";
        cdTxt.fontSize  = 100;
        cdTxt.fontStyle = FontStyles.Bold;
        cdTxt.alignment = TextAlignmentOptions.Center;
        cdTxt.color     = Color.white;
        cdTxt.enabled   = false;
        var cdRT = cdGO.GetComponent<RectTransform>();
        cdRT.anchorMin = cdRT.anchorMax = new Vector2(0.5f, 0.5f);
        cdRT.pivot     = new Vector2(0.5f, 0.5f);
        cdRT.sizeDelta = new Vector2(300f, 150f);
        cdRT.anchoredPosition = Vector2.zero;

        return (areaRT, birdRT, scoreTxt, cdTxt);
    }

    // ================================================================
    // END SCREEN
    // ================================================================
    static (TMP_Text result, TMP_Text finalScore,
            TMP_Text bestScore, TMP_Text medal,
            BonusSliderComponent slider,
            RectTransform track, RectTransform indicator,
            TMP_Text leftLbl, TMP_Text rightLbl,
            GameObject buttonsGO, Button restart, Button finish)
        BuildEndScreen(Transform parent)
    {
        var card = CreateCard(parent, "Card", 370f, 600f, 0f, 0f);

        var medalTxt  = CreateLabel(card.transform, "MedalText",      "—",             48, FontStyles.Bold,   new Vector2(0f, 240f), new Vector2(340f, 60f));
        var resultTxt = CreateLabel(card.transform, "ResultText",     "Игра окончена!", 28, FontStyles.Bold,   new Vector2(0f, 185f), new Vector2(340f, 42f));
        var finalTxt  = CreateLabel(card.transform, "FinalScoreText", "Пролетел: 0 труб\nОчки: 0", 20, FontStyles.Normal, new Vector2(0f, 120f), new Vector2(340f, 56f));
        var bestTxt   = CreateLabel(card.transform, "BestScoreText",  "",              18, FontStyles.Normal, new Vector2(0f, 78f),  new Vector2(340f, 30f));
        bestTxt.color = new Color(1f, 0.85f, 0.3f);

        // ── BonusSlider ─────────────────────────────────────────────
        var sliderGO = new GameObject("BonusSlider");
        sliderGO.transform.SetParent(card.transform, false);
        var sliderRT = sliderGO.GetComponent<RectTransform>() ?? sliderGO.AddComponent<RectTransform>();
        sliderRT.anchorMin = sliderRT.anchorMax = new Vector2(0.5f, 0.5f);
        sliderRT.pivot     = new Vector2(0.5f, 0.5f);
        sliderRT.sizeDelta = new Vector2(340f, 120f);
        sliderRT.anchoredPosition = new Vector2(0f, 10f);
        var sliderComp = sliderGO.AddComponent<BonusSliderComponent>();

        var bonusTitleTxt = CreateLabel(sliderGO.transform, "BonusTitle", "Бонус ловкости", 18, FontStyles.Normal, new Vector2(0f, 46f), new Vector2(300f, 30f));
        bonusTitleTxt.color = new Color(0.8f, 0.8f, 0.8f);

        // Track
        var trackGO  = new GameObject("Track");
        trackGO.transform.SetParent(sliderGO.transform, false);
        var trackImg = trackGO.AddComponent<Image>();
        trackImg.color = new Color(0.25f, 0.25f, 0.30f);
        var trackRT  = trackGO.GetComponent<RectTransform>();
        trackRT.anchorMin = trackRT.anchorMax = new Vector2(0.5f, 0.5f);
        trackRT.pivot     = new Vector2(0.5f, 0.5f);
        trackRT.sizeDelta = new Vector2(300f, 18f);
        trackRT.anchoredPosition = Vector2.zero;

        // Indicator
        var indicGO  = new GameObject("Indicator");
        indicGO.transform.SetParent(trackGO.transform, false);
        var indicImg = indicGO.AddComponent<Image>();
        indicImg.color = new Color(1f, 0.85f, 0.1f);
        var indicRT  = indicGO.GetComponent<RectTransform>();
        indicRT.anchorMin = indicRT.anchorMax = new Vector2(0.5f, 0.5f);
        indicRT.pivot     = new Vector2(0.5f, 0.5f);
        indicRT.sizeDelta = new Vector2(22f, 32f);
        indicRT.anchoredPosition = Vector2.zero;

        // Подписи слайдера
        var leftLbl  = CreateLabel(sliderGO.transform, "LeftLabel",  "−", 18, FontStyles.Bold, new Vector2(-120f, -28f), new Vector2(60f, 28f));
        var rightLbl = CreateLabel(sliderGO.transform, "RightLabel", "+", 18, FontStyles.Bold, new Vector2( 120f, -28f), new Vector2(60f, 28f));
        leftLbl.color  = new Color(1f, 0.4f, 0.4f);
        rightLbl.color = new Color(0.4f, 1f, 0.5f);

        // ── Кнопки ──────────────────────────────────────────────────
        var buttonsGO = new GameObject("ButtonsContainer");
        buttonsGO.transform.SetParent(card.transform, false);
        var buttonsRT = buttonsGO.AddComponent<RectTransform>();
        buttonsRT.anchorMin = buttonsRT.anchorMax = new Vector2(0.5f, 0.5f);
        buttonsRT.pivot     = new Vector2(0.5f, 0.5f);
        buttonsRT.sizeDelta = new Vector2(300f, 140f);
        buttonsRT.anchoredPosition = new Vector2(0f, -210f);

        var restartBtn = CreateButton(buttonsGO.transform, "RestartButton", "↺  Ещё раз",   new Color(0.22f, 0.72f, 0.28f), new Vector2(0f,  36f), new Vector2(280f, 58f));
        var finishBtn  = CreateButton(buttonsGO.transform, "FinishButton",  "✓  Завершить", new Color(0.20f, 0.50f, 0.85f), new Vector2(0f, -36f), new Vector2(280f, 58f));

        return (resultTxt, finalTxt, bestTxt, medalTxt, sliderComp, trackRT, indicRT, leftLbl, rightLbl, buttonsGO, restartBtn, finishBtn);
    }

    // ================================================================
    // HELPERS
    // ================================================================

    static Image CreateImageGO(Transform parent, string name, Color color)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        return img;
    }

    static GameObject CreateStretchGO(Transform parent, string name)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        SetStretch(go.AddComponent<RectTransform>());
        return go;
    }

    static RectTransform CreateCard(Transform parent, string name, float w, float h, float x, float y)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(0.10f, 0.10f, 0.14f, 0.92f);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(w, h);
        rt.anchoredPosition = new Vector2(x, y);
        return rt;
    }

    static TMP_Text CreateLabel(Transform parent, string name, string text,
                                float size, FontStyles style, Vector2 pos, Vector2 sizeD)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var t  = go.AddComponent<TextMeshProUGUI>();
        t.text      = text;
        t.fontSize  = size;
        t.fontStyle = style;
        t.alignment = TextAlignmentOptions.Center;
        t.color     = Color.white;
        t.enableWordWrapping = true;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = sizeD;
        rt.anchoredPosition = pos;
        return t;
    }

    static Button CreateButton(Transform parent, string name, string label,
                               Color bgColor, Vector2 pos, Vector2 size)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = bgColor;
        var btn = go.AddComponent<Button>();
        btn.targetGraphic = img;

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;

        var txtGO = new GameObject("Label");
        txtGO.transform.SetParent(go.transform, false);
        var txt   = txtGO.AddComponent<TextMeshProUGUI>();
        txt.text      = label;
        txt.fontSize  = 22;
        txt.fontStyle = FontStyles.Bold;
        txt.alignment = TextAlignmentOptions.Center;
        txt.color     = Color.white;
        SetStretch(txt.rectTransform);

        return btn;
    }

    static void CreateDivider(Transform parent, Vector2 pos, Vector2 size)
    {
        var go  = new GameObject("Divider");
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.15f);
        var rt  = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot     = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = pos;
    }

    static void SetStretch(RectTransform rt)
    {
        rt.anchorMin        = Vector2.zero;
        rt.anchorMax        = Vector2.one;
        rt.pivot            = new Vector2(0.5f, 0.5f);
        rt.sizeDelta        = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;
    }

    static RectTransform EnsureRect(GameObject go) =>
        go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();

    static T GetOrAdd<T>(GameObject go) where T : Component =>
        go.GetComponent<T>() ?? go.AddComponent<T>();
}
