using UnityEngine;

public interface IEnemyAttack
{
    void Attack();

    bool CanAttack { get; }
}
