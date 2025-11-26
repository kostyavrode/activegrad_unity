using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

public class ImageLoadService : IImageLoadService
{
    private readonly Dictionary<string, Sprite> _spriteCache = new();
    private readonly Dictionary<string, Texture2D> _textureCache = new();

    public async Task<Sprite> LoadSpriteAsync(string url)
    {
        if (_spriteCache.TryGetValue(url, out var cachedSprite))
            return cachedSprite;
        
        var texture = await LoadTextureAsync(url);
        if (texture == null)
            return null;

        var sprite = Sprite.Create(
            texture,
            new Rect(0, 0, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        _spriteCache[url] = sprite;
        return sprite;
    }

    public async Task<Texture2D> LoadTextureAsync(string url)
    {
        if (_textureCache.TryGetValue(url, out var cachedTex))
            return cachedTex;

        using var request = UnityWebRequestTexture.GetTexture(url);
        await request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"ImageLoader: Failed to load image: {url}");
            return null;
        }

        var texture = DownloadHandlerTexture.GetContent(request);

        _textureCache[url] = texture;
        return texture;
    }
}