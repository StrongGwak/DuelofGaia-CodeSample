using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;

/// <summary>
/// 로컬라이제이션 String Table에 등장하는 모든 문자를 로딩 중에 TMP 동적 폰트 아틀라스로 미리 굽는다.
/// 게임플레이 중 새 글리프가 처음 등장할 때 발생하는 CanvasUpdate.PreRender 스파이크를
/// 로딩 화면으로 옮기는 것이 목적이다.
/// </summary>
public static class FontWarmup
{
    /// <summary>String Table에는 없지만 런타임에 조합되는 숫자·기호</summary>
    private const string ExtraCharacters = "0123456789%+-*/:.,()[]<>!?~…'\"#&×÷";

    /// <summary>
    /// 지정한 폰트 에셋들에 String Table의 문자를 배치 단위로 추가한다.
    /// </summary>
    /// <param name="fontAssets">워밍업할 동적 폰트 에셋. Static 에셋은 무시된다.</param>
    /// <param name="tableNames">문자를 수집할 String Table 이름</param>
    /// <param name="onProgress">0~1 진행률 콜백</param>
    /// <param name="includeFontFeatures">
    /// 커닝(pair adjustment) 정보까지 미리 가져올지 여부.
    /// 폰트 에셋의 Get Font Features가 켜져 있다면 true로 두어야 런타임 조회 비용이 사라진다.
    /// </param>
    /// <param name="charactersPerBatch">한 프레임에 처리할 문자 수</param>
    public static IEnumerator WarmupRoutine(
        IReadOnlyList<TMP_FontAsset> fontAssets,
        IReadOnlyList<string> tableNames,
        Action<float> onProgress = null,
        bool includeFontFeatures = true,
        int charactersPerBatch = 128)
    {
        onProgress?.Invoke(0f);

        if (fontAssets == null || fontAssets.Count == 0)
        {
            Debug.LogWarning("[FontWarmup] 워밍업할 폰트가 지정되지 않아 건너뛴다. " +
                             "TitleScene의 Warmup Fonts를 확인할 것.");
            onProgress?.Invoke(1f);
            yield break;
        }

        var characters = new HashSet<char>();
        foreach (char c in ExtraCharacters)
            characters.Add(c);

        if (tableNames != null)
        {
            foreach (string tableName in tableNames)
            {
                var op = LocalizationSettings.StringDatabase.GetTableAsync(tableName);
                while (!op.IsDone) yield return null;

                var table = op.Result;
                if (table == null) continue;

                string characterSet;
                try
                {
                    characterSet = table.GenerateCharacterSet();
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[FontWarmup] '{tableName}' 문자 수집 실패: {e.Message}");
                    continue;
                }

                foreach (char c in characterSet)
                {
                    // 서로게이트는 배치 경계에서 쪼개지면 깨지므로 제외한다 (한글·ASCII는 해당 없음)
                    if (char.IsSurrogate(c)) continue;
                    characters.Add(c);
                }
            }
        }

        var batches = BuildBatches(characters, charactersPerBatch);
        if (batches.Count == 0)
        {
            onProgress?.Invoke(1f);
            yield break;
        }

        int totalSteps = fontAssets.Count * batches.Count;
        int step = 0;

        foreach (var font in fontAssets)
        {
            if (font == null)
            {
                step += batches.Count;
                continue;
            }

            foreach (string batch in batches)
            {
                if (!font.TryAddCharacters(batch, out string missingCharacters, includeFontFeatures)
                    && !string.IsNullOrEmpty(missingCharacters))
                {
                    Debug.LogWarning(
                        $"[FontWarmup] '{font.name}'에 추가하지 못한 문자 {missingCharacters.Length}개: {missingCharacters}");
                }

                step++;
                onProgress?.Invoke((float)step / totalSteps);
                yield return null;
            }
        }

        onProgress?.Invoke(1f);
    }

    private static List<string> BuildBatches(HashSet<char> characters, int charactersPerBatch)
    {
        var batches = new List<string>();
        if (charactersPerBatch < 1) charactersPerBatch = 1;

        var buffer = new StringBuilder(charactersPerBatch);
        foreach (char c in characters)
        {
            buffer.Append(c);
            if (buffer.Length < charactersPerBatch) continue;

            batches.Add(buffer.ToString());
            buffer.Clear();
        }

        if (buffer.Length > 0)
            batches.Add(buffer.ToString());

        return batches;
    }
}