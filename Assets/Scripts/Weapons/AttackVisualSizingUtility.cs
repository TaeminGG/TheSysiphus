using UnityEngine;

public static class AttackVisualSizingUtility
{
    public static void FitToCircularHitRange(GameObject visualObject, AttackData attackData, float rangeMultiplier = 1f, Vector3? scaleMultiplier = null)
    {
        if (visualObject == null || attackData == null)
            return;

        float diameter = GetCircularHitDiameter(attackData, rangeMultiplier);
        FitToWorldSize(visualObject, new Vector2(diameter, diameter), scaleMultiplier ?? Vector3.one);
    }

    public static float GetCircularHitDiameter(AttackData attackData, float rangeMultiplier = 1f)
    {
        if (attackData == null)
            return 0f;

        float multiplier = Mathf.Max(0.01f, rangeMultiplier);
        return Mathf.Max(0.1f, Mathf.Max(attackData.hitboxSize.x, attackData.hitboxSize.y) * multiplier);
    }

    private static void FitToWorldSize(GameObject visualObject, Vector2 targetSize, Vector3 scaleMultiplier)
    {
        Renderer[] renderers = visualObject.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 currentSize = bounds.size;
        if (currentSize.x <= 0.001f || currentSize.y <= 0.001f)
            return;

        Vector3 currentScale = visualObject.transform.localScale;
        visualObject.transform.localScale = new Vector3(
            currentScale.x * (targetSize.x / currentSize.x) * scaleMultiplier.x,
            currentScale.y * (targetSize.y / currentSize.y) * scaleMultiplier.y,
            currentScale.z * scaleMultiplier.z);
    }
}
