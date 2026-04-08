using UnityEngine;

[CreateAssetMenu(fileName = "GorgonEye", menuName = "Scriptable Objects/ActiveItem/GorgonEye")]
public class GorgonEye : ActiveItem
{
    private Transform player;

    public GameObject dummy;

    public override void OnUsed()
    {
        player = GameObject.Find("Player").transform;

        Instantiate(dummy, player.position, Quaternion.identity);

        Debug.Log("Gorgon Eye Used");
    }
}
