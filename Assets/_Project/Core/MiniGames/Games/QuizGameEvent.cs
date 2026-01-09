using UnityEngine;

public class QuizGameEvent : BaseGameEvent
{
    protected override void OnStartGame()
    {
        var gameUI = new GameObject("QuizGameUI");
        gameUI.transform.SetParent(_parentContainer, false);
        
        var rectTransform = gameUI.AddComponent<RectTransform>();
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.sizeDelta = Vector2.zero;
    }

    protected override void OnCleanup()
    {
        if (_parentContainer != null)
        {
            foreach (Transform child in _parentContainer)
            {
                Object.Destroy(child.gameObject);
            }
        }
    }
}



