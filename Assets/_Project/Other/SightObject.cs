using UnityEngine;

public class SightObject : MonoBehaviour
{
    private int pageID;

    public void SetPageID(int pageID)
    {
        this.pageID = pageID;
    }

    public int GetSightInfo()
    {
        return pageID;
    }
}
