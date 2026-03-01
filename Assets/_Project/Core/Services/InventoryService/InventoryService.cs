using System;
using System.Collections.Generic;
using System.Linq;

public class InventoryService : IInventoryService
{
    private readonly Dictionary<string, int> _resources = new Dictionary<string, int>();
    private readonly Dictionary<string, bool> _hasItem = new Dictionary<string, bool>();

    public IReadOnlyDictionary<string, int> Resources => _resources;

    public int GetResourceAmount(string resourceId)
    {
        return _resources.TryGetValue(resourceId ?? "", out var amount) ? amount : 0;
    }

    public bool HasItem(string itemId)
    {
        return _hasItem.TryGetValue(itemId ?? "", out var has) && has;
    }

    public event Action OnInventoryUpdated;

    public void SetResources(APIService.InventoryResourceData[] resources)
    {
        _resources.Clear();
        if (resources == null) return;

        foreach (var r in resources)
        {
            if (!string.IsNullOrEmpty(r?.id))
                _resources[r.id] = r.amount;
        }
        OnInventoryUpdated?.Invoke();
    }

    public void SetItems(APIService.InventoryItemData[] items)
    {
        _hasItem.Clear();
        if (items == null) return;

        foreach (var item in items)
        {
            if (!string.IsNullOrEmpty(item?.id))
                _hasItem[item.id] = item.has_item;
        }
        OnInventoryUpdated?.Invoke();
    }

    public void ApplyInventoryData(APIService.InventoryData data)
    {
        if (data == null) return;

        if (data.resources != null)
            SetResources(data.resources);

        if (data.items != null)
            SetItems(data.items);
    }
}
