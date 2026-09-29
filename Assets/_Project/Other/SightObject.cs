using UnityEngine;

public class SightObject : MonoBehaviour
{
    private int pageID;

    public void SetPageID(int pageID)
    {
        this.pageID = pageID;

        if (!TryGetComponent(out SightMarkerFx fx))
            fx = gameObject.AddComponent<SightMarkerFx>();
        fx.Init(pageID);
    }

    public int GetSightInfo()
    {
        return pageID;
    }
}
