using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using Random = UnityEngine.Random;

public class AudioManager : MonoBehaviour
{
    private static AudioManager instance;
    
    [SerializeField] private AudioMixer audioMixer;
    [SerializeField] private List<SceneBGMData> bgmDatas = new List<SceneBGMData>();
    [SerializeField] private List<UIAudioData> uiDatas = new List<UIAudioData>();
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource uiSource;

    private Dictionary<Define.Scene, SceneBGMData> bgmClips = new();
    private Dictionary<UISoundType, UIAudioData> uiClips = new();
    private AudioMixerGroup sfxMixerGroup;
    public IReadOnlyDictionary<Define.Scene, SceneBGMData> BgmClips => bgmClips;
    public IReadOnlyDictionary<UISoundType, UIAudioData> UiClips => uiClips;
    public static AudioManager Instance => instance;
    public AudioMixerGroup SfxMixerGroup => sfxMixerGroup;
    public AudioSource SfxSource => sfxSource;
    
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
        
        foreach (var audioData in bgmDatas) bgmClips[audioData.SceneType] = audioData;
        foreach (var audioData in uiDatas) uiClips[audioData.SoundType] = audioData;
        sfxMixerGroup = audioMixer?.FindMatchingGroups("SFX")[0];

    }

    private void Start()
    {
        // 저장된 볼륨 불러오기
        LoadVolumes();
    }
    
    // BGM 재생 — volume 곱셈 제거 (Mixer가 처리)
    public void PlayBGM(Define.Scene sceneType)
    {
        var resolved = ResolveFusionScene(sceneType);
        if (!bgmClips.TryGetValue(resolved, out var audioData)) return;
        if (audioData?.AudioClip == null) return;
        bgmSource.clip = audioData.AudioClip;
        bgmSource.Play();
        
        /*
        if (!bgmClips.TryGetValue(sceneType, out var audioData)) return;
        if (audioData?.AudioClip == null) return;
        bgmSource.clip = audioData.AudioClip;
        bgmSource.Play();*/
    }
    
    private Define.Scene ResolveFusionScene(Define.Scene sceneType)
    {
        var parts = sceneType.ToString().Split('_');
        if (parts.Length <= 1) return sceneType;

        var chosen = parts[Random.Range(0, parts.Length)];
        return Enum.TryParse<Define.Scene>(chosen, out var resolved) ? resolved : sceneType;
    }
    
    public void PauseBGM()
    {
        bgmSource.Pause();
    }

    public void ResumeBGM()
    {
        bgmSource.UnPause();
    }

    // SFX 재생 — volume 곱셈 제거
    public void PlaySFX(AudioData audioData)
    {
        if (audioData?.AudioClip == null) return;
        sfxSource.PlayOneShot(audioData.AudioClip);
    }
    
    public void PlayUISound(UISoundType soundType)
    {
        if (uiClips.TryGetValue(soundType, out var audioData))
            uiSource.PlayOneShot(audioData.AudioClip);
    }
    
    // ── 볼륨 제어 (옵션 UI 슬라이더에서 호출) ──────────────────
    // value: 0.0001 ~ 1 (슬라이더 min을 0.0001로 설정해야 -80dB 방지)
    public void SetMasterVolume(float value)
    {
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20f);
        SaveManager.Settings.masterVolume = value;
        SaveManager.SaveSettings();
    }

    public void SetBGMVolume(float value)
    {
        audioMixer.SetFloat("BGMVolume", Mathf.Log10(value) * 20f);
        SaveManager.Settings.bgmVolume = value;
        SaveManager.SaveSettings();
    }

    public void SetSFXVolume(float value)
    {
        audioMixer.SetFloat("GameSFXVolume", Mathf.Log10(value) * 20f);
        SaveManager.Settings.gameSfxVolume = value;
        SaveManager.SaveSettings();
    }

    public void SetUISFXVolume(float value)
    {
        audioMixer.SetFloat("UISFXVolume", Mathf.Log10(value) * 20f);
        SaveManager.Settings.uiSfxVolume = value;
        SaveManager.SaveSettings();
    }

    public void SetMasterMute(bool muted)
    {
        audioMixer.SetFloat("MasterVolume", muted ? -80f : Mathf.Log10(SaveManager.Settings.masterVolume) * 20f);
    }

    public void SetBGMMute(bool muted)
    {
        audioMixer.SetFloat("BGMVolume", muted ? -80f : Mathf.Log10(SaveManager.Settings.bgmVolume) * 20f);
    }

    public void SetSFXMute(bool muted)
    {
        audioMixer.SetFloat("GameSFXVolume", muted ? -80f : Mathf.Log10(SaveManager.Settings.gameSfxVolume) * 20f);
    }

    public void SetUISFXMute(bool muted)
    {
        audioMixer.SetFloat("UISFXVolume", muted ? -80f : Mathf.Log10(SaveManager.Settings.uiSfxVolume) * 20f);
    }

    private void LoadVolumes()
    {
        var s = SaveManager.Settings;
        SetMasterVolume(s.masterVolume);
        SetBGMVolume(s.bgmVolume);
        SetSFXVolume(s.gameSfxVolume);
        SetUISFXVolume(s.uiSfxVolume);

        if (s.masterMute)  SetMasterMute(true);
        if (s.bgmMute)     SetBGMMute(true);
        if (s.gameSfxMute) SetSFXMute(true);
        if (s.uiSfxMute)   SetUISFXMute(true);
    }
}
