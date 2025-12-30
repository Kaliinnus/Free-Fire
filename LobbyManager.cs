using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class LobbyManager : MonoBehaviour
{
    [Header("Danh sách nhân vật")]
    public List<CharacterData> allCharacters;
    private int selectedIndex = 0;

    [Header("Hiển thị UI")]
    public Text charNameText;
    public Text charSkillText;
    public Transform characterDisplaySpot; // Điểm đặt model nhân vật trong sảnh
    private GameObject currentModel;

    [Header("Cài đặt trận đấu")]
    public string gameSceneName = "BattleRoyaleMap";
    public GameObject loadingScreen;
    public Slider loadingBar;

    void Start()
    {
        UpdateCharacterSelection();
    }

    // Nút mũi tên phải
    public void NextCharacter()
    {
        selectedIndex = (selectedIndex + 1) % allCharacters.Count;
        UpdateCharacterSelection();
    }

    // Nút mũi tên trái
    public void PreviousCharacter()
    {
        selectedIndex--;
        if (selectedIndex < 0) selectedIndex = allCharacters.Count - 1;
        UpdateCharacterSelection();
    }

    void UpdateCharacterSelection()
    {
        CharacterData data = allCharacters[selectedIndex];

        // Cập nhật chữ
        charNameText.text = data.characterName;
        charSkillText.text = "Kỹ năng: " + data.skillDescription;

        // Cập nhật Model 3D trong sảnh
        if (currentModel != null) Destroy(currentModel);
        currentModel = Instantiate(data.modelPrefab, characterDisplaySpot.position, characterDisplaySpot.rotation);
        
        // Lưu lựa chọn vào bộ nhớ tạm để mang vào trận
        PlayerPrefs.SetInt("SelectedCharIndex", selectedIndex);
    }

    // Nút BẮT ĐẦU (Start Game)
    public void StartMatch()
    {
        StartCoroutine(LoadLevelAsync());
    }

    IEnumerator<WaitForEndOfFrame> LoadLevelAsync()
    {
        loadingScreen.SetActive(true);
        AsyncOperation operation = SceneManager.LoadSceneAsync(gameSceneName);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);
            loadingBar.value = progress;
            yield return null;
        }
    }
}
