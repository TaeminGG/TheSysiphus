using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class AttackHitBox : MonoBehaviour
{
    public LayerMask hitLayer;
    private AttackData currentData;
    private Collider2D col;
    private List<Collider2D> hitTargets = new List<Collider2D>();

    private float playerDamage;
    private float floorZ = 0;


    [Header("Debug")]
    public bool showDebugInGame = true;     // 게임뷰 표시 여부
    public Color activeColor = new Color(1f, 0f, 0f, 0.4f);
    public Color inactiveColor = new Color(1f, 1f, 0f, 0.2f);

    private SpriteRenderer debugRenderer;


    private void Awake()
    {
        col = GetComponent<Collider2D>();

        SetupDebugRenderer();
    }


    // 기본 공격속도 1.0f
    public void Setup(AttackData data, float attackSpeed, float playerAttackDamage, Vector2 direction, float z)
    {
        currentData = data;
        hitTargets.Clear();
        playerDamage = playerAttackDamage;
        floorZ = z;

        if(col is BoxCollider2D box)
        {
            box.size = data.hitboxSize;
            box.offset = data.hitboxOffset;
        }

        // 입력받은 마우스 방향에 맞춰 회전
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        angle -= 90f;
        transform.rotation=Quaternion.Euler(0f,0f,angle);

        EnableHitBox();

        // 공격 속도에 비례해서 히트박스 지속시간 조절
        float asDuration = (data.attackDuration * 0.5f) / attackSpeed;
        StartCoroutine(AutoDisable(asDuration));
    }

    private IEnumerator AutoDisable(float delay)
    {
        yield return new WaitForSeconds(delay);
        DisableHitBox();
    }

    public void EnableHitBox()
    {
        col.enabled = true;

        if (debugRenderer != null)
        {
            // 콜라이더 크기에 맞게 스프라이트 크기 조정
            if (col is BoxCollider2D box)
            {
                debugRenderer.transform.localPosition = box.offset;
                debugRenderer.transform.localScale = box.size;
            }
            debugRenderer.color = activeColor;  // 활성화 시 빨강
        }
    }

    public void DisableHitBox()
    {
        col.enabled = false;

        if (debugRenderer != null)
            debugRenderer.color = inactiveColor;  // 비활성화 시 노랑
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & hitLayer) == 0) return;
        if (hitTargets.Contains(other)) return;

        hitTargets.Add(other);

        if (other.TryGetComponent<IDamageable>(out var target))
        {
            // Z 공격 범위에 해당하는 적만 공격
            float zUpper = currentData.zUpperRange;
            float zLower = currentData.zLowerRange;

            other.TryGetComponent<IEnemyMovement>(out var targetEnemy);

            float enemyFloorZ = targetEnemy.FloorZ;

            if(floorZ + zUpper >= enemyFloorZ)
            {
                if(floorZ - zLower <= enemyFloorZ)
                {
                    // 계수 적용된 최종 데미지 계산
                    float finalDamage = currentData.GetFinalDamage(playerDamage);

                    Vector2 knockback = (other.transform.position - transform.position).normalized
                                        * currentData.knockbackForce;
                    target.TakeDamage(finalDamage, knockback);
                }
            }
        }
    }


    private void SetupDebugRenderer()
    {
        if (!showDebugInGame) return;

        // 디버그용 자식 오브젝트 생성
        GameObject debugObj = new GameObject("HitboxDebug");
        debugObj.transform.SetParent(transform);
        debugObj.transform.localPosition = Vector3.zero;

        debugRenderer = debugObj.AddComponent<SpriteRenderer>();
        debugRenderer.sprite = CreateWhiteSprite();
        debugRenderer.color = inactiveColor;
        debugRenderer.sortingOrder = 999;   // 최상위 렌더링
    }
    // 흰색 스프라이트 생성
    private Sprite CreateWhiteSprite()
    {
        Texture2D tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
    }
    private void OnDrawGizmos()
    {
        if (col == null) col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            // 비활성화 상태: 반투명 빨강
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);

            // 콜라이더 offset과 rotation 반영
            Vector3 center = transform.TransformPoint(box.offset);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, box.size);

            // 외곽선
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(Vector3.zero, box.size);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // 오브젝트 선택 시 더 진하게 표시
        if (col == null) col = GetComponent<Collider2D>();
        if (col is BoxCollider2D box)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f);
            Vector3 center = transform.TransformPoint(box.offset);
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, box.size);
        }
    }

}
