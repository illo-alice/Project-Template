using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using VContainer.Unity;

public sealed class AudioService : ITickable, IDisposable
{
    private const string MasterVolume = "MasterVolume";
    private const string MusicVolume = "MusicVolume";
    private const string SfxVolume = "SFXVolume";
    private const string VoiceChatVolume = "VoiceChatVolume";
    private const string UIVolume = "UIVolume";
    private const string AmbienceVolume = "AmbienceVolume";

    private const float MinVolumeDb = -80f;
    private const int MaxEffectSources = 48;

    private readonly AudioMixer _mixer;
    private readonly Transform _parent;
    private readonly AudioMixerGroup _sfxGroup;
    private readonly AudioMixerGroup _musicGroup;
    private readonly List<Voice> _voices = new List<Voice>(MaxEffectSources + 2);
    private readonly List<Voice> _effects = new List<Voice>(MaxEffectSources);
    private readonly Dictionary<SoundDefinition, double> _cooldowns = new Dictionary<SoundDefinition, double>();
    private Transform _root;
    private Voice _musicA;
    private Voice _musicB;
    private Voice _currentMusic;
    private ulong _nextId;
    private bool _disposed;

    public AudioService(AudioMixer mixer, Transform parent)
    {
        _mixer = mixer != null ? mixer : throw new ArgumentNullException(nameof(mixer));
        _parent = parent != null ? parent : throw new ArgumentNullException(nameof(parent));
        _sfxGroup = FindGroup("SFX");
        _musicGroup = FindGroup("Music");
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }
    
    public void SetMasterVolume(float value)
    {
        SetVolume(MasterVolume, value);
    }

    public void SetMusicVolume(float value)
    {
        SetVolume(MusicVolume, value);
    }

    public void SetSfxVolume(float value)
    {
        SetVolume(SfxVolume, value);
    }
    
    public void SetVoiceChatVolume(float value)
    {
        SetVolume(VoiceChatVolume, value);
    }

    public void SetUIVolume(float value)
    {
        SetVolume(UIVolume, value);
    }

    public void SetAmbienceVolume(float value)
    {
        SetVolume(AmbienceVolume, value);
    }
    
    private void SetVolume(string parameterName, float value)
    {
        if (_disposed || _mixer == null) return;
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            Debug.LogError($"Invalid volume for '{parameterName}': {value}");
            return;
        }

        value = Mathf.Clamp01(value);

        var decibels = value <= 0f
            ? MinVolumeDb
            : Mathf.Max(MinVolumeDb, 20f * Mathf.Log10(value));

        if (!_mixer.SetFloat(parameterName, decibels))
            Debug.LogError($"Could not set AudioMixer parameter '{parameterName}'.");
    }

    public AudioHandle Play2D(SoundDefinition sound, bool? loop = null) =>
        PlayEffect(sound, Vector3.zero, null, false, loop);

    public AudioHandle PlayAt(SoundDefinition sound, Vector3 position, bool? loop = null) =>
        PlayEffect(sound, position, null, true, loop);

    public AudioHandle PlayFollowing(SoundDefinition sound, Transform target, bool? loop = null)
    {
        if (target == null) return default;
        return PlayEffect(sound, target.position, target, true, loop);
    }

    public AudioHandle PlayMusic(SoundDefinition sound, float fade = 1f)
    {
        if (!TryGetClip(sound, out var clip)) return default;
        if (_currentMusic != null && _currentMusic.Active &&
            _currentMusic.Sound == sound && !_currentMusic.StopAfterFade)
            return new AudioHandle(this, _currentMusic.Id);

        if (IsCoolingDown(sound)) return default;

        _musicA = _musicA ?? CreateVoice("Music A");
        _musicB = _musicB ?? CreateVoice("Music B");
        var next = _currentMusic == _musicA ? _musicB : _musicA;
        // A rapid third request replaces the older tail, keeping at most two music voices.
        Release(next);
        if (_currentMusic != null && _currentMusic.Active) FadeOut(_currentMusic, fade);
        var duration = ValidDuration(fade);
        StartVoice(next, sound, clip, _musicGroup, Vector3.zero, null, false, true,
            duration > 0f ? 0f : sound.Volume);
        _currentMusic = next;
        if (duration > 0f)
        {
            BeginFade(next, sound.Volume, duration, false);
        }
        return new AudioHandle(this, next.Id);
    }

    public void StopMusic(float fade = 1f)
    {
        if (_disposed) return;
        if (_musicA != null && _musicA.Active) FadeOut(_musicA, fade);
        if (_musicB != null && _musicB.Active) FadeOut(_musicB, fade);
        _currentMusic = null;
    }

    public void StopAll(float fadeOut = 0f)
    {
        if (_disposed) return;
        foreach (var voice in _voices)
            if (voice.Active) FadeOut(voice, fadeOut);
        _currentMusic = null;
    }

    public void Tick()
    {
        if (_disposed) return;
        foreach (var voice in _voices)
        {
            if (!voice.Active) continue;
            if (voice.Source == null || (voice.Follows && voice.Target == null))
            {
                Release(voice);
                continue;
            }
            if (voice.Follows) voice.Source.transform.position = voice.Target.position;

            if (voice.FadeDuration > 0f)
            {
                voice.FadeElapsed += Time.unscaledDeltaTime;
                var progress = Mathf.Clamp01(voice.FadeElapsed / voice.FadeDuration);
                voice.Source.volume = Mathf.Lerp(voice.FadeFrom, voice.FadeTo, progress);
                if (progress >= 1f)
                {
                    voice.FadeDuration = 0f;
                    if (voice.StopAfterFade)
                    {
                        Release(voice);
                        continue;
                    }
                }
            }

            // isPlaying is false while the listener is paused and during asynchronous loading.
            if (AudioListener.pause || voice.Source.isPlaying) continue;
            if (voice.Source.clip != null && voice.Source.clip.loadState == AudioDataLoadState.Loading) continue;
            if (Time.frameCount > voice.StartFrame) Release(voice);
        }
    }

    internal bool IsPlaying(ulong id) => FindVoice(id) != null;

    internal void Stop(ulong id, float fadeOut)
    {
        var voice = FindVoice(id);
        if (voice != null) FadeOut(voice, fadeOut);
    }

    public void Dispose()
    {
        if (_disposed) return;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        foreach (var voice in _voices) Release(voice);
        _disposed = true;
        if (_root != null) UnityEngine.Object.Destroy(_root.gameObject);
        _voices.Clear();
        _effects.Clear();
        _cooldowns.Clear();
        _currentMusic = null;
    }

    private AudioHandle PlayEffect(SoundDefinition sound, Vector3 position, Transform target,
        bool spatial, bool? loop)
    {
        if (!TryGetClip(sound, out var clip)) return default;
        if (IsCoolingDown(sound)) return default;
        if (!IsFinite(position.x) || !IsFinite(position.y) || !IsFinite(position.z))
        {
            Debug.LogWarning("Cannot play audio at a non-finite position.");
            return default;
        }
        var voice = AcquireEffect(sound.Priority);
        if (voice == null) return default;
        StartVoice(voice, sound, clip, sound.Output != null ? sound.Output : _sfxGroup,
            position, target, spatial, loop ?? sound.Loop);
        return new AudioHandle(this, voice.Id);
    }

    private bool IsCoolingDown(SoundDefinition sound) =>
        sound.Cooldown > 0f && _cooldowns.TryGetValue(sound, out var nextStart) &&
        Time.unscaledTimeAsDouble < nextStart;

    private bool TryGetClip(SoundDefinition sound, out AudioClip clip)
    {
        clip = null;
        if (_disposed || _parent == null || sound == null) return false;
        clip = sound.PickClip();
        if (clip != null) return true;
        Debug.LogWarning($"Sound '{sound.name}' has no assigned clips.", sound);
        return false;
    }

    private Voice AcquireEffect(int priority)
    {
        foreach (var voice in _effects)
            if (!voice.Active) return voice;
        if (_effects.Count < MaxEffectSources)
        {
            var voice = CreateVoice($"Effect {_effects.Count + 1}");
            _effects.Add(voice);
            return voice;
        }
        Voice oldest = null;
        foreach (var voice in _effects)
            if (!voice.Source.loop && voice.Source.priority >= priority &&
                (oldest == null || voice.Id < oldest.Id)) oldest = voice;
        if (oldest != null) Release(oldest);
        // Keep loops and higher-priority effects intact when all 48 slots are occupied.
        return oldest;
    }

    private Voice CreateVoice(string name)
    {
        if (_root == null)
        {
            _root = new GameObject("Audio Playback").transform;
            _root.SetParent(_parent, false);
        }
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(_root, false);
        var source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        gameObject.SetActive(false);
        var voice = new Voice { Source = source };
        _voices.Add(voice);
        return voice;
    }

    private void StartVoice(Voice voice, SoundDefinition sound, AudioClip clip, AudioMixerGroup output,
        Vector3 position, Transform target, bool spatial, bool loop, float? initialVolume = null)
    {
        var source = voice.Source;
        source.gameObject.SetActive(true);
        source.transform.position = position;
        source.outputAudioMixerGroup = output;
        source.clip = clip;
        source.volume = initialVolume ?? sound.Volume;
        source.pitch = sound.PickPitch();
        source.loop = loop;
        source.spatialBlend = spatial ? 1f : 0f;
        source.minDistance = sound.MinDistance;
        source.maxDistance = sound.MaxDistance;
        source.rolloffMode = sound.RolloffMode;
        source.priority = sound.Priority;
        source.dopplerLevel = 0f;
        source.panStereo = 0f;
        source.spread = 0f;
        source.ignoreListenerPause = false;
        source.mute = false;
        voice.Id = ++_nextId;
        voice.Active = true;
        voice.Sound = sound;
        voice.Target = target;
        voice.Follows = target != null;
        voice.Spatial = spatial;
        voice.SceneHandle = target != null ? target.gameObject.scene.handle : SceneManager.GetActiveScene().handle;
        voice.StartFrame = Time.frameCount;
        voice.FadeDuration = 0f;
        voice.StopAfterFade = false;
        source.Play();
        // Only a successful start consumes the cooldown; rejected requests never extend it.
        if (sound.Cooldown > 0f)
            _cooldowns[sound] = Time.unscaledTimeAsDouble + sound.Cooldown;
    }

    private Voice FindVoice(ulong id)
    {
        if (_disposed || id == 0) return null;
        foreach (var voice in _voices)
            if (voice.Active && voice.Id == id) return voice;
        return null;
    }

    private static void Release(Voice voice)
    {
        if (voice.Source != null)
        {
            voice.Source.Stop();
            voice.Source.clip = null;
            voice.Source.gameObject.SetActive(false);
        }
        voice.Active = false;
        voice.Sound = null;
        voice.Target = null;
        voice.FadeDuration = 0f;
        voice.StopAfterFade = false;
    }

    private static void FadeOut(Voice voice, float duration)
    {
        duration = ValidDuration(duration);
        if (duration <= 0f || voice.Source == null) Release(voice);
        else if (!voice.StopAfterFade) BeginFade(voice, 0f, duration, true);
    }

    private static void BeginFade(Voice voice, float volume, float duration, bool stop)
    {
        voice.FadeFrom = voice.Source.volume;
        voice.FadeTo = volume;
        voice.FadeElapsed = 0f;
        voice.FadeDuration = duration;
        voice.StopAfterFade = stop;
    }

    private AudioMixerGroup FindGroup(string name)
    {
        foreach (var group in _mixer.FindMatchingGroups(name))
            if (group.name == name) return group;
        throw new InvalidOperationException($"Audio mixer '{_mixer.name}' has no '{name}' group.");
    }

    private void OnSceneUnloaded(Scene scene)
    {
        foreach (var voice in _voices)
            if (voice.Active && voice.Spatial && voice.SceneHandle == scene.handle) Release(voice);
    }

    private static float ValidDuration(float value) => IsFinite(value) ? Mathf.Max(0f, value) : 0f;
    private static bool IsFinite(float value) => !float.IsNaN(value) && !float.IsInfinity(value);

    private sealed class Voice
    {
        public AudioSource Source;
        public SoundDefinition Sound;
        public Transform Target;
        public ulong Id;
        public bool Active;
        public bool Follows;
        public bool Spatial;
        public SceneHandle SceneHandle;
        public int StartFrame;
        public float FadeFrom;
        public float FadeTo;
        public float FadeElapsed;
        public float FadeDuration;
        public bool StopAfterFade;
    }
}
