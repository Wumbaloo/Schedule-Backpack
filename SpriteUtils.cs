using MelonLoader;
using UnityEngine;

namespace BackpackMod;

public static class SpriteUtils
{
    public static Sprite LoadSpriteFromEmbeddedResource(string resourceName, int width = 64, int height = 64)
    {
        var assembly = typeof(Core).Assembly;
        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            MelonLogger.Error($"Resource '{resourceName}' not found!");
            return null;
        }

        var bytes = new byte[stream.Length];
        stream.Read(bytes, 0, bytes.Length);

        var texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
        texture.LoadImage(bytes);
        texture.filterMode = FilterMode.Point;
        texture.Apply();


        return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
    }
}
