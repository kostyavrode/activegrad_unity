using UnityEngine;
using UnityEngine.UI;

public class Station : MonoBehaviour
{
    private Vector2 _position;
    private float _waitTime;
    private int _index;
    private Image _image;
    private bool _isStart;
    private bool _isEnd;
    private bool _isHighlighted;

    public Vector2 Position => _position;
    public float WaitTime => _waitTime;
    public int Index => _index;

    public void Initialize(Vector2 position, float waitTime, int index)
    {
        _position = position;
        _waitTime = waitTime;
        _index = index;
        _image = GetComponent<Image>();
    }

    public void SetAsStart()
    {
        _isStart = true;
        if (_image != null)
            _image.color = new Color(0.2f, 0.8f, 0.2f); // Зеленый для старта
    }

    public void SetAsEnd()
    {
        _isEnd = true;
        if (_image != null)
            _image.color = new Color(0.8f, 0.2f, 0.2f); // Красный для финиша
    }

    public void SetHighlight(bool highlight)
    {
        if (_isHighlighted == highlight)
            return;
        
        _isHighlighted = highlight;
        
        if (_image == null)
            return;
        
        if (_isStart || _isEnd)
            return; // Не меняем цвет старта и финиша
        
        if (highlight)
        {
            _image.color = new Color(0.8f, 0.8f, 0.2f); // Желтый для доступных
        }
        else
        {
            _image.color = new Color(0.3f, 0.3f, 0.8f); // Синий для обычных
        }
    }
}
