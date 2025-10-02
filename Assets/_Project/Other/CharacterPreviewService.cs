using System;
using UnityEngine;
using Zenject;

public class CharacterPreviewService : IInitializable, IDisposable
{
    private readonly DiContainer _container;
    private readonly UserDataService _userData;
    private GameObject _previewInstance;
    private ClothingApplier _clothing;

    private readonly Vector3 _previewSpawnPoint = new Vector3(0, 0, 0);

    public CharacterPreviewService(DiContainer container, UserDataService userData)
    {
        _container = container;
        _userData = userData;
    }

    public void Initialize()
    {

    }

    public void SpawnPreviewCharacter()
    {
        var prefab = Resources.Load<GameObject>("Character/CharacterBase");
        _previewInstance = _container.InstantiatePrefab(prefab, _previewSpawnPoint, Quaternion.identity, null);

        Debug.Log("CharacterPreviewService initialized");
        _clothing = _previewInstance.GetComponent<ClothingApplier>();
        _clothing.ApplyClothing(_userData.Data);

        _previewInstance.layer = LayerMask.NameToLayer("Preview");
    }

    public void ApplyClothing(int[] newData)
    {
        Debug.Log(_clothing);
        _clothing.ApplyClothing(newData);
    }

    public void Dispose()
    {
        if (_previewInstance != null)
            GameObject.Destroy(_previewInstance);
    }
}