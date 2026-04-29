using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrainMapGenerator
{
    private readonly TrainPathConfig _config;

    public TrainMapGenerator(TrainPathConfig config)
    {
        _config = config;
    }

    public void GenerateMap(RectTransform container, out List<Station> stations,
        out List<TrainPathConnection> paths, out Station startStation, out Station endStation)
    {
        stations = new List<Station>();
        paths = new List<TrainPathConnection>();

        int stationCount = Random.Range(_config.MinStations, _config.MaxStations + 1);

        List<Vector2> positions = GenerateStationPositions(stationCount);

        for (int i = 0; i < stationCount; i++)
        {
            float waitTime = Random.Range(_config.MinWaitTime, _config.MaxWaitTime);
            Station station = CreateStation(container, positions[i], i, waitTime);
            stations.Add(station);
        }

        startStation = stations[0];
        endStation = stations[stationCount - 1];
        startStation.SetAsStart();
        endStation.SetAsEnd();

        GeneratePaths(stations, paths, container);
        AssignCargoStations(stations, stationCount);
        ApplyPathVisuals(paths);
    }

    private void ApplyPathVisuals(List<TrainPathConnection> paths)
    {
        if (paths.Count == 0) return;
        float minTime = paths.Min(p => p.TravelTime);
        float maxTime = paths.Max(p => p.TravelTime);
        foreach (var path in paths)
            path.SetVisual(minTime, maxTime, _config);
    }

    private void AssignCargoStations(List<Station> stations, int stationCount)
    {
        var intermediates = new List<Station>(stations.GetRange(1, stationCount - 2));

        for (int i = intermediates.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            Station tmp = intermediates[i];
            intermediates[i] = intermediates[j];
            intermediates[j] = tmp;
        }

        int cargoCount = Mathf.Clamp(
            stationCount / _config.StationsPerCargo,
            _config.MinCargoStations,
            _config.MaxCargoStations);

        for (int i = 0; i < Mathf.Min(cargoCount, intermediates.Count); i++)
            intermediates[i].SetAsCargo();
    }

    private List<Vector2> GenerateStationPositions(int count)
    {
        float mapWidth = _config.MapWidth;
        float mapHeight = _config.MapHeight;

        int cols = Mathf.CeilToInt(Mathf.Sqrt(count * 1.5f));
        int rows = Mathf.CeilToInt((float)count / cols);

        float cellW = mapWidth / cols;
        float cellH = mapHeight / rows;

        List<int> allCells = Enumerable.Range(0, cols * rows).OrderBy(_ => Random.value).ToList();
        List<Vector2> positions = new List<Vector2>();

        for (int i = 0; i < count; i++)
        {
            int cell = allCells[i];
            int col = cell % cols;
            int row = cell / cols;

            float x = -mapWidth * 0.5f + cellW * col + cellW * Random.Range(0.2f, 0.8f);
            float y = -mapHeight * 0.5f + cellH * row + cellH * Random.Range(0.2f, 0.8f);

            positions.Add(new Vector2(x, y));
        }

        positions.Sort((a, b) => a.x.CompareTo(b.x));
        return positions;
    }

    private Station CreateStation(RectTransform container, Vector2 position, int index, float waitTime)
    {
        GameObject stationObj = new GameObject($"Station_{index}");
        stationObj.transform.SetParent(container, false);

        Image image = stationObj.AddComponent<Image>();
        image.color = _config.ColorDefault;
        if (_config.StationSprite != null)
            image.sprite = _config.StationSprite;

        RectTransform rect = stationObj.GetComponent<RectTransform>();
        rect.sizeDelta = _config.StationSize;
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        GameObject textObj = new GameObject("WaitTimeText");
        textObj.transform.SetParent(stationObj.transform, false);

        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = $"{waitTime:F1}s";
        text.fontSize = _config.WaitTimeFontSize;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(_config.StationSize.x + 10f, 18f);
        textRect.anchoredPosition = new Vector2(0, -(_config.StationSize.y * 0.5f + 9f));
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);

        Station station = stationObj.AddComponent<Station>();
        station.Initialize(position, waitTime, index, _config);

        return station;
    }

    private void GeneratePaths(List<Station> stations, List<TrainPathConnection> paths, RectTransform container)
    {
        for (int i = 0; i < stations.Count - 1; i++)
            CreateAndAddPath(container, stations[i], stations[i + 1], paths);

        float connectionRadius = _config.MapWidth * _config.ConnectionRadiusFactor;

        for (int i = 0; i < stations.Count; i++)
        {
            for (int j = i + 2; j < stations.Count; j++)
            {
                float dist = Vector2.Distance(stations[i].Position, stations[j].Position);
                if (dist > connectionRadius)
                    continue;

                bool exists = paths.Any(p =>
                    (p.From == stations[i] && p.To == stations[j]) ||
                    (p.From == stations[j] && p.To == stations[i]));

                if (exists)
                    continue;

                float probability = (1f - dist / connectionRadius) * _config.ExtraConnectionProbability;
                if (Random.value < probability)
                    CreateAndAddPath(container, stations[i], stations[j], paths);
            }
        }
    }

    private void CreateAndAddPath(RectTransform container, Station from, Station to, List<TrainPathConnection> paths)
    {
        float distance = Vector2.Distance(from.Position, to.Position);
        float baseTravelTime = Mathf.Lerp(_config.MinTravelTime, _config.MaxTravelTime, distance / _config.MapWidth);
        float travelTime = Mathf.Clamp(
            baseTravelTime * Random.Range(_config.TravelTimeVarianceMin, _config.TravelTimeVarianceMax),
            _config.MinTravelTime,
            _config.MaxTravelTime);

        TrainPathConnection path = CreatePath(container, from, to, travelTime);
        paths.Add(path);
    }

    private TrainPathConnection CreatePath(RectTransform container, Station from, Station to, float travelTime)
    {
        GameObject pathObj = new GameObject($"Path_{from.Index}_to_{to.Index}");
        pathObj.transform.SetParent(container, false);

        Image image = pathObj.AddComponent<Image>();
        image.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);

        Vector2 fromPos = from.Position;
        Vector2 toPos = to.Position;
        Vector2 direction = (toPos - fromPos).normalized;
        float distance = Vector2.Distance(fromPos, toPos);

        RectTransform rect = pathObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(distance, 4f);
        rect.anchoredPosition = (fromPos + toPos) * 0.5f;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        rect.localRotation = Quaternion.Euler(0, 0, angle);

        rect.SetAsFirstSibling();

        GameObject labelObj = new GameObject("TimeLabel");
        labelObj.transform.SetParent(pathObj.transform, false);

        TMPro.TextMeshProUGUI label = labelObj.AddComponent<TMPro.TextMeshProUGUI>();
        label.fontSize = _config.PathTimeFontSize;
        label.color = Color.white;
        label.alignment = TMPro.TextAlignmentOptions.Center;

        RectTransform labelRect = labelObj.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(44, 18);
        labelRect.anchoredPosition = new Vector2(0, 7);
        labelRect.anchorMin = new Vector2(0.5f, 0.5f);
        labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.pivot = new Vector2(0.5f, 0.5f);
        labelRect.localRotation = Quaternion.Euler(0, 0, -angle);

        TrainPathConnection path = pathObj.AddComponent<TrainPathConnection>();
        path.Initialize(from, to, travelTime);

        return path;
    }
}
