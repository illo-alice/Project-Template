using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class SettingsService
{
    private const int DATA_FORMAT_VERSION = 1;
    private const string CONFIG_DIRECTORY_NAME = "Settings Config";
    private const string BROKEN_DIRECTORY_NAME = "Broken";
    private const string CURRENT_FILE_NAME = "Settings.json";
    private const string BACKUP_FILE_NAME = "Settings.backup.json";
    
    private readonly Dictionary<string, ISettingsParameter> _parametersByKey = new(StringComparer.Ordinal);
    private List<ISettingsParameter> _parameters;
    private readonly DisplaySettingsService _display;
    private readonly SemaphoreSlim _storageGate = new(1, 1);
    private SettingsData _currentSettingsData;
    private bool _isInitialized;
    
    public string CurrentFilePath { get; private set; }
    public string BackupFilePath { get; private set; }
    public string ConfigDirectoryPath { get; private set; }
    public string BrokenDirectoryPath { get; private set; }
    
    public SettingsService(List<ISettingsParameter> parameters, DisplaySettingsService display)
    {
        _parameters = parameters;
        _display = display;
    }

    public async UniTask<SettingsLoadStatus> InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _storageGate.WaitAsync(cancellationToken);
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            _isInitialized = false;
            PrepareStorage();

            foreach (var parameter in _parameters)
                Register(parameter);

            var current = await TryLoadAndApply(CurrentFilePath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (current.loaded)
            {
                _isInitialized = true;
                return SettingsLoadStatus.Loaded;
            }

            var backup = await TryLoadAndApply(BackupFilePath, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (!backup.loaded)
            {
                _currentSettingsData = new SettingsData();
                await ApplyParameters(cancellationToken);
            }

            // Write only after the selected settings have been applied successfully.
            var json = JsonConvert.SerializeObject(_currentSettingsData);
            await FileStorage.WriteAtomicAsync(CurrentFilePath, json, cancellationToken);
            if (!backup.loaded)
                await FileStorage.WriteAtomicAsync(BackupFilePath, json, cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();
            _isInitialized = true;
            return backup.loaded
                ? SettingsLoadStatus.RestoredBackup
                : current.failed || backup.failed
                    ? SettingsLoadStatus.ResetToDefaults
                    : SettingsLoadStatus.CreatedDefaults;
        }
        finally
        {
            _storageGate.Release();
        }
    }

    private void PrepareStorage()
    {
        ConfigDirectoryPath = Path.Combine(Application.persistentDataPath, CONFIG_DIRECTORY_NAME);
        BrokenDirectoryPath = Path.Combine(ConfigDirectoryPath, BROKEN_DIRECTORY_NAME);
        CurrentFilePath = Path.Combine(ConfigDirectoryPath, CURRENT_FILE_NAME);
        BackupFilePath = Path.Combine(ConfigDirectoryPath, BACKUP_FILE_NAME);

        Directory.CreateDirectory(ConfigDirectoryPath);
        Directory.CreateDirectory(BrokenDirectoryPath);
    }

    private async UniTask ApplyParameters(CancellationToken cancellationToken)
    {
        _display.BeginUpdate();
        var applied = false;
        try
        {
            foreach (var parameter in _parameters)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await Load(parameter);
            }
            cancellationToken.ThrowIfCancellationRequested();
            applied = true;
        }
        finally
        {
            _display.EndUpdate(applied);
        }
    }
    
    private SettingsData ParseSettings(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            throw new JsonSerializationException("Settings File is empty.");

        var root = JObject.Parse(json);
        var versionToken = root["version"];

        if (versionToken is not { Type: JTokenType.Integer })
        {
            throw new JsonSerializationException(
                "File do not have version");
        }

        if (versionToken.Value<long>() != DATA_FORMAT_VERSION)
        {
            throw new InvalidDataException(
                $"Unsupported settings file version: {versionToken}, current version: {DATA_FORMAT_VERSION}");
        }

        var data = root.ToObject<SettingsData>();

        if (data == null || data.Values == null)
            throw new JsonSerializationException("There's no settings data found.");

        return data;
    }
    
    private async UniTask<(bool loaded, bool failed)> TryLoadAndApply(string path, CancellationToken cancellationToken)
    {
        var wasRead = false;
        try
        {
            string json;
            try
            {
                json = await File.ReadAllTextAsync(path, cancellationToken);
            }
            catch (FileNotFoundException)
            {
                return (false, false);
            }
            catch (DirectoryNotFoundException)
            {
                return (false, false);
            }

            cancellationToken.ThrowIfCancellationRequested();
            wasRead = true;
            _currentSettingsData = ParseSettings(json);
            await ApplyParameters(cancellationToken);
            return (true, false);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Debug.LogWarning($"Could not load or apply settings from '{path}'. Trying fallback.\n{exception}");
            if (wasRead)
                ArchiveBroken(path);
            return (false, true);
        }
    }

    private void ArchiveBroken(string path)
    {
        try
        {
            FileStorage.MoveToArchive(path, BrokenDirectoryPath);
        }
        catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
        {
            Debug.LogWarning($"Could not archive settings from '{path}'.\n{exception}");
        }
    }
    
    public async UniTask SaveChanges()
    {
        await _storageGate.WaitAsync();
        try
        {
            if (!_isInitialized)
                throw new InvalidOperationException("Settings have not been initialized successfully.");

            var nextData = new SettingsData
            {
                Values = new Dictionary<string, string>(
                    _currentSettingsData.Values,
                    StringComparer.Ordinal)
            };

            foreach (var parameter in _parametersByKey.Values)
            {
                var (shouldSave, data) = await parameter.TrySave();

                if (shouldSave)
                    nextData.Values[parameter.SaveKey] = data;
            }

            var json = JsonConvert.SerializeObject(nextData);

            await FileStorage.WriteAtomicAsync(CurrentFilePath, json);
            await FileStorage.WriteAtomicAsync(BackupFilePath, json);

            _currentSettingsData = nextData;
        }
        finally
        {
            _storageGate.Release();
        }
    }

    private void Register(ISettingsParameter settingsParameter)
    {
        if (_parametersByKey.TryGetValue(settingsParameter.SaveKey, out var existing))
        {
            if (!ReferenceEquals(existing, settingsParameter))
                throw new InvalidOperationException($"Setting '{settingsParameter.SaveKey}' is already registered by another instance.");
            return;
        }

        _parametersByKey.Add(settingsParameter.SaveKey, settingsParameter);
    }

    private async UniTask Load(ISettingsParameter settingsParameter)
    {
        if (!_currentSettingsData.Values.TryGetValue(settingsParameter.SaveKey, out var data))
            await settingsParameter.Reset();
        else
            await settingsParameter.Load(data);
    }

    public IReadOnlyList<ISettingsParameter> GetParameters(string categoryKey = null)
    {
        return _parameters
            .Where(p => string.IsNullOrEmpty(categoryKey) || p.CategoryKey == categoryKey)
            .ToList();
    }

    [JsonObject(MemberSerialization.OptIn)]
    private sealed class SettingsData
    {
        [JsonProperty("version", Required = Required.Always)]
        public int Version = DATA_FORMAT_VERSION;

        [JsonProperty("values", Required = Required.Always)]
        public Dictionary<string, string> Values = new(StringComparer.Ordinal);
    }
}
