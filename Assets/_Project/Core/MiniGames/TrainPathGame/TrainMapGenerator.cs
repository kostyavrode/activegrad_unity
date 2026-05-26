using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainMapGenerator
{
    private readonly TrainPathConfig _config;

    // ── Layer patterns: [start=1, middle layers..., end=1]
    // Каждый паттерн — количество станций в каждом слое слева направо.
    // Разные паттерны дают разную топологию сети.
    private static readonly int[][] LayerPatterns =
    {
        new[] { 1, 2, 3, 4, 3, 1 },       // 14 — широкий центр
        new[] { 1, 3, 4, 4, 2, 1 },       // 15 — плотный старт
        new[] { 1, 2, 4, 4, 4, 1 },       // 16 — широкий финиш
        new[] { 1, 3, 3, 3, 4, 1 },       // 15 — нарастающий
        new[] { 1, 2, 3, 3, 4, 2, 1 },    // 16 — 7 слоёв, много развилок
        new[] { 1, 3, 4, 3, 3, 1 },       // 15 — равномерный
        new[] { 1, 2, 3, 4, 2, 3, 1 },    // 16 — 7 слоёв, «бутылочное горло»
        new[] { 1, 3, 4, 3, 2, 1 },       // 14 — компактный
        new[] { 1, 2, 3, 3, 3, 2, 1 },    // 15 — 7 слоёв, равномерный
        new[] { 1, 3, 3, 4, 4, 1 },       // 16 — нарастающий плотный
    };

    public TrainMapGenerator(TrainPathConfig config)
    {
        _config = config;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PUBLIC
    // ══════════════════════════════════════════════════════════════════════════

    public void GenerateMap(
        RectTransform container,
        out List<Station> stations,
        out List<TrainPathConnection> paths,
        out Station startStation,
        out Station endStation,
        float mapWidthOverride  = 0f,
        float mapHeightOverride = 0f)
    {
        stations = new List<Station>();
        paths    = new List<TrainPathConnection>();

        float mapW = mapWidthOverride  > 0 ? mapWidthOverride  : (_config?.MapWidth  ?? 280f);
        float mapH = mapHeightOverride > 0 ? mapHeightOverride : (_config?.MapHeight ?? 400f);

        // Выбираем случайный паттерн слоёв
        int[] pattern  = LayerPatterns[Random.Range(0, LayerPatterns.Length)];
        int numLayers  = pattern.Length;
        int totalCount = pattern.Sum();

        // Генерируем позиции по слоям
        var layerPositions = BuildLayerPositions(pattern, mapW, mapH);

        // Создаём станции, группируем по слоям
        var stationsByLayer = new List<List<Station>>();
        int globalIndex = 0;

        for (int l = 0; l < numLayers; l++)
        {
            var layerGroup = new List<Station>();
            for (int i = 0; i < pattern[l]; i++)
            {
                float waitTime = Random.Range(
                    _config?.MinWaitTime ?? 0.3f,
                    _config?.MaxWaitTime ?? 1.5f);

                var s = CreateStation(container, layerPositions[l][i], globalIndex++, waitTime);
                stations.Add(s);
                layerGroup.Add(s);
            }
            stationsByLayer.Add(layerGroup);
        }

        // Старт — первый в нулевом слое, финиш — первый в последнем
        startStation = stationsByLayer[0][0];
        endStation   = stationsByLayer[numLayers - 1][0];
        startStation.SetAsStart();
        endStation.SetAsEnd();

        // Строим рёбра
        GenerateLayeredPaths(stationsByLayer, paths, container, mapW, mapH);
        AssignCargoStations(stations, totalCount);
        ApplyPathVisuals(paths);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // POSITION GENERATION
    // ══════════════════════════════════════════════════════════════════════════

    private List<List<Vector2>> BuildLayerPositions(int[] pattern, float mapW, float mapH)
    {
        int numLayers  = pattern.Length;
        float margin   = 28f;
        float usableW  = mapW - margin * 2f;
        float usableH  = mapH - margin * 2f;
        float layerStep = numLayers > 1 ? usableW / (numLayers - 1) : 0f;

        var result = new List<List<Vector2>>();

        for (int l = 0; l < numLayers; l++)
        {
            int count = pattern[l];
            float centerX = -usableW * 0.5f + layerStep * l;

            // Горизонтальный джиттер (кроме первого и последнего слоя)
            float xJitter = (l == 0 || l == numLayers - 1) ? 0f : layerStep * 0.20f;

            var positions = new List<Vector2>();

            if (count == 1)
            {
                float y = Random.Range(-usableH * 0.12f, usableH * 0.12f);
                positions.Add(new Vector2(
                    centerX + Random.Range(-xJitter, xJitter), y));
            }
            else
            {
                float step = usableH / (count - 1);
                for (int i = 0; i < count; i++)
                {
                    float y = -usableH * 0.5f + step * i
                              + Random.Range(-step * 0.20f, step * 0.20f);
                    float x = centerX + Random.Range(-xJitter, xJitter);
                    positions.Add(new Vector2(x, y));
                }
            }

            result.Add(positions);
        }

        return result;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CONNECTION GENERATION
    // ══════════════════════════════════════════════════════════════════════════

    private void GenerateLayeredPaths(
        List<List<Station>> byLayer,
        List<TrainPathConnection> paths,
        RectTransform container,
        float mapW,
        float mapH)
    {
        int numLayers = byLayer.Count;

        // ── 1. Прямые связи (слой i → слой i+1) ─────────────────────────────
        for (int l = 0; l < numLayers - 1; l++)
        {
            var curr = byLayer[l];
            var next = byLayer[l + 1];

            // Каждая станция текущего слоя → 1–2 ближайшие следующего
            foreach (var from in curr)
            {
                var sorted = next
                    .OrderBy(s => Vector2.Distance(from.Position, s.Position))
                    .ToList();

                // Всегда соединяем с ближайшей
                AddPath(container, from, sorted[0], paths, mapW);

                // Вторая ближайшая с вероятностью 70%
                if (sorted.Count > 1 && Random.value < 0.70f)
                    AddPath(container, from, sorted[1], paths, mapW);

                // Третья с вероятностью 25%
                if (sorted.Count > 2 && Random.value < 0.25f)
                    AddPath(container, from, sorted[2], paths, mapW);
            }

            // Гарантируем, что у каждой станции следующего слоя есть хотя бы один вход
            foreach (var to in next)
            {
                bool connected = paths.Any(p => p.To == to || p.From == to);
                if (!connected)
                {
                    var closest = curr
                        .OrderBy(s => Vector2.Distance(s.Position, to.Position))
                        .First();
                    AddPath(container, closest, to, paths, mapW);
                }
            }
        }

        // ── 2. Шорткаты: слой i → слой i+2 (обходные пути) ─────────────────
        for (int l = 0; l < numLayers - 2; l++)
        {
            foreach (var from in byLayer[l])
            {
                if (Random.value > 0.35f) continue; // 35% вероятность
                var skipLayer = byLayer[l + 2];
                var target    = skipLayer[Random.Range(0, skipLayer.Count)];
                AddPath(container, from, target, paths, mapW);
            }
        }

        // ── 3. Внутрислоевые связи (тупики / петли) ─────────────────────────
        // Делают карту запутанней: можно «застрять» в одном слое
        for (int l = 1; l < numLayers - 1; l++)
        {
            var layer = byLayer[l];
            for (int i = 0; i < layer.Count; i++)
            {
                for (int j = i + 1; j < layer.Count; j++)
                {
                    float dist = Vector2.Distance(layer[i].Position, layer[j].Position);
                    // Соединяем только если достаточно близко (в пределах 55% высоты карты)
                    float threshold = mapH * 0.55f;
                    if (dist < threshold && Random.value < 0.40f)
                        AddPath(container, layer[i], layer[j], paths, mapW);
                }
            }
        }

        // ── 4. Обратные связи: слой i+1 → слой i-1 («тупиковые ветки») ─────
        // Добавляют один-два обратных хода, усложняющих навигацию
        int backCount = Random.Range(1, 3);
        for (int attempt = 0; attempt < backCount * 3 && backCount > 0; attempt++)
        {
            int l = Random.Range(2, numLayers - 1);
            if (byLayer[l].Count == 0 || byLayer[l - 2].Count == 0) continue;

            var from = byLayer[l][Random.Range(0, byLayer[l].Count)];
            var to   = byLayer[l - 2][Random.Range(0, byLayer[l - 2].Count)];

            // Не создаём обратные связи к старту или финишу
            if (to == byLayer[0][0] || from == byLayer[numLayers - 1][0]) continue;

            if (AddPath(container, from, to, paths, mapW))
                backCount--;
        }
    }

    // Возвращает true, если ребро было добавлено (не дублирует существующее)
    private bool AddPath(RectTransform container, Station from, Station to,
        List<TrainPathConnection> paths, float mapW)
    {
        if (from == to) return false;
        if (paths.Any(p =>
            (p.From == from && p.To == to) ||
            (p.From == to   && p.To == from))) return false;

        float distance = Vector2.Distance(from.Position, to.Position);
        float baseTravelTime = Mathf.Lerp(
            _config?.MinTravelTime ?? 0.5f,
            _config?.MaxTravelTime ?? 4.0f,
            distance / mapW);

        float travelTime = Mathf.Clamp(
            baseTravelTime * Random.Range(
                _config?.TravelTimeVarianceMin ?? 0.7f,
                _config?.TravelTimeVarianceMax ?? 1.3f),
            _config?.MinTravelTime ?? 0.5f,
            _config?.MaxTravelTime ?? 4.0f);

        paths.Add(CreatePath(container, from, to, travelTime));
        return true;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // CARGO ASSIGNMENT
    // ══════════════════════════════════════════════════════════════════════════

    private void AssignCargoStations(List<Station> stations, int stationCount)
    {
        // Исключаем старт и финиш
        var intermediates = stations.Skip(1).Take(stationCount - 2).ToList();

        // Shuffle
        for (int i = intermediates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (intermediates[i], intermediates[j]) = (intermediates[j], intermediates[i]);
        }

        // Больше станций → больше груза (3–5 вместо 2–4)
        int cargoCount = Mathf.Clamp(
            stationCount / (_config?.StationsPerCargo ?? 3),
            _config?.MinCargoStations ?? 3,
            _config?.MaxCargoStations ?? 5);

        for (int i = 0; i < Mathf.Min(cargoCount, intermediates.Count); i++)
            intermediates[i].SetAsCargo();
    }

    // ══════════════════════════════════════════════════════════════════════════
    // VISUALS
    // ══════════════════════════════════════════════════════════════════════════

    private void ApplyPathVisuals(List<TrainPathConnection> paths)
    {
        if (paths.Count == 0) return;
        float minTime = paths.Min(p => p.TravelTime);
        float maxTime = paths.Max(p => p.TravelTime);
        foreach (var path in paths)
            path.SetVisual(minTime, maxTime, _config);
    }

    // ══════════════════════════════════════════════════════════════════════════
    // STATION CREATION
    // ══════════════════════════════════════════════════════════════════════════

    private Station CreateStation(RectTransform container, Vector2 position, int index, float waitTime)
    {
        var stationObj = new GameObject($"Station_{index}");
        stationObj.transform.SetParent(container, false);

        stationObj.AddComponent<Image>();

        var rect = stationObj.GetComponent<RectTransform>();
        Vector2 size = _config?.StationSize ?? new Vector2(46f, 46f);
        rect.sizeDelta        = size;
        rect.anchoredPosition = position;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);

        var labelObj = new GameObject("Label");
        labelObj.transform.SetParent(stationObj.transform, false);
        var labelTmp = labelObj.AddComponent<TextMeshProUGUI>();
        labelTmp.fontSize      = 14;
        labelTmp.color         = Color.white;
        labelTmp.alignment     = TextAlignmentOptions.Center;
        labelTmp.raycastTarget = false;

        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta        = new Vector2(size.x + 4f, size.y);
        labelRect.anchoredPosition = Vector2.zero;
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot     = new Vector2(0.5f, 0.5f);

        var station = stationObj.AddComponent<Station>();
        station.Initialize(position, waitTime, index, _config);
        return station;
    }

    // ══════════════════════════════════════════════════════════════════════════
    // PATH CREATION
    // ══════════════════════════════════════════════════════════════════════════

    private TrainPathConnection CreatePath(RectTransform container, Station from, Station to, float travelTime)
    {
        var pathObj = new GameObject($"Path_{from.Index}_to_{to.Index}");
        pathObj.transform.SetParent(container, false);

        var img = pathObj.AddComponent<Image>();
        img.color = new Color(0.4f, 0.4f, 0.6f, 0.7f);

        Vector2 fromPos = from.Position;
        Vector2 toPos   = to.Position;
        Vector2 dir     = (toPos - fromPos).normalized;
        float   dist    = Vector2.Distance(fromPos, toPos);
        float   angle   = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        var rect = pathObj.GetComponent<RectTransform>();
        rect.sizeDelta        = new Vector2(dist, 5f);
        rect.anchoredPosition = (fromPos + toPos) * 0.5f;
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot     = new Vector2(0.5f, 0.5f);
        rect.localRotation = Quaternion.Euler(0, 0, angle);
        rect.SetAsFirstSibling(); // рельсы позади станций

        // Тонкий блик — имитация рельс
        var hlObj = new GameObject("RailHighlight");
        hlObj.transform.SetParent(pathObj.transform, false);
        var hlImg = hlObj.AddComponent<Image>();
        hlImg.color        = new Color(1f, 1f, 1f, 0.15f);
        hlImg.raycastTarget = false;
        var hlRect = hlObj.GetComponent<RectTransform>();
        hlRect.anchorMin = Vector2.zero; hlRect.anchorMax = Vector2.one;
        hlRect.offsetMin = new Vector2(2f, 1f); hlRect.offsetMax = new Vector2(-2f, -1f);

        // Метка времени в пути
        var labelObj = new GameObject("TimeLabel");
        labelObj.transform.SetParent(pathObj.transform, false);
        var label = labelObj.AddComponent<TextMeshProUGUI>();
        label.fontSize      = _config?.PathTimeFontSize ?? 10;
        label.color         = Color.white;
        label.alignment     = TextAlignmentOptions.Center;
        label.raycastTarget = false;

        var labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta        = new Vector2(40f, 18f);
        labelRect.anchoredPosition = new Vector2(0, 8f);
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot     = new Vector2(0.5f, 0.5f);
        labelRect.localRotation = Quaternion.Euler(0, 0, -angle);

        var path = pathObj.AddComponent<TrainPathConnection>();
        path.Initialize(from, to, travelTime);
        return path;
    }
}
