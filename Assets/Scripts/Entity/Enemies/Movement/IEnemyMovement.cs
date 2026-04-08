using UnityEngine;

public interface IEnemyMovement
{
    enum CC
    {
        Slow,
        Stun
    }
    void Move();

    void SetPlayerDetected(bool val);

    void CrowdControl(CC cc, float t);

    int FloorZ { get; }
}
