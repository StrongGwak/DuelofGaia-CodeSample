using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// ShaderVariantCollection의 셰이더 변형을 로딩 화면에서 프레임 단위로 나눠 컴파일한다.
/// 머티리얼이 처음 그려질 때 발생하는 Shader.CreateGPUProgram 스파이크를
/// 로딩 구간으로 옮기는 것이 목적이다.
/// </summary>
public static class ShaderWarmup
{
    /// <summary>
    /// 컬렉션의 변형을 배치 단위로 컴파일한다.
    /// </summary>
    /// <param name="collection">워밍업할 변형 컬렉션. Graphics Settings에서 수집·저장한 에셋</param>
    /// <param name="onProgress">0~1 진행률 콜백</param>
    /// <param name="variantsPerBatch">한 프레임에 컴파일할 변형 수</param>
    public static IEnumerator WarmupRoutine(
        ShaderVariantCollection collection,
        Action<float> onProgress = null,
        int variantsPerBatch = 8)
    {
        onProgress?.Invoke(0f);

        if (collection == null)
        {
            Debug.LogWarning("[ShaderWarmup] 워밍업할 ShaderVariantCollection이 지정되지 않아 건너뛴다. " +
                             "TitleScene의 Warmup Shaders를 확인할 것.");
            onProgress?.Invoke(1f);
            yield break;
        }

        if (collection.isWarmedUp || collection.variantCount <= 0)
        {
            onProgress?.Invoke(1f);
            yield break;
        }

        if (variantsPerBatch < 1) variantsPerBatch = 1;

        int total = collection.variantCount;
        int processed = 0;

        // WarmUpProgressively가 예상과 다르게 동작해도 무한 루프에 빠지지 않도록 상한을 둔다
        int maxIterations = total / variantsPerBatch + 2;

        for (int i = 0; i < maxIterations; i++)
        {
            bool hasMore = collection.WarmUpProgressively(variantsPerBatch);

            processed = Mathf.Min(processed + variantsPerBatch, total);
            onProgress?.Invoke(hasMore ? (float)processed / total : 1f);

            if (!hasMore) break;
            yield return null;
        }

        onProgress?.Invoke(1f);
    }
}