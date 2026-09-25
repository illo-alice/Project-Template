using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Fusion;
using UnityEngine;

public sealed class RegionParameter : ChoiceParameter
{
    private readonly List<string> _regionCodes = new();
    private bool _refreshing;
    public override string CategoryKey => ParameterCategory.ONLINE;
    public override string SaveKey => $"{CategoryKey}.Region";
    protected override string DefaultValue => "auto";
    public string FixedRegion => string.IsNullOrEmpty(Value) || Value == "auto" ? null : Value;
    protected override IEnumerable<SettingsChoice> CreateOptions()
    {
        yield return new SettingsChoice("auto", SettingsText.Get("automatic", "Automatic"));
        foreach (var code in _regionCodes.OrderBy(code => code, StringComparer.Ordinal))
            yield return new SettingsChoice(code, code.ToUpperInvariant());
    }
    public override UniTask Load(string data)
    {
        // Retain the saved region while offline; discovery is not required to load settings.
        if (!string.IsNullOrWhiteSpace(data) && data != "auto" && !_regionCodes.Contains(data)) _regionCodes.Add(data);
        return base.Load(data);
    }
    protected override void ApplyValue(string id) { /* Session uses FixedRegion on its next connection. */ }

    public async UniTask RefreshRegionsAsync(CancellationToken cancellationToken)
    {
        if (_refreshing)
        {
            await UniTask.WaitUntil(() => !_refreshing, cancellationToken: cancellationToken);
            return;
        }
        _refreshing = true;
        try
        {
            var regions = await NetworkRunner.GetAvailableRegions(cancellationToken: cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            if (regions == null || regions.Count == 0)
            {
                Debug.LogWarning("Photon returned no regions; keeping the previous region options.");
                return;
            }
            _regionCodes.Clear();
            foreach (var region in regions)
                if (!_regionCodes.Contains(region.RegionCode)) _regionCodes.Add(region.RegionCode);
            if (Value != null && Value != "auto" && !_regionCodes.Contains(Value)) _regionCodes.Add(Value);
            RefreshOptions();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
        catch (Exception exception)
        {
            Debug.LogWarning($"Could not refresh Photon regions; keeping the saved selection. {exception.Message}");
        }
        finally { _refreshing = false; }
    }
}
