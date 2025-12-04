using UnityEngine;
using TMPro; // TextMeshPro 사용
using UnityEngine.UI; // UI 요소 사용

public class UpgradeCardUI : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI descriptionText;
    public Button selectButton;
    // Optional: Image for icon or background
    // public Image iconImage;

    private UpgradeData upgradeData; // 이 카드에 할당된 강화 데이터

    public void SetupCard(UpgradeData data)
    {
        upgradeData = data;

        if (nameText != null)
        {
            nameText.text = data.name;
        }
        else
        {
            Debug.LogWarning($"UpgradeCardUI: nameText가 할당되지 않았습니다. '{gameObject.name}' 오브젝트에서 확인해주세요.");
        }
        if (descriptionText != null)
        {
            descriptionText.text = data.description;
        }
        else
        {
            Debug.LogWarning($"UpgradeCardUI: descriptionText가 할당되지 않았습니다. '{gameObject.name}' 오브젝트에서 확인해주세요.");
        }
        
        // 버튼 클릭 이벤트 설정
        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            selectButton.onClick.AddListener(OnCardSelected);
        }
    }

    void OnCardSelected()
    {
        // 강화가 선택되었을 때 GameController (또는 UpgradeManager)에 알림
        // GameController에서 ApplyUpgrade를 호출하도록 할 것임
        if (GameController.Instance != null)
        {
            GameController.Instance.SelectUpgrade(upgradeData);
        }
        else
        {
            Debug.LogError("GameController.Instance not found when selecting upgrade card!");
        }
    }
}
