using UnityEngine;
using UnityEngine.Audio;

[CreateAssetMenu(menuName = "Game/Audio/Sound", fileName = "New Sound")]
public sealed class SoundDefinition : ScriptableObject
{
    [SerializeField] private AudioClip[] _clips;
    [Tooltip("Defaults to SFX when empty. PlayMusic always uses the Music group.")]
    [SerializeField] private AudioMixerGroup _output;
    [SerializeField, Range(0f, 1f)] private float _volume = 1f;
    [SerializeField] private Vector2 _pitchRange = Vector2.one;
    [SerializeField] private bool _loop;
    [Tooltip("Minimum seconds between starts of this sound across all emitters. Uses unscaled time; 0 disables the cooldown.")]
    [SerializeField, Min(0f)] private float _cooldown;
    [SerializeField, Min(0.01f)] private float _minDistance = 1f;
    [SerializeField, Min(0.01f)] private float _maxDistance = 30f;
    [SerializeField] private AudioRolloffMode _rolloffMode = AudioRolloffMode.Logarithmic;
    [Tooltip("Lower values have higher priority. Loops are never stolen by the pool.")]
    [SerializeField, Range(0, 256)] private int _priority = 128;

    public AudioMixerGroup Output => _output;
    public float Volume => Mathf.Clamp01(FiniteOr(_volume, 1f));
    public bool Loop => _loop;
    public float Cooldown => Mathf.Max(0f, FiniteOr(_cooldown, 0f));
    public float MinDistance => Mathf.Max(0.01f, FiniteOr(_minDistance, 1f));
    public float MaxDistance => Mathf.Max(MinDistance, FiniteOr(_maxDistance, 30f));
    public AudioRolloffMode RolloffMode => _rolloffMode;
    public int Priority => Mathf.Clamp(_priority, 0, 256);

    public float PickPitch()
    {
        var min = Mathf.Clamp(FiniteOr(_pitchRange.x, 1f), 0.01f, 3f);
        var max = Mathf.Clamp(FiniteOr(_pitchRange.y, 1f), min, 3f);
        return Random.Range(min, max);
    }

    public AudioClip PickClip()
    {
        AudioClip selected = null;
        var count = 0;
        if (_clips == null) return null;
        foreach (var clip in _clips)
            if (clip != null && Random.Range(0, ++count) == 0)
                selected = clip;
        return selected;
    }

    private void OnValidate()
    {
        _volume = Volume;
        _cooldown = Cooldown;
        _minDistance = MinDistance;
        _maxDistance = MaxDistance;
        _priority = Priority;
        _pitchRange.x = Mathf.Clamp(FiniteOr(_pitchRange.x, 1f), 0.01f, 3f);
        _pitchRange.y = Mathf.Clamp(FiniteOr(_pitchRange.y, 1f), _pitchRange.x, 3f);
    }

    private static float FiniteOr(float value, float fallback) =>
        float.IsNaN(value) || float.IsInfinity(value) ? fallback : value;
}
