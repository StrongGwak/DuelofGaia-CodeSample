using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// 코드 이벤트로 시작·종료를 고정한 구간의 프레임 시간을 기록해 CSV에 누적한다.
/// 사람이 버튼을 누르는 타이밍과 무관하게 항상 동일 구간을 측정하기 위한 A/B 계측용 임시 도구.
/// 릴리즈 빌드에서는 Conditional로 호출부째 제거되고 인스턴스도 생성되지 않는다.
/// </summary>
public class PerfProbe : MonoBehaviour
{
    private const string StageSelectScene = "Stage_Select";
    private const int StageEnterFrames = 30;   // SceneFader의 탭 대기보다 먼저 끝나도록 짧게 잡는다
    private const string FileName = "perf_samples.csv";

    private static PerfProbe instance;
    private static string runLabel = "unlabeled";

    private readonly List<float> samples = new(512);
    private string segment;
    private int autoEndFrames = -1;

    private bool IsSampling => segment != null;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Install()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (instance != null) return;

        runLabel = ReadRunLabel();

        var go = new GameObject("[PerfProbe]") { hideFlags = HideFlags.HideAndDontSave };
        DontDestroyOnLoad(go);
        instance = go.AddComponent<PerfProbe>();

        // 스테이지 선택 진입은 종료 앵커가 될 코드 이벤트가 없으므로 짧은 고정 프레임으로 자른다
        SceneManager.sceneLoaded += (scene, _) =>
        {
            if (scene.name == StageSelectScene)
                BeginFrames("stage_enter", StageEnterFrames);
        };
#endif
    }

    /// <summary>명시적으로 End()를 호출할 때까지 샘플링한다.</summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void Begin(string segment) => instance?.BeginInternal(segment, -1);

    /// <summary>지정 프레임 수만큼 샘플링한 뒤 자동 종료한다.</summary>
    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void BeginFrames(string segment, int frames) => instance?.BeginInternal(segment, frames);

    [Conditional("UNITY_EDITOR"), Conditional("DEVELOPMENT_BUILD")]
    public static void End() => instance?.EndInternal();

    private void BeginInternal(string newSegment, int frames)
    {
        if (IsSampling)
        {
            Debug.LogWarning($"[PerfProbe] '{segment}' 측정 중에 '{newSegment}' 시작 요청 — 무시한다.");
            return;
        }

        samples.Clear();
        segment = newSegment;
        autoEndFrames = frames;
    }

    private void EndInternal()
    {
        if (!IsSampling) return;

        string finished = segment;
        segment = null;
        autoEndFrames = -1;

        if (samples.Count > 0)
            WriteResult(finished, samples);
    }

    private void Update()
    {
        if (!IsSampling) return;

        samples.Add(Time.unscaledDeltaTime * 1000f);

        if (autoEndFrames > 0 && samples.Count >= autoEndFrames)
            EndInternal();
    }

    private static void WriteResult(string segment, List<float> samples)
    {
        var sorted = samples.ToArray();
        Array.Sort(sorted);

        float max = sorted[^1];
        float p99 = sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Length * 0.99f) - 1, 0, sorted.Length - 1)];
        float median = sorted[sorted.Length / 2];

        float sum = 0f;
        int over16 = 0;
        int over33 = 0;
        foreach (float s in samples)
        {
            sum += s;
            if (s > 16.7f) over16++;
            if (s > 33.3f) over33++;
        }

        var c = CultureInfo.InvariantCulture;
        string path = Path.Combine(Application.persistentDataPath, FileName);

        var sb = new StringBuilder();
        if (!File.Exists(path))
            sb.AppendLine("run,segment,timestamp,frames,maxMs,p99Ms,medianMs,meanMs,over16,over33,vSync,targetFps");

        sb.AppendLine(string.Join(",",
            runLabel,
            segment,
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", c),
            samples.Count.ToString(c),
            max.ToString("F2", c),
            p99.ToString("F2", c),
            median.ToString("F2", c),
            (sum / samples.Count).ToString("F2", c),
            over16.ToString(c),
            over33.ToString(c),
            QualitySettings.vSyncCount.ToString(c),
            Application.targetFrameRate.ToString(c)));

        File.AppendAllText(path, sb.ToString());

        Debug.Log($"[PerfProbe] {runLabel}/{segment}  frames={samples.Count}  " +
                  $"max={max:F2}ms  p99={p99:F2}ms  median={median:F2}ms  over33={over33}");
    }

    private static string ReadRunLabel()
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "-perfLabel")
                return args[i + 1];
        }
        return "unlabeled";
    }
}