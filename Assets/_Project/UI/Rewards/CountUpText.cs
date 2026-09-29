using System;
using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Анимированный счётчик для TMP_Text: плавно считает от текущего значения к новому и "пульсирует" при росте.
/// </summary>
[DisallowMultipleComponent]
public class CountUpText : MonoBehaviour
{
    [SerializeField] private string _format = "{0}";
    [SerializeField] private float _duration = 0.6f;
    [SerializeField] private float _punch = 0.15f;

    private TMP_Text _text;
    private bool _initialized;
    private int _target;
    private float _shown;
    private Vector3 _baseScale = Vector3.one;
    private Tween _countTween;
    private Tween _punchTween;

    public int Value
    {
        get { EnsureInit(); return _target; }
    }

    /// <summary>Формат string.Format, например "{0}" или "x{0}". Пусто — просто число.</summary>
    public string Format
    {
        get => _format;
        set
        {
            _format = string.IsNullOrEmpty(value) ? "{0}" : value;
            Render();
        }
    }

    private void Awake() => EnsureInit();

    private void EnsureInit()
    {
        if (_initialized) return;
        _initialized = true;
        _text = GetComponent<TMP_Text>();
        _baseScale = transform.localScale;
        if (_text != null && int.TryParse(_text.text, out var v))
        {
            _target = v;
            _shown = v;
        }
    }

    public void SetValue(int value, bool animate = true)
    {
        EnsureInit();

        bool increasing = value > _target;
        _target = value;

        if (_countTween != null && _countTween.IsActive())
            _countTween.Kill();

        if (!animate || !gameObject.activeInHierarchy)
        {
            _shown = value;
            Render();
            return;
        }

        _countTween = DOTween.To(() => _shown, x =>
            {
                _shown = x;
                Render();
            }, value, _duration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .SetLink(gameObject);

        if (increasing)
            Punch();
    }

    private void Punch()
    {
        if (_punchTween != null && _punchTween.IsActive())
            _punchTween.Kill();
        transform.localScale = _baseScale;
        _punchTween = transform.DOPunchScale(_baseScale * _punch, 0.35f, 6, 0.6f)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void Render()
    {
        if (_text == null) return;
        int v = Mathf.RoundToInt(_shown);
        string s;
        try
        {
            s = string.IsNullOrEmpty(_format) ? v.ToString() : string.Format(_format, v);
        }
        catch (FormatException)
        {
            s = v.ToString();
        }
        _text.text = s;
    }

    private void OnDisable()
    {
        if (!_initialized) return;
        // Не оставляем "недосчитанное" значение и увеличенный масштаб
        if (_countTween != null && _countTween.IsActive()) _countTween.Kill();
        if (_punchTween != null && _punchTween.IsActive())
        {
            _punchTween.Kill();
            transform.localScale = _baseScale;
        }
        _shown = _target;
        Render();
    }
}
