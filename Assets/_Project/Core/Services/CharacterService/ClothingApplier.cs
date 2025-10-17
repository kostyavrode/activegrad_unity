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
    public void ApplyClothing(int[] data)
    {
        LoadAndEquip("Cap", data[0].ToString(), headSlot);
        LoadAndEquip("Tshirt", data[1].ToString(), bodySlot);
        LoadAndEquip("Pants", data[2].ToString(), legsSlot);
        LoadAndEquip("Boots", data[3].ToString(), feetSlot);
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
            _currentClothes[type] = null;
            return;
        }
        
        GameObject instance = Instantiate(prefab, parent);
        var bodyAnimator = GetComponentInChildren<Animator>();
        var bodyRenderer = bodyAnimator.GetComponentInChildren<SkinnedMeshRenderer>();
        var newRenderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();

        if (newRenderer != null && bodyRenderer != null)
        {
            newRenderer.rootBone = bodyRenderer.rootBone;
            newRenderer.bones = bodyRenderer.bones;
        }

        
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;

        _currentClothes[type] = instance;
    }
}