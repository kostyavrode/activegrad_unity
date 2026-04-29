using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainPathConnection : MonoBehaviour
{
    private Station _from;
    private Station _to;
    private float _travelTime;

    public Station From => _from;
    public Station To => _to;
    public float TravelTime => _travelTime;

    public void Initialize(Station from, Station to, float travelTime)
    {
        _from = from;
        _to = to;
        _travelTime = travelTime;
    }

    public void SetVisual(float minTime, float maxTime, TrainPathConfig config)
    {
        float t = maxTime > minTime ? Mathf.InverseLerp(minTime, maxTime, _travelTime) : 0f;

        Color fastColor = config != null ? config.PathColorFast : new Color(0.20f, 0.85f, 0.35f, 0.85f);
        Color slowColor = config != null ? config.PathColorSlow : new Color(0.90f, 0.25f, 0.20f, 0.85f);
        float widthFast = config != null ? config.PathWidthFast : 6f;
        float widthSlow = config != null ? config.PathWidthSlow : 3f;

        var image = GetComponent<Image>();
        if (image != null)
            image.color = Color.Lerp(fastColor, slowColor, t);

        var rect = GetComponent<RectTransform>();
        if (rect != null)
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, Mathf.Lerp(widthFast, widthSlow, t));

        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text = $"{_travelTime:F1}s";
            if (config != null) label.fontSize = config.PathTimeFontSize;
        }
    }

    public Station GetOtherStation(Station station)
    {
        if (_from == station) return _to;
        if (_to == station) return _from;
        return null;
    }
}
