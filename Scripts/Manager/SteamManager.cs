using UnityEngine;
using Steamworks;

public class SteamManager : MonoBehaviour
{
    private static SteamManager instance;
    private static bool initialized = false;

    public static bool Initialized => initialized;
    public static SteamManager Instance => instance;
    public static string PlayerName => initialized ? SteamFriends.GetPersonaName() : "Player";

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        if (!Packsize.Test())
        {
            Debug.LogError("[SteamManager] Packsize 불일치 — 올바른 Steamworks.NET 빌드를 사용하세요.");
            return;
        }

        if (!DllCheck.Test())
        {
            Debug.LogError("[SteamManager] DLL 불일치 — steam_api.dll 버전을 확인하세요.");
            return;
        }

        try
        {
            if (!SteamAPI.Init())
            {
                Debug.LogError("[SteamManager] SteamAPI.Init() 실패 — Steam이 실행 중인지, steam_appid.txt가 있는지 확인하세요.");
                return;
            }
        }
        catch (System.DllNotFoundException e)
        {
            Debug.LogError($"[SteamManager] steam_api.dll을 찾을 수 없습니다: {e}");
            return;
        }

        initialized = true;
        Debug.Log($"[SteamManager] 초기화 성공 — {SteamFriends.GetPersonaName()} ({SteamUser.GetSteamID()})");
    }

    private void Update()
    {
        if (!initialized) return;
        SteamAPI.RunCallbacks();
    }

    private void OnDestroy()
    {
        if (!initialized) return;
        SteamAPI.Shutdown();
        initialized = false;
        instance = null;
    }
}