using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

public class GameSaveService : IDisposable
{
    private readonly Session _session;
    private readonly GameSaveStorage _storage = new();
    private readonly SemaphoreSlim _storageGate = new(1, 1);
    private readonly Dictionary<string, PlayerSaver> _playerSavers = new(StringComparer.Ordinal);
    private readonly Dictionary<string, IDataManipulator> _worldSavers = new(StringComparer.Ordinal);

    private Dictionary<string, SaveInfo> _saves = new(StringComparer.Ordinal);
    private IReadOnlyList<SaveInfo> _visibleSaves = Array.Empty<SaveInfo>();
    private GameSaveData _currentSave;

    public event Action SavesChanged;

    public SaveInfo CurrentSave => _currentSave != null && IsCurrentPlayer(_currentSave.PlayerId)
        ? _currentSave : null;

    public GameSaveService(Session session)
    {
        _session = session;
        _session.PlayerIdChanged += RefreshVisibleSaves;
    }

    public void Dispose() => _session.PlayerIdChanged -= RefreshVisibleSaves;

    public async UniTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        _storage.Initialize(Application.persistentDataPath);
        await RefreshSavesAsync(cancellationToken);
    }

    public IReadOnlyList<SaveInfo> GetSavesForUI() => _visibleSaves;

    // image contains PNG bytes. The owner is assigned once, when the save is created.
    public async UniTask<SaveInfo> Create(string name, byte[] image = null, CancellationToken cancellationToken = default)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            var playerId = RequirePlayerId(); // Session gets this from Photon Fusion's NetworkRunner.UserId.
            var save = new GameSaveData
            {
                Id = Guid.NewGuid().ToString("N"),
                PlayerId = playerId,
                Name = name.Trim(),
                CreatedAtUtc = DateTime.UtcNow
            };

            await _storage.WriteImage(save, image, cancellationToken);
            RequireSaveOwner(save);
            await _storage.Write(save, cancellationToken);

            _currentSave = save;
            UpdateSaveList(save);
            return save;
        }
        finally
        {
            _storageGate.Release();
        }
    }

    public UniTask Save(CancellationToken cancellationToken = default) =>
        SaveCurrent(savePlayers: true, saveWorld: true, cancellationToken: cancellationToken);

    public UniTask SavePlayers(CancellationToken cancellationToken = default) =>
        SaveCurrent(savePlayers: true, saveWorld: false, cancellationToken: cancellationToken);

    public UniTask SaveWorld(CancellationToken cancellationToken = default) =>
        SaveCurrent(savePlayers: false, saveWorld: true, cancellationToken: cancellationToken);

    public UniTask Delete(string saveId, CancellationToken cancellationToken = default) =>
        Delete(saveId, RequirePlayerId(), cancellationToken);

    // Failed startup can disconnect Fusion; use the owner captured when that attempt created the save.
    internal async UniTask Delete(string saveId, string playerId, CancellationToken cancellationToken = default)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            var save = _saves.TryGetValue(saveId, out var info)
                ? info : await _storage.Read(saveId, cancellationToken);
            if (string.IsNullOrWhiteSpace(playerId) || !string.Equals(save.PlayerId, playerId, StringComparison.Ordinal))
                throw new InvalidOperationException("This game save belongs to a different player.");

            cancellationToken.ThrowIfCancellationRequested();
            _storage.Delete(saveId);
            _saves.Remove(saveId);
            if (_currentSave?.Id == saveId)
                _currentSave = null;
            RefreshVisibleSaves();
        }
        finally
        {
            _storageGate.Release();
        }
    }

    public async UniTask Load(string saveId, CancellationToken cancellationToken = default)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            GameSaveData save;
            try
            {
                save = await _storage.Read(saveId, cancellationToken);
            }
            catch (Exception exception) when (exception is JsonException || exception is InvalidDataException)
            {
                if (_storage.TryArchiveBroken(saveId))
                {
                    _saves.Remove(saveId);
                    RefreshVisibleSaves();
                }
                throw;
            }

            // A different owner is not a corrupt file. Keep both the file and previous selection intact.
            cancellationToken.ThrowIfCancellationRequested();
            RequireSaveOwner(save);
            save.PreviewImage = _saves.TryGetValue(saveId, out var info) ? info.PreviewImage : null;
            _currentSave = save;
        }
        finally
        {
            _storageGate.Release();
        }
    }

    public async UniTask RefreshSavesAsync(CancellationToken cancellationToken = default)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            var saves = await _storage.ReadInfos(cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            _saves = saves;
            if (_currentSave != null)
                _currentSave.PreviewImage = _saves.TryGetValue(_currentSave.Id, out var info) ? info.PreviewImage : null;
            RefreshVisibleSaves();
        }
        finally
        {
            _storageGate.Release();
        }
    }

    // Passing null clears the preview without changing the JSON or save timestamp.
    public async UniTask SetImage(byte[] image, CancellationToken cancellationToken = default)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            var save = RequireCurrentSave();
            await _storage.WriteImage(save, image, cancellationToken);
            UpdateSaveList(save);
        }
        finally
        {
            _storageGate.Release();
        }
    }

    public async UniTask<byte[]> LoadImage(string saveId, CancellationToken cancellationToken = default)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            return await _storage.ReadImage(saveId, cancellationToken);
        }
        finally
        {
            _storageGate.Release();
        }
    }

    public void BindPlayer(string playerId, PlayerSaver playerSaver)
    {
        if (string.IsNullOrWhiteSpace(playerId))
            throw new ArgumentException("Player ID is required.", nameof(playerId));
        if (playerSaver == null)
            throw new ArgumentNullException(nameof(playerSaver));

        if (_playerSavers.TryGetValue(playerId, out var existing) && !ReferenceEquals(existing, playerSaver))
            throw new InvalidOperationException($"Player '{playerId}' already has a registered PlayerSaver.");

        _playerSavers[playerId] = playerSaver;
    }

    public void UnbindPlayer(string playerId, PlayerSaver playerSaver)
    {
        // A late despawn must not remove a newer instance registered for the same account.
        if (IsPlayerBound(playerId, playerSaver))
            _playerSavers.Remove(playerId);
    }

    // Key must be unique in the world and remain unchanged while registered.
    public void BindWorld(IDataManipulator dataManipulator)
    {
        if (dataManipulator == null)
            throw new ArgumentNullException(nameof(dataManipulator));
        var key = dataManipulator.Key;
        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("World data key is required.", nameof(dataManipulator));

        if (_worldSavers.TryGetValue(key, out var existing) && !ReferenceEquals(existing, dataManipulator))
            throw new InvalidOperationException($"World data '{key}' already has a registered IDataManipulator.");

        _worldSavers[key] = dataManipulator;
    }

    public void UnbindWorld(IDataManipulator dataManipulator)
    {
        if (IsWorldBound(dataManipulator.Key, dataManipulator))
            _worldSavers.Remove(dataManipulator.Key);
    }

    private async UniTask SaveCurrent(bool savePlayers, bool saveWorld, CancellationToken cancellationToken)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            var save = RequireCurrentSave();
            if (savePlayers) await GatherPlayers(save);
            if (saveWorld) await GatherWorld(save);

            // Identity may have changed while components were gathering their state.
            RequireSaveOwner(save);
            await _storage.Write(save, cancellationToken);
            UpdateSaveList(save);
        }
        finally
        {
            _storageGate.Release();
        }
    }

    private async UniTask GatherPlayers(GameSaveData save)
    {
        // Registrations can change while a component's async Save is running.
        foreach (var (playerId, saver) in _playerSavers.ToArray())
        {
            if (saver == null || !IsPlayerBound(playerId, saver)) continue;
            var data = await saver.Save();
            if (IsPlayerBound(playerId, saver))
                save.Players[playerId] = data;
        }
    }

    private async UniTask GatherWorld(GameSaveData save)
    {
        foreach (var (key, saver) in _worldSavers.ToArray())
        {
            if (!IsWorldBound(key, saver)) continue;
            var data = await saver.Save();
            if (IsWorldBound(key, saver))
                save.World.Values[key] = data;
        }
    }

    private bool IsPlayerBound(string playerId, PlayerSaver saver) =>
        playerId != null && _playerSavers.TryGetValue(playerId, out var existing) && ReferenceEquals(existing, saver);

    private bool IsWorldBound(string key, IDataManipulator saver) =>
        _worldSavers.TryGetValue(key, out var existing) && ReferenceEquals(existing, saver);

    private string RequirePlayerId()
    {
        if (string.IsNullOrWhiteSpace(_session.PlayerId))
            throw new InvalidOperationException("Connect to Photon Fusion before accessing game saves.");
        return _session.PlayerId;
    }

    private bool IsCurrentPlayer(string playerId) =>
        !string.IsNullOrWhiteSpace(_session.PlayerId) &&
        string.Equals(playerId, _session.PlayerId, StringComparison.Ordinal);

    private void RequireSaveOwner(SaveInfo save)
    {
        if (!string.Equals(save.PlayerId, RequirePlayerId(), StringComparison.Ordinal))
            throw new InvalidOperationException("This game save belongs to a different player.");
    }

    private GameSaveData RequireCurrentSave()
    {
        if (_currentSave == null)
            throw new InvalidOperationException("No game save is selected.");
        RequireSaveOwner(_currentSave);
        return _currentSave;
    }

    private void UpdateSaveList(GameSaveData save)
    {
        _saves[save.Id] = save;
        RefreshVisibleSaves();
    }

    private void RefreshVisibleSaves()
    {
        _visibleSaves = Array.AsReadOnly(_saves.Values
            .Where(save => IsCurrentPlayer(save.PlayerId))
            .OrderByDescending(save => save.SavedAtUtc)
            .ThenBy(save => save.Id, StringComparer.Ordinal)
            .ToArray());
        SavesChanged?.Invoke();
    }
}
