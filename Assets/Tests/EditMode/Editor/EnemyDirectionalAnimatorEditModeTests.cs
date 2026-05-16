using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class EnemyDirectionalAnimatorEditModeTests
{
    [Test]
    public void LockDeathDirection_KeepsInitialPlayerDirection_WhenCalledAgain()
    {
        GameObject enemy = new GameObject("Enemy");
        GameObject player = new GameObject("Player");

        try
        {
            enemy.transform.position = Vector3.zero;
            player.transform.position = Vector3.right;

            EnemyDirectionalAnimator directionalAnimator = enemy.AddComponent<EnemyDirectionalAnimator>();
            SetPrivateField(directionalAnimator, "player", player.transform);

            directionalAnimator.LockDeathDirection();
            Vector2 initialDirection = GetPrivateField<Vector2>(directionalAnimator, "lockedDeathDirection");

            player.transform.position = Vector3.up;
            directionalAnimator.LockDeathDirection();
            Vector2 lockedDirection = GetPrivateField<Vector2>(directionalAnimator, "lockedDeathDirection");

            Assert.That(GetPrivateField<bool>(directionalAnimator, "isDeathDirectionLocked"), Is.True);
            Assert.That(initialDirection, Is.EqualTo(Vector2.right));
            Assert.That(lockedDirection, Is.EqualTo(initialDirection));
        }
        finally
        {
            Object.DestroyImmediate(player);
            Object.DestroyImmediate(enemy);
        }
    }

    private static void SetPrivateField<T>(object target, string fieldName, T value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        field.SetValue(target, value);
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
        Assert.That(field, Is.Not.Null);
        return (T)field.GetValue(target);
    }
}
