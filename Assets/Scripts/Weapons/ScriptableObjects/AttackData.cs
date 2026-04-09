using UnityEngine;

[CreateAssetMenu(fileName = "AttackStats", menuName = "Scriptable Objects/AttackData")]
public class AttackData : ScriptableObject
{
    [Header("Combat")]
    public string animTrigger;
    public float damage;
    public float damageScaling;
    public Vector2 hitboxSize;
    public Vector2 hitboxOffset;
    public float attackDuration;
    public float comboInputWindow;
    public float knockbackForce;
    public float zUpperRange;
    public float zLowerRange;

    [Header("Feedback")]
    public CameraFeedbackPreset onHitCameraPreset;
    public ParticleSystem onHitSparkPrefab;
    public HitSparkSettings onHitSparkSettings;

    [Header("Visual")]
    public GameObject visual;
    public bool flipX;
    public bool flipY;

    public float GetFinalDamage(float playerDamage)
    {
        return damage + Mathf.RoundToInt(playerDamage * damageScaling);
    }
}
