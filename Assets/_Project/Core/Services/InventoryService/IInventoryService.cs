using System;
using System.Collections.Generic;

public interface IInventoryService
{
    IReadOnlyDictionary<string, int> Resources { get; }
    int GetResourceAmount(string resourceId);
    bool HasItem(string itemId);

    event Action OnInventoryUpdated;

    void SetResources(APIService.InventoryResourceData[] resources);
    void SetItems(APIService.InventoryItemData[] items);
    void ApplyInventoryData(APIService.InventoryData data);
}
