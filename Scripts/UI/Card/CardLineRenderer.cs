using UnityEngine;

public class CardLineRenderer : MonoBehaviour
{
    private LineRenderer lineRenderer;

    private void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        lineRenderer.positionCount = 2;
        lineRenderer.enabled = false;
    }

    public void Play(Vector3 to)
    {
        float distance = Vector3.Distance(Camera.main.transform.position, lineRenderer.transform.position);
        lineRenderer.widthMultiplier = distance * 0.01f; // 원하는 비율로 조정

        lineRenderer.enabled = true;

        lineRenderer.SetPosition(0, gameObject.transform.position);
        lineRenderer.SetPosition(1, to);
    }

    public void Stop()
    {
        lineRenderer.enabled = false;
    }
}
