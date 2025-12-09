using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class OtherSightsView : MonoBehaviour
{
    [SerializeField] private Transform _contentParent;
    [SerializeField] private Button _closeButton;
    
    public Transform ContentParent => _contentParent;
    
    public event Action OnCloseClicked;
    
    private void Awake()
    {
        _closeButton.onClick.AddListener(() => OnCloseClicked?.Invoke());
    }
    
    private void OnDestroy()
    {
        _closeButton.onClick.RemoveAllListeners();
    }
    
    
}

public class OtherSightViewFactory : PlaceholderFactory<OtherSightsView>{}
