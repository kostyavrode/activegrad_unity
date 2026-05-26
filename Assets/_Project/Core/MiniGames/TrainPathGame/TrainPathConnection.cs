using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TrainPathConnection : MonoBehaviour
{
    private Station _from;
    private Station _to;
    private float   _travelTime;
    private Image   _lineImage;
    private Tween   _activeTween;

    public Station From        => _from;
    public Station To          => _to;
    public float   TravelTime  => _travelTime;

    public void Initialize(Station from, Station to, float travelTime)
    {
        _from        = from;
        _to          = to;
        _travelTime  = travelTime;
        _lineImage   = GetComponent<Image>();
    }

    public void SetVisual(float minTime, float maxTime, TrainPathConfig config)
    {
        float t = maxTime > minTime ? Mathf.InverseLerp(minTime, maxTime, _travelTime) : 0f;

        Color fastColor  = config?.PathColorFast ?? new Color(0.18f, 0.85f, 0.38f, 0.85f);
        Color slowColor  = config?.PathColorSlow ?? new Color(0.90f, 0.25f, 0.22f, 0.85f);
        float widthFast  = config?.PathWidthFast ?? 6f;
        float widthSlow  = config?.PathWidthSlow ?? 3f;

        Color lineColor = Color.Lerp(fastColor, slowColor, t);

        if (_lineImage != null)
            _lineImage.color = lineColor;

        var rect = GetComponent<RectTransform>();
        if (rect != null)
        {
            float lineW = Mathf.Lerp(widthFast, widthSlow, t);
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, lineW);
        }

        // Center rail highlight (thin white overlay for "rail" look)
        var rail = transform.Find("RailHighlight");
        if (rail != null)
        {
            var rImg = rail.GetComponent<Image>();
            if (rImg != null) rImg.color = new Color(1f, 1f, 1f, 0.18f);
        }

        // Travel-time label
        var label = GetComponentInChildren<TextMeshProUGUI>();
        if (label != null)
        {
            label.text     = $"{_travelTime:F1}s";
            label.fontSize = config?.PathTimeFontSize ?? 10;
            label.color    = new Color(lineColor.r * 1.3f, lineColor.g * 1.3f, lineColor.b * 1.3f, 1f);
        }
    }

    /// <summary>Вспышка линии, когда поезд движется по ней.</summary>
    public void PlayActiveFlash(float duration)
    {
        if (_lineImage == null) return;
        _activeTween?.Kill();
        Color original = _lineImage.color;
        Color bright   = new Color(
            Mathf.Min(original.r * 2f, 1f),
            Mathf.Min(original.g * 2f, 1f),
            Mathf.Min(original.b * 2f, 1f),
            1f);
        _activeTween = DOTween.Sequence()
            .Append(DOTween.To(() => _lineImage.color, c => _lineImage.color = c, bright, 0.12f))
            .Append(DOTween.To(() => _lineImage.color, c => _lineImage.color = c, original, duration - 0.12f))
            .SetUpdate(true);
    }

    public Station GetOtherStation(Station station)
    {
        if (_from == station) return _to;
        if (_to   == station) return _from;
        return null;
    }

    private void OnDestroy() => _activeTween?.Kill();
}
