using UnityEngine;

public interface IEnemyMovement
{
    void Move();

    void SetPlayerDetected(bool val);

    void ApplyCrowdControl(EnemyCrowdControlRequest request);

    int FloorZ { get; }
}
