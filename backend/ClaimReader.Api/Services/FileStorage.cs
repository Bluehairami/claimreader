namespace ClaimReader.Api.Services;

public class FileStorage(IConfiguration config, IHostEnvironment env)
{
    private readonly string root = Path.GetFullPath(config["Storage:Path"] ?? "uploads", env.ContentRootPath);

    /// <summary>Saves the bytes under a generated name and returns that name (not a path).</summary>
    public async Task<string> SaveAsync(byte[] bytes, string extension, CancellationToken ct)
    {
        Directory.CreateDirectory(root);
        var name = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        await File.WriteAllBytesAsync(Path.Combine(root, name), bytes, ct);
        return name;
    }

    public string GetPath(string storedName) => Path.Combine(root, Path.GetFileName(storedName));
}