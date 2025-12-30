using UnityEngine;

public class GameLoader : MonoBehaviour
{
    public List<CharacterData> characterConfigs;
    public Transform spawnPoint;

    void Awake()
    {
        int chosenIndex = PlayerPrefs.GetInt("SelectedCharIndex", 0);
        CharacterData chosenChar = characterConfigs[chosenIndex];

        // Tạo nhân vật tại điểm nhảy dù/xuất phát
        GameObject playerObj = Instantiate(chosenChar.modelPrefab, spawnPoint.position, Quaternion.identity);
        
        // Gán các chỉ số kỹ năng vào Script FreeFireMini
        FreeFireMini controller = playerObj.AddComponent<FreeFireMini>();
        controller.maxHealth += chosenChar.bonusHealth;
        controller.health = controller.maxHealth;
        controller.moveSpeed *= chosenChar.speedMultiplier;

        // Thêm tag để Bot có thể nhận diện
        playerObj.tag = "Player";
    }
}
