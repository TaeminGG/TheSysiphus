using UnityEngine;

public class UndeadAttack : MonoBehaviour, IEnemyAttack
{
    private Animator anim;
    public void Attack()
    {

    }

    public bool CanAttack => AttackRange();

    private bool AttackRange()
    {
        return false;
    }
}
