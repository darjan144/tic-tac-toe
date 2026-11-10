using System.Text.Json;

namespace SoundAds.Server;

/// <summary>Loads creatives from <c>ads.json</c> and selects one per request (format + moment targeting).</summary>
public sealed class AdInventory
{
    static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    readonly IReadOnlyList<AdCreative> _creatives;

    public AdInventory(IReadOnlyList<AdCreative> creatives) => _creatives = creatives;

    public IReadOnlyList<AdCreative> All => _creatives;

    public static AdInventory Load(string inventoryPath, string audioFolder, ILogger logger)
    {
        var creatives = JsonSerializer.Deserialize<List<AdCreative>>(File.ReadAllText(inventoryPath), JsonOptions) ?? [];

        foreach (var ad in creatives)
        {
            string path = Path.Combine(audioFolder, ad.AudioFile);
            ad.DurationSec = File.Exists(path) ? WavDuration(path) : 0f;
            if (ad.DurationSec <= 0f)
                logger.LogWarning("Audio for {AdId} missing or unreadable: {Path}", ad.AdId, path);
        }

        logger.LogInformation("Loaded {Count} creatives from {Path}", creatives.Count, inventoryPath);
        return new AdInventory(creatives);
    }

    /// <summary>
    /// Picks a random creative matching the format whose target moments include the requested moment
    /// (or that targets every moment). Returns null for no fill.
    /// </summary>
    public AdCreative? Select(string format, string moment, Random random)
    {
        var candidates = _creatives
            .Where(a => a.DurationSec > 0f)
            .Where(a => string.Equals(a.Format, format, StringComparison.OrdinalIgnoreCase))
            .Where(a => a.TargetMoments.Length == 0 ||
                        a.TargetMoments.Contains(moment, StringComparer.OrdinalIgnoreCase))
            .ToList();

        return candidates.Count == 0 ? null : candidates[random.Next(candidates.Count)];
    }

    /// <summary>Reads duration from a PCM WAV header (byte rate + data chunk size).</summary>
    internal static float WavDuration(string path)
    {
        try
        {
            using var reader = new BinaryReader(File.OpenRead(path));
            if (new string(reader.ReadChars(4)) != "RIFF") return 0f;
            reader.ReadInt32();
            if (new string(reader.ReadChars(4)) != "WAVE") return 0f;

            int byteRate = 0;
            while (reader.BaseStream.Position < reader.BaseStream.Length - 8)
            {
                string chunkId = new(reader.ReadChars(4));
                int chunkSize = reader.ReadInt32();

                if (chunkId == "fmt ")
                {
                    reader.ReadInt16();           // audio format
                    reader.ReadInt16();           // channels
                    reader.ReadInt32();           // sample rate
                    byteRate = reader.ReadInt32();
                    reader.BaseStream.Seek(chunkSize - 12, SeekOrigin.Current);
                }
                else if (chunkId == "data")
                {
                    return byteRate > 0 ? chunkSize / (float)byteRate : 0f;
                }
                else
                {
                    reader.BaseStream.Seek(chunkSize, SeekOrigin.Current);
                }
            }
        }
        catch (IOException) { } // includes EndOfStreamException (truncated file)

        return 0f;
    }
}
