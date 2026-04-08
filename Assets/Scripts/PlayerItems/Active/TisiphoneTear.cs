using UnityEngine;

[CreateAssetMenu(fileName = "TisiphoneTear", menuName = "Scriptable Objects/ActiveItem/TisiphoneTear")]
public class TisiphoneTear : ActiveItem
{
    private Transform player;

    public float amount = 10;
    public float duration = 10;

    public override void OnUsed()
    {
        player = GameObject.Find("Player").transform;

        player.GetComponent<PlayerStats>().Buff(0, amount, duration);

        Debug.Log("Tisiphone tear Used");
    }
}
