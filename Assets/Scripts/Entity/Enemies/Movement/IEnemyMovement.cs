using UnityEngine;

public interface IEnemyMovement
{
    void Move();
    void Stop();

    void SetPlayerDetected(bool val);

    int FloorZ { get; }
}
