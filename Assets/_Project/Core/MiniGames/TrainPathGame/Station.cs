using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Station : MonoBehaviour
{
    private Vector2 _position;
    private float _waitTime;
    private int _index;
    private Image _image;
    private TextMeshProUGUI _label;
    private bool _isStart;
    private bool _isEnd;
    private bool _isCargo;
    private bool _isCargoCollected;
    private Color _baseColor;
    private TrainPathConfig _config;

    public Vector2 Position => _position;
    public float WaitTime => _waitTime;
    public int Index => _index;
    public bool IsCargo => _isCargo;
    public bool IsCargoCollected => _isCargoCollected;

    public void Initialize(Vector2 position, float waitTime, int index, TrainPathConfig config)
    {
        _position = position;
        _waitTime = waitTime;
        _index = index;
        _config = config;
        _image = GetComponent<Image>();
        _label = GetComponentInChildren<TextMeshProUGUI>();

        if (_config != null && _image != null && _config.StationSprite != null)
            _image.sprite = _config.StationSprite;

        _baseColor = _config != null ? _config.ColorDefault : new Color(0.30f, 0.35f, 0.80f);
        ApplyColor();
    }

    public void SetAsStart()
    {
        _isStart = true;
        _baseColor = _config != null ? _config.ColorStart : new Color(0.20f, 0.75f, 0.25f);
        if (_config != null && _image != null && _config.StartStationSprite != null)
            _image.sprite = _config.StartStationSprite;
        ApplyColor();
    }

    public void SetAsEnd()
    {
        _isEnd = true;
        _baseColor = _config != null ? _config.ColorEnd : new Color(0.75f, 0.20f, 0.20f);
        if (_config != null && _image != null && _config.EndStationSprite != null)
            _image.sprite = _config.EndStationSprite;
        ApplyColor();
    }

    public void SetAsCargo()
    {
        _isCargo = true;
        _baseColor = _config != null ? _config.ColorCargo : new Color(0.90f, 0.55f, 0.10f);
        if (_config != null && _image != null && _config.CargoStationSprite != null)
            _image.sprite = _config.CargoStationSprite;
        ApplyColor();
        if (_label != null) _label.text = "📦";
    }

    public void CollectCargo()
    {
        _isCargoCollected = true;
        _baseColor = _config != null ? _config.ColorCollected : new Color(0.42f, 0.45f, 0.50f);
        ApplyColor();
        if (_label != null) _label.text = "✓";
    }

    public void SetHighlight(bool highlight)
    {
        if (_isStart || _isEnd || _isCargoCollected) return;
        if (_image == null) return;
        Color highlightColor = _config != null ? _config.ColorHighlight : new Color(1f, 0.9f, 0.1f);
        _image.color = highlight ? highlightColor : _baseColor;
    }

    private void ApplyColor()
    {
        if (_image != null) _image.color = _baseColor;
    }
}
