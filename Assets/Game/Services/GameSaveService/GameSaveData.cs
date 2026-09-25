using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// The same metadata is read from save.json and displayed in the menu.
[JsonObject(MemberSerialization.OptIn)]
public class SaveInfo
{
    [JsonProperty("version", Required = Required.Always)]
    public int Version { get; internal set; } = GameSaveData.FormatVersion;

    [JsonProperty("id", Required = Required.Always)]
    public string Id { get; internal set; }

    // Missing in older saves: these stay on disk but are hidden from the menu.
    [JsonProperty("playerId")]
    public string PlayerId { get; internal set; }

    [JsonProperty("name", Required = Required.Always)]
    public string Name { get; internal set; }

    [JsonProperty("createdAtUtc", Required = Required.Always)]
    public DateTime CreatedAtUtc { get; internal set; }

    [JsonProperty("savedAtUtc", Required = Required.Always)]
    public DateTime SavedAtUtc { get; internal set; }

    [JsonIgnore]
    public string ImagePath { get; internal set; }

    // Cached PNG bytes for UI; treat as read-only.
    [JsonIgnore]
    public byte[] PreviewImage { get; internal set; }
}

[JsonObject(MemberSerialization.OptIn)]
internal sealed class GameSaveData : SaveInfo
{
    public const int FormatVersion = 1;

    [JsonProperty("world", Required = Required.Always)]
    public WorldSaveData World = new();

    [JsonProperty("players", Required = Required.Always)]
    public Dictionary<string, PlayerSaveData> Players = new(StringComparer.Ordinal);
}

public class WorldSaveData
{
    public Dictionary<string, string> Values = new();
}

public class PlayerSaveData
{
    public Dictionary<string, string> Values = new();
}
