using System.Threading.Tasks;
using UnityEngine;

public interface IImageLoadService
{
    Task<Sprite> LoadSpriteAsync(string url);
    Task<Texture2D> LoadTextureAsync(string url);
}