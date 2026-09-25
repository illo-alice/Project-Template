using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

internal sealed class GameSaveStorage
{
    private string _savesPath;
    private string _brokenPath;

    public void Initialize(string rootPath)
    {
        _savesPath = Path.Combine(rootPath, "Saves");
        _brokenPath = Path.Combine(rootPath, "Broken");
        Directory.CreateDirectory(_savesPath);
        Directory.CreateDirectory(_brokenPath);
    }

    public async UniTask<Dictionary<string, SaveInfo>> ReadInfos(CancellationToken cancellationToken)
    {
        var saves = new Dictionary<string, SaveInfo>(StringComparer.Ordinal);
        foreach (var folder in Directory.EnumerateDirectories(_savesPath))
        {
            cancellationToken.ThrowIfCancellationRequested();
            var saveId = Path.GetFileName(folder);
            if (!Guid.TryParseExact(saveId, "N", out _)) continue;
            var path = GetSavePath(saveId);
            if (!File.Exists(path)) continue;

            try
            {
                var json = await File.ReadAllTextAsync(path, cancellationToken);
                // Menu cards need metadata only, without world or player state.
                var info = JsonConvert.DeserializeObject<SaveInfo>(json);
                ValidateMetadata(info, saveId);
                info.ImagePath = GetImagePath(saveId);
                try
                {
                    info.PreviewImage = await ReadImage(saveId, cancellationToken);
                }
                catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
                {
                    Debug.LogWarning($"Could not load save preview '{saveId}': {exception.Message}");
                }

                saves.Add(saveId, info);
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException ||
                                               exception is NotSupportedException || exception is IOException ||
                                               exception is UnauthorizedAccessException)
            {
                // Listing saves must never move or delete their files.
                Debug.LogWarning($"Could not list game save '{path}'.\n{exception.Message}");
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        return saves;
    }

    public async UniTask<GameSaveData> Read(string saveId, CancellationToken cancellationToken)
    {
        var json = await File.ReadAllTextAsync(GetSavePath(saveId), cancellationToken);
        cancellationToken.ThrowIfCancellationRequested();
        var save = JsonConvert.DeserializeObject<GameSaveData>(json);
        ValidateMetadata(save, saveId);
        save.ImagePath = GetImagePath(saveId);
        return save;
    }

    public async UniTask Write(GameSaveData save, CancellationToken cancellationToken)
    {
        var path = GetSavePath(save.Id);
        var previousSavedAtUtc = save.SavedAtUtc;
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            Directory.CreateDirectory(GetSaveFolder(save.Id));
            save.SavedAtUtc = DateTime.UtcNow;
            // SaveInfo.PlayerId is serialized as "playerId" alongside the other metadata.
            var json = JsonConvert.SerializeObject(save, Formatting.Indented);
            await FileStorage.WriteAtomicAsync(path, json, cancellationToken);
        }
        catch
        {
            save.SavedAtUtc = previousSavedAtUtc;
            throw;
        }
    }

    public async UniTask WriteImage(SaveInfo save, byte[] image, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetImagePath(save.Id);
        Directory.CreateDirectory(GetSaveFolder(save.Id));
        if (image == null || image.Length == 0)
            File.Delete(path);
        else
            await FileStorage.WriteAtomicAsync(path, image, cancellationToken);

        save.ImagePath = path;
        save.PreviewImage = image == null ? null : (byte[])image.Clone();
    }

    public async UniTask<byte[]> ReadImage(string saveId, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = GetImagePath(saveId);
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }

    public void Delete(string saveId)
    {
        var folder = Path.GetFullPath(GetSaveFolder(saveId));
        var savesPath = Path.GetFullPath(_savesPath);
        if (!string.Equals(Path.GetDirectoryName(folder), savesPath, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Save folder must be inside the saves directory.");

        if (Directory.Exists(folder))
            Directory.Delete(folder, recursive: true);
    }

    public bool TryArchiveBroken(string saveId)
    {
        var folder = GetSaveFolder(saveId);
        try
        {
            FileStorage.MoveToArchive(folder, _brokenPath);
            return true;
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Could not archive game save '{GetSavePath(saveId)}'.\n{exception}");
            return false;
        }
    }

    private static void ValidateMetadata(SaveInfo save, string saveId)
    {
        if (save == null || save.Id != saveId || string.IsNullOrWhiteSpace(save.Name))
            throw new InvalidDataException("Invalid save metadata.");
        if (save.Version != GameSaveData.FormatVersion)
            throw new NotSupportedException($"Unsupported game save version: {save.Version}.");
    }

    public string GetImagePath(string saveId) => Path.Combine(GetSaveFolder(saveId), "preview.png");

    private string GetSavePath(string saveId) => Path.Combine(GetSaveFolder(saveId), "save.json");

    private string GetSaveFolder(string saveId)
    {
        if (!Guid.TryParseExact(saveId, "N", out _))
            throw new ArgumentException("Save ID must be a GUID in N format.", nameof(saveId));
        return Path.Combine(_savesPath, saveId);
    }
}
