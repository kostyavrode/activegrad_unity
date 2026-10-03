using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Выносит постоянно анимируемый элемент UI во вложенный Canvas.
/// Весь интерфейс лежит на одном Canvas: любое изменение цвета/масштаба элемента каждый кадр
/// заставляет перестраивать батчи ВСЕГО Canvas. Со своим Canvas перестраивается только этот элемент.
/// Порядок отрисовки не меняется (overrideSorting выключен), нажатия работают через свой GraphicRaycaster.
/// </summary>
public static class UICanvasIsolation
{
    public static void Ensure(GameObject target)
    {
        if (target == null || target.GetComponent<Canvas>() != null)
            return;

        // Корневой Canvas не трогаем — элемент должен быть внутри существующего UI.
        var parentCanvas = target.GetComponentInParent<Canvas>(true);
        if (parentCanvas == null)
            return;

        var canvas = target.AddComponent<Canvas>();
        canvas.overrideSorting = false;
        canvas.additionalShaderChannels = parentCanvas.rootCanvas.additionalShaderChannels;

        if (target.GetComponent<GraphicRaycaster>() == null)
            target.AddComponent<GraphicRaycaster>();
    }
}
