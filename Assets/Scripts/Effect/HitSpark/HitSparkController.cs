using UnityEngine;

[System.Serializable]
public struct HitSparkSettings
{
    public int count;
    public Color color;
    public float speedMin;
    public float speedMax;
    public float gravity;
    public float spreadAngle;
    public float lifetime;
    public Vector2 direction;
}

public class HitSparkController : MonoBehaviour
{
    public void Play(ParticleSystem sparkPrefab, Vector3 worldPos, HitSparkSettings settings)
    {
        if (sparkPrefab == null) return;

        ParticleSystem ps = Instantiate(sparkPrefab, worldPos, Quaternion.identity);
        var main = ps.main;
        main.startColor = settings.color;
        main.startLifetime = settings.lifetime;
        main.gravityModifier = settings.gravity;

        Vector2 dir = settings.direction.sqrMagnitude > 0.001f
            ? settings.direction.normalized
            : Vector2.up;

        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        float halfSpread = settings.spreadAngle * 0.5f;

        ParticleSystem.EmitParams emitParams = new ParticleSystem.EmitParams();

        for (int i = 0; i < settings.count; i++)
        {
            float angle = baseAngle + Random.Range(-halfSpread, halfSpread);
            float speed = Random.Range(settings.speedMin, settings.speedMax);
            Vector2 velocityDir = new Vector2(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));

            emitParams.velocity = velocityDir * speed;
            emitParams.startColor = settings.color;
            emitParams.startLifetime = settings.lifetime;
            ps.Emit(emitParams, 1);
        }

        ps.Play();
        Destroy(ps.gameObject, settings.lifetime + 0.5f);
    }
}
