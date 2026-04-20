using UnityEngine;

public class PartnerStoreObject : MonoBehaviour
{
    private int _storeId;

    public void SetStoreID(int storeId)
    {
        _storeId = storeId;
    }

    public int GetStoreID()
    {
        return _storeId;
    }
}
