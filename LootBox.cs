using UnityEngine;

public class LootItem : MonoBehaviour
{
    public enum ItemType { Ammo, Medkit, Shield }
    public ItemType type;
    public int amount = 30;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            FreeFireMini player = other.GetComponent<FreeFireMini>();
            
            switch (type)
            {
                case ItemType.Ammo:
                    player.LootAmmo(amount);
                    Debug.Log("Đã nhặt " + amount + " viên đạn");
                    break;
                case ItemType.Medkit:
                    player.Heal(50f);
                    Debug.Log("Đã dùng túi cứu thương");
                    break;
            }
            Destroy(gameObject); // Nhặt xong thì biến mất
        }
    }
}
