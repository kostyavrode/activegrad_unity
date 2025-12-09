using System;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class OtherPlayerSightsDetailsView : MonoBehaviour
{
    [SerializeField] private Button _closeButton;
    
    public Transform Content;

    private void Awake()
    {
        _closeButton.onClick.AddListener(() => Destroy(gameObject) );
    }

    private void OnDestroy()
    {
        _closeButton.onClick.RemoveAllListeners();
    }
    
    public class Factory : PlaceholderFactory<OtherPlayerSightsDetailsView> { }
}
