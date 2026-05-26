using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SightItemView : MonoBehaviour
{
    public TMP_Text Title;
    public TMP_Text Distance;

    public Image Image;
    public Button Button;

    public int PageId;

    public Action<int> OnClicked;

    private void Awake()
    {
        Button.onClick.AddListener(() => OnClicked?.Invoke(PageId));
        OptimizeForScroll();
    }

    private void OnDisable()
    {
        Button.onClick.RemoveAllListeners();
    }

    public void SetImage(Sprite sprite)
    {
        if (this == null || Image == null) return;
        Image.sprite = sprite;
    }

    /// <summary>
    /// Изолирует элемент в отдельный Canvas, чтобы загрузка изображения или смена текста
    /// не перестраивали Layout всего ScrollView. Отключает raycastTarget на нерабочих графиках.
    /// </summary>
    public void OptimizeForScroll()
    {
        // Separate Canvas per item — dirty flag stays local, not propagated up to ScrollView.
        if (!gameObject.TryGetComponent<Canvas>(out var canvas))
        {
            canvas = gameObject.AddComponent<Canvas>();
            // GraphicRaycaster is required for Button interaction when Canvas is overriding.
            if (!gameObject.TryGetComponent<GraphicRaycaster>(out _))
                gameObject.AddComponent<GraphicRaycaster>();
        }
        canvas.overrideSorting = false;

        // Disable raycastTarget on every Graphic that isn't the Button's target graphic.
        foreach (var graphic in GetComponentsInChildren<Graphic>(includeInactive: true))
        {
            if (graphic == null) continue;

            // Keep raycast only on the actual Button target (usually the background Image of the Button)
            var btn = graphic.GetComponent<Button>();
            if (btn != null || (Button != null && Button.targetGraphic == graphic))
                continue;

            graphic.raycastTarget = false;
        }
    }
}