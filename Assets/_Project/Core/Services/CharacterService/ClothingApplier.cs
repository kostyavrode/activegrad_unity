using UnityEngine;
using System.Collections.Generic;

public class ClothingApplier : MonoBehaviour
{
    [SerializeField] private Transform headSlot;
    [SerializeField] private Transform bodySlot;
    [SerializeField] private Transform legsSlot;
    [SerializeField] private Transform feetSlot;

    private readonly Dictionary<string, GameObject> _currentClothes = new();

    public void ApplyClothing(UserData data)
    {
        LoadAndEquip("Cap", data.cap.ToString(), headSlot);
        LoadAndEquip("Tshirt", data.tshirt.ToString(), bodySlot);
        LoadAndEquip("Pants", data.pants.ToString(), legsSlot);
        LoadAndEquip("Boots", data.boots.ToString(), feetSlot);
    }

    private void LoadAndEquip(string type, string itemName, Transform parent)
    {
        if (_currentClothes.TryGetValue(type, out var oldItem) && oldItem != null)
        {
            Destroy(oldItem);
        }

        if (string.IsNullOrEmpty(itemName))
        {
            _currentClothes[type] = null;
            return;
        }
        
        string path = $"Clothes/{type}/{type}{itemName}";
        GameObject prefab = Resources.Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogWarning($"[ClothingApplier] Prefab не найден по пути: {path}");
            _currentClothes[type] = null;
            return;
        }
        
        GameObject instance = Instantiate(prefab, parent);
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        _currentClothes[type] = instance;
    }
}