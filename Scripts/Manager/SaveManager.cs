using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using Steamworks;

[Serializable]
public class StringBoolMap
{
    [SerializeField] private List<string> keys = new();
    [SerializeField] private List<bool> values = new();

    public bool Get(string key, bool defaultValue = false)
    {
        int i = keys.IndexOf(key);
        return i >= 0 ? values[i] : defaultValue;
    }

    public void Set(string key, bool value)
    {
        int i = keys.IndexOf(key);
        if (i >= 0) values[i] = value;
        else { keys.Add(key); values.Add(value); }
    }

    public void Clear()
    {
        keys.Clear();
        values.Clear();
    }
}

[Serializable]
public class GameSaveData
{
    public StringBoolMap stageCleared = new();
    public bool tutorialPromptDismissed;   // 튜토리얼 안내 팝업 "다시 나타내지 않음"
}

[Serializable]
public class SettingsData
{
    public float masterVolume = 0.5f;
    public float bgmVolume = 0.5f;
    public float gameSfxVolume = 0.5f;
    public float uiSfxVolume = 0.5f;
    public bool masterMute;
    public bool bgmMute;
    public bool gameSfxMute;
    public bool uiSfxMute;
    public string locale = "";

    public int cameraPanSpeedRatio = 50;

    // UIHotkeys 액션맵의 리바인딩 오버라이드 (InputActionAsset.SaveBindingOverridesAsJson 결과)
    public string uiHotkeyOverridesJson = "";

    public int graphicQualityLevel = 2;
    public int targetFrameRateIndex = 1;
    public int fullScreenModeIndex = 0;
    public int resolutionWidth = 0;
    public int resolutionHeight = 0;
    public bool vSync = false;
}

public static class SaveManager
{
    private static readonly string SavePath = Path.Combine(Application.persistentDataPath, "save.json");
    private static readonly string SettingsPath = Path.Combine(Application.persistentDataPath, "settings.json");

    // Steam Cloud는 로컬 경로가 아니라 논리적 파일 이름(namespace)으로 파일을 구분한다.
    private const string CloudSaveFileName = "save.json";

    private static GameSaveData game;
    private static SettingsData settings;

    public static GameSaveData Game
    {
        get { if (game == null) LoadAll(); return game; }
        private set { game = value; }
    }

    public static SettingsData Settings
    {
        get { if (settings == null) LoadAll(); return settings; }
        private set { settings = value; }
    }

    public static void LoadAll()
    {
        game = LoadGameData() ?? new GameSaveData();
        settings = LoadFrom<SettingsData>(SettingsPath) ?? new SettingsData();
    }

    public static void SaveGame()
    {
        var data = game ?? new GameSaveData();
        SaveTo(SavePath, data);
        WriteToCloud(data);
    }

    public static void SaveSettings()
    {
        SaveTo(SettingsPath, settings ?? new SettingsData());
    }

    public static void ResetGame()
    {
        game = new GameSaveData();
        SaveGame();
    }
    
    public static void ResetSettings()                                                                                                                                                                                           
    {                                                                                                                                                                                                                            
        settings = new SettingsData();                                                                                                                                                                                          
        SaveSettings();                                                                                                                                                                                                          
    } 

    // 로컬 세이브와 클라우드 세이브 중 더 최신인 쪽을 골라서 로드한다.
    // (Steam이 꺼져있거나 클라우드가 비활성화된 경우 로컬만 사용)
    private static GameSaveData LoadGameData()
    {
        // SteamRemoteStorage.FileExists: 클라우드에 해당 이름의 파일이 존재하는지 확인.
        // 로컬 File.Exists와 별개의 저장소(Steam 서버/로컬 캐시)를 조회한다.
        bool cloudAvailable = SteamManager.Initialized && SteamRemoteStorage.FileExists(CloudSaveFileName);
        bool localAvailable = File.Exists(SavePath);

        if (cloudAvailable && localAvailable)
        {
            // SteamRemoteStorage.GetFileTimestamp: 클라우드 파일의 마지막 수정 시각(Unix timestamp, 초).
            long cloudTime = SteamRemoteStorage.GetFileTimestamp(CloudSaveFileName);
            long localTime = new DateTimeOffset(File.GetLastWriteTimeUtc(SavePath)).ToUnixTimeSeconds();
            return cloudTime >= localTime ? LoadFromCloud() : LoadFrom<GameSaveData>(SavePath);
        }
        if (cloudAvailable) return LoadFromCloud();
        if (localAvailable) return LoadFrom<GameSaveData>(SavePath);
        return null;
    }

    private static GameSaveData LoadFromCloud()
    {
        try
        {
            // SteamRemoteStorage.GetFileSize: 클라우드 파일의 바이트 크기.
            // 읽기 버퍼를 미리 할당하기 위해 FileRead 전에 필요하다.
            int size = SteamRemoteStorage.GetFileSize(CloudSaveFileName);
            if (size <= 0) return null;

            byte[] buffer = new byte[size];
            // SteamRemoteStorage.FileRead: 클라우드 파일 내용을 buffer로 읽어온다 (동기 호출).
            SteamRemoteStorage.FileRead(CloudSaveFileName, buffer, size);
            return JsonUtility.FromJson<GameSaveData>(Encoding.UTF8.GetString(buffer));
        }
        catch { return null; }
    }

    private static void WriteToCloud(GameSaveData data)
    {
        if (!SteamManager.Initialized) return;

        byte[] bytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(data, true));
        // SteamRemoteStorage.FileWrite: 로컬 캐시에 즉시 쓰고, 다음 동기화 시점(주로 게임 종료/Overlay)에
        // Steam 서버로 업로드된다. Steamworks 파트너 사이트에서 앱의 Cloud 설정이 꺼져 있으면 조용히 무시된다.
        bool ok = SteamRemoteStorage.FileWrite(CloudSaveFileName, bytes, bytes.Length);
        Debug.Log($"[SaveManager] Cloud write {(ok ? "성공" : "실패")} ({bytes.Length} bytes)");
    }

    private static T LoadFrom<T>(string path) where T : class
    {
        if (!File.Exists(path)) return null;
        try { return JsonUtility.FromJson<T>(File.ReadAllText(path)); }
        catch { return null; }
    }

    private static void SaveTo<T>(string path, T data)
    {
        File.WriteAllText(path, JsonUtility.ToJson(data, true));
    }
}