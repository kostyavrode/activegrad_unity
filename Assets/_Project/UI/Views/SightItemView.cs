using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SightItemView : MonoBehaviour
{
    public TMP_Text Title;
    public TMP_Text Distance;
    public Image Image;

    public void SetImage(Sprite sprite)
    {
        Image.sprite = sprite;
    }
}