using UnityEngine;
using UnityEngine.InputSystem;

public class WeaponController : MonoBehaviour
{
    public WeaponData[] weapons;

    private int currentIndex = 0;
    private AttackController attackController;

    void Start()
    {
        attackController = GetComponent<AttackController>();

        // 시작 시 index:0 무기 장착
        if(weapons.Length > 0)
        {
            EquipWeapon(0);
        }
    }


    // index에 해당하는 무기 장착
    // 0 : 구속구 (기본무기)
    // 1 : 
    public void EquipWeapon(int index)
    {
        if (index < 0 || index >= weapons.Length)
            return;

        currentIndex = index;
        attackController.EquipWeapon(weapons[index]);
    }

    // 다음 무기로
    public void NextWeapon()
    {
        EquipWeapon((currentIndex + 1) % weapons.Length);
    }

    // 이전 무기로
    public void PrevWeapon()
    {
        EquipWeapon((currentIndex - 1 + weapons.Length) % weapons.Length);
    }

    // 무기 교체 키 할당
    private void OnWeaponSwitch(InputValue value)
    {
        float scroll = value.Get<float>();
        if (scroll > 0) NextWeapon();
        else if (scroll < 0) PrevWeapon();
    }
}
