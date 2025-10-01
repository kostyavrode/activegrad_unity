using System;
using UnityEngine;
using Zenject;

public class GPSTest : MonoBehaviour
{
    [Inject] private LocationService LocationSerice;

    private void Start()
    {
        Debug.Log(LocationSerice.GetCoordinates());
    }
}
