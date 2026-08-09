using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ShopItemView : MonoBehaviour
{
    public TMP_Text Header;
    public TMP_Text Description;
    public TMP_Text Price;
    public Image Image;
    public Button BuyButton;

    public void Init(string header, string description, string price, Image image = null)
    {
        Header.SetText(header);
        Description.SetText(description);
        Price.SetText(price);
        if (image != null)
        {
            Image.sprite = image.sprite;
        }
    }

    public void SetImage(Sprite image)
    {
        Image.sprite = image;
    }
    
    public class Factory : Zenject.PlaceholderFactory<ShopItemView> { }
}
