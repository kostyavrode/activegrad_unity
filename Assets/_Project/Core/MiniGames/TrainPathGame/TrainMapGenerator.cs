using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class TrainMapGenerator
{
    private const int MIN_STATIONS = 6;
    private const int MAX_STATIONS = 10;
    private const float MAP_WIDTH = 800f;
    private const float MAP_HEIGHT = 600f;
    private const float MIN_DISTANCE = 100f;
    private const float MIN_TRAVEL_TIME = 1f;
    private const float MAX_TRAVEL_TIME = 3f;
    private const float MIN_WAIT_TIME = 0.5f;
    private const float MAX_WAIT_TIME = 2f;

    public void GenerateMap(RectTransform container, out List<Station> stations, 
        out List<TrainPathConnection> paths, out Station startStation, out Station endStation)
    {
        stations = new List<Station>();
        paths = new List<TrainPathConnection>();
        
        int stationCount = Random.Range(MIN_STATIONS, MAX_STATIONS + 1);
        
        // Генерируем позиции станций
        List<Vector2> positions = GenerateStationPositions(stationCount);
        
        // Создаем станции
        for (int i = 0; i < stationCount; i++)
        {
            float waitTime = Random.Range(MIN_WAIT_TIME, MAX_WAIT_TIME);
            Station station = CreateStation(container, positions[i], i, waitTime);
            stations.Add(station);
        }
        
        // Определяем стартовую и конечную станции
        startStation = stations[0];
        endStation = stations[stationCount - 1];
        
        startStation.SetAsStart();
        endStation.SetAsEnd();
        
        // Генерируем пути между станциями
        GeneratePaths(stations, paths, container);
    }

    private List<Vector2> GenerateStationPositions(int count)
    {
        List<Vector2> positions = new List<Vector2>();
        
        // Размещаем станции в виде извилистого пути
        float stepX = MAP_WIDTH / (count + 1);
        float baseY = MAP_HEIGHT * 0.5f;
        
        for (int i = 0; i < count; i++)
        {
            float x = -MAP_WIDTH * 0.5f + stepX * (i + 1);
            float y = baseY + Random.Range(-MAP_HEIGHT * 0.3f, MAP_HEIGHT * 0.3f);
            
            // Проверяем минимальное расстояние
            bool valid = true;
            foreach (var pos in positions)
            {
                if (Vector2.Distance(new Vector2(x, y), pos) < MIN_DISTANCE)
                {
                    valid = false;
                    break;
                }
            }
            
            if (!valid)
            {
                // Пробуем другое место
                y = baseY + Random.Range(-MAP_HEIGHT * 0.2f, MAP_HEIGHT * 0.2f);
            }
            
            positions.Add(new Vector2(x, y));
        }
        
        return positions;
    }

    private Station CreateStation(RectTransform container, Vector2 position, int index, float waitTime)
    {
        GameObject stationObj = new GameObject($"Station_{index}");
        stationObj.transform.SetParent(container, false);
        
        Image image = stationObj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.3f, 0.8f);
        
        RectTransform rect = stationObj.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(40, 40);
        rect.anchoredPosition = position;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        
        // Добавляем текст с временем ожидания
        GameObject textObj = new GameObject("WaitTimeText");
        textObj.transform.SetParent(stationObj.transform, false);
        
        TMPro.TextMeshProUGUI text = textObj.AddComponent<TMPro.TextMeshProUGUI>();
        text.text = $"{waitTime:F1}с";
        text.fontSize = 12;
        text.color = Color.white;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.sizeDelta = new Vector2(40, 20);
        textRect.anchoredPosition = new Vector2(0, -30);
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.pivot = new Vector2(0.5f, 0.5f);
        
        Station station = stationObj.AddComponent<Station>();
        station.Initialize(position, waitTime, index);
        
        return station;
    }

    private void GeneratePaths(List<Station> stations, List<TrainPathConnection> paths, RectTransform container)
    {
        // Создаем основной путь от начала до конца
        for (int i = 0; i < stations.Count - 1; i++)
        {
            Station from = stations[i];
            Station to = stations[i + 1];
            
            float distance = Vector2.Distance(from.Position, to.Position);
            float travelTime = Mathf.Lerp(MIN_TRAVEL_TIME, MAX_TRAVEL_TIME, distance / MAP_WIDTH);
            
            TrainPathConnection path = CreatePath(container, from, to, travelTime);
            paths.Add(path);
        }
        
        // Добавляем дополнительные пути для усложнения
        int extraPaths = Random.Range(2, stations.Count / 2);
        int attempts = 0;
        
        while (paths.Count < stations.Count - 1 + extraPaths && attempts < 50)
        {
            attempts++;
            
            Station from = stations[Random.Range(0, stations.Count)];
            Station to = stations[Random.Range(0, stations.Count)];
            
            if (from == to)
                continue;
            
            // Проверяем, нет ли уже такого пути
            bool exists = paths.Any(p => 
                (p.From == from && p.To == to) ||
                (p.From == to && p.To == from));
            
            if (exists)
                continue;
            
            // Не создаем слишком длинные пути
            float distance = Vector2.Distance(from.Position, to.Position);
            if (distance > MAP_WIDTH * 0.6f)
                continue;
            
            float travelTime = Mathf.Lerp(MIN_TRAVEL_TIME, MAX_TRAVEL_TIME, distance / MAP_WIDTH);
            TrainPathConnection path = CreatePath(container, from, to, travelTime);
            paths.Add(path);
        }
    }

    private TrainPathConnection CreatePath(RectTransform container, Station from, Station to, float travelTime)
    {
        GameObject pathObj = new GameObject($"Path_{from.Index}_to_{to.Index}");
        pathObj.transform.SetParent(container, false);
        
        Image image = pathObj.AddComponent<Image>();
        image.color = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        
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
        
        // Устанавливаем порядок отрисовки (пути под станциями)
        rect.SetAsFirstSibling();
        
        TrainPathConnection path = pathObj.AddComponent<TrainPathConnection>();
        path.Initialize(from, to, travelTime);
        
        return path;
    }
}
