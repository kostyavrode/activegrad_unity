using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

public class UIGroupSelector : MonoBehaviour
{
    [Serializable]
    public class SelectableItem
    {
        public Button button;
        public TMP_Text buttonText;

        [Range(0f, 1f)] public float normalAlpha = 0.5f;
        [Range(0f, 1f)] public float selectedAlpha = 1f;
        public Color normalTextColor = Color.gray;
        public Color selectedTextColor = Color.white;

        public UnityEngine.Events.UnityEvent OnSelected;

        [HideInInspector] public Image buttonImage;
    }

    [SerializeField] private SelectableItem[] _items;
    [SerializeField] private int _defaultSelectedIndex = 0;
    [SerializeField] private float _animDuration = 0.2f;

    private int _currentSelectedIndex = -1;

    public event Action<int> OnItemSelected;

    private void OnEnable()
    {
        for (int i = 0; i < _items.Length; i++)
        {
            var item = _items[i];
            if (item?.button == null) continue;

            item.buttonImage = item.button.GetComponent<Image>();
            item.button.onClick.RemoveAllListeners();

            int index = i;
            item.button.onClick.AddListener(() => SelectItem(index));
        }

        int selectIndex = _currentSelectedIndex >= 0 ? _currentSelectedIndex : _defaultSelectedIndex;
        _currentSelectedIndex = -1;
        SelectItemImmediate(selectIndex);
    }

    private void OnDisable()
    {
        foreach (var item in _items)
        {
            if (item?.button != null)
                item.button.onClick.RemoveAllListeners();
        }
    }

    public void SelectItem(int index)
    {
        if (index < 0 || index >= _items.Length) return;
        if (index == _currentSelectedIndex) return;

        if (_currentSelectedIndex >= 0)
            AnimateItem(_currentSelectedIndex, false);

        AnimateItem(index, true);
        _currentSelectedIndex = index;

        OnItemSelected?.Invoke(index);
        _items[index].OnSelected?.Invoke();
    }

    private void SelectItemImmediate(int index)
    {
        if (index < 0 || index >= _items.Length) return;

        for (int i = 0; i < _items.Length; i++)
            SetVisualState(i, i == index);

        _currentSelectedIndex = index;
    }

    private void AnimateItem(int index, bool selected)
    {
        var item = _items[index];
        float targetAlpha = selected ? item.selectedAlpha : item.normalAlpha;
        Color targetTextColor = selected ? item.selectedTextColor : item.normalTextColor;

        if (item.buttonImage != null)
        {
            item.buttonImage.DOKill();
            item.buttonImage.DOFade(targetAlpha, _animDuration).SetEase(Ease.OutQuad);
        }

        if (item.buttonText != null)
        {
            item.buttonText.DOKill();
            item.buttonText.DOColor(targetTextColor, _animDuration).SetEase(Ease.OutQuad);
        }
    }

    private void SetVisualState(int index, bool selected)
    {
        var item = _items[index];

        if (item.buttonImage != null)
        {
            var c = item.buttonImage.color;
            c.a = selected ? item.selectedAlpha : item.normalAlpha;
            item.buttonImage.color = c;
        }

        if (item.buttonText != null)
            item.buttonText.color = selected ? item.selectedTextColor : item.normalTextColor;
    }

    public int GetCurrentIndex() => _currentSelectedIndex;

    public void ResetToDefault() => SelectItem(_defaultSelectedIndex);

    private void OnDestroy()
    {
        foreach (var item in _items)
        {
            item?.buttonImage?.DOKill();
            item?.buttonText?.DOKill();
        }
    }
}
