using System;

[Serializable]
public class SightShortInfo
{
    public int PageId;
    public string Title;
    public float Distance;
    public double Latitude;
    public double Longitude;
}

[Serializable]
public class SightFullInfo
{
    public int PageId;
    public string Title;
    public string Description;
    public string ImageUrl;
}