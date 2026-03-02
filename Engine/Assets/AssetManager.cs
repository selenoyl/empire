using System.Text.Json;

namespace Engine.Assets;

public sealed class AssetManager(string contentRoot)
{
    private readonly string _contentRoot = contentRoot;
    public string LoadText(string relativePath) => File.ReadAllText(Path.Combine(_contentRoot, relativePath));
    public T LoadJson<T>(string relativePath) where T : class
    {
        var json = LoadText(relativePath);
        return JsonSerializer.Deserialize<T>(json) ?? throw new InvalidDataException($"Failed to parse {relativePath}");
    }
}
