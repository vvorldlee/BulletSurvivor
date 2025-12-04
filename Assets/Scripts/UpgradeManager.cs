using System.Collections.Generic;
using UnityEngine;

// UpgradeType enum (PlayerController.cs에서 이동)
public enum UpgradeType
{
    MaxHP,
    MoveSpeed,
    MagnetRange,
    AttackDamage,
    AttackDelay,
    CriticalChance,
    CriticalMultiplier,
    ReloadTime,
    BurstShot,
    SpreadShot,
    PiercingShot
}

// UpgradeData struct (PlayerController.cs에서 이동)
// UpgradeManager에서 선택된 데이터를 보낼 구조체
public struct UpgradeData
{
    public UpgradeType type;    // 어떤 능력치가 강화될건지
    public float amount;        // 얼마나 강화할건지
    public string name;         // UI에 표시될 이름
    public string description;  // UI에 표시될 설명
}

// 각 업그레이드 설정을 위한 내부 클래스 (가중치 포함)
[System.Serializable]
public class UpgradeConfig
{
    public UpgradeType type;
    public float amount;
    public int weight;
    public string name;
    public string description;
    public int maxLevel; // 이 강화의 최대 레벨 (0이면 제한 없음)
}

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance;

    [Header("Upgrade Configurations")]
    [SerializeField] private List<UpgradeConfig> allUpgrades;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        // 초기화 시 업그레이드 목록 설정 (Inspector에서 직접 설정하는 것을 선호하는 사용자이므로 예시만)
        // allUpgrades = new List<UpgradeConfig>
        // {
        //     new UpgradeConfig { type = UpgradeType.MaxHP, amount = 20f, weight = 12, name = "최대 체력 증가", description = "최대 체력이 20 증가합니다.", maxLevel = 15 },
        //     new UpgradeConfig { type = UpgradeType.MoveSpeed, amount = 0.1f, weight = 8, name = "이동 속도 증가", description = "이동 속도가 0.1m/s 증가합니다.", maxLevel = 10 },
        //     // ... 나머지 업그레이드들
        // };
    }

    // 지정된 개수만큼 고유한 랜덤 업그레이드 선택
    public List<UpgradeData> GetRandomUpgrades(int count)
    {
        List<UpgradeData> selectedUpgrades = new List<UpgradeData>();
        
        // PlayerController 인스턴스가 없는 경우 처리
        if (PlayerController.Instance == null)
        {
            Debug.LogError("PlayerController.Instance not found. Cannot get player upgrade levels.");
            return selectedUpgrades;
        }

        // 1. 현재 플레이어가 선택 가능한 업그레이드만 필터링
        List<UpgradeConfig> filterableUpgrades = new List<UpgradeConfig>(allUpgrades);
        
        // TODO: 플레이어의 기본 MaxHP를 상수로 관리하는 것이 좋습니다.
        // TODO: 초기 AttackDamage를 상수로 관리하는 것이 좋습니다.
        // TODO: 초기 AttackDelay를 상수로 관리하는 것이 좋습니다.
        // TODO: 초기 ReloadTime을 상수로 관리하는 것이 좋습니다.

        for (int i = filterableUpgrades.Count - 1; i >= 0; i--)
        {
            var upgrade = filterableUpgrades[i];
            // 최대 레벨이 0이 아니면서 이미 최대 레벨에 도달한 업그레이드는 제외
            if (upgrade.maxLevel > 0 && PlayerController.Instance.GetUpgradeCurrentLevel(upgrade.type) >= upgrade.maxLevel)
            {
                filterableUpgrades.RemoveAt(i); // 이 강화는 선택 불가
            }
        }
        
        List<UpgradeConfig> availableUpgradesForSelection = new List<UpgradeConfig>(filterableUpgrades); // 선택을 위한 목록 복사 (중복 방지)

        for (int i = 0; i < count; i++)
        {
            if (availableUpgradesForSelection.Count == 0) break; // 더 이상 선택 가능한 업그레이드가 없으면 중단

            int totalWeight = 0;
            foreach (var upgrade in availableUpgradesForSelection)
            {
                totalWeight += upgrade.weight;
            }

            if (totalWeight == 0) break; // 가중치 합이 0이면 선택할 강화가 없음

            int randomWeight = Random.Range(0, totalWeight); // 0부터 totalWeight-1까지
            UpgradeConfig chosenConfig = null;

            foreach (var upgrade in availableUpgradesForSelection)
            {
                randomWeight -= upgrade.weight;
                if (randomWeight < 0) // 현재 강화가 선택됨
                {
                    chosenConfig = upgrade;
                    break;
                }
            }

            if (chosenConfig != null)
            {
                selectedUpgrades.Add(new UpgradeData
                {
                    type = chosenConfig.type,
                    amount = chosenConfig.amount,
                    name = chosenConfig.name,
                    description = chosenConfig.description
                });
                availableUpgradesForSelection.Remove(chosenConfig); // 중복 선택 방지를 위해 목록에서 제거
            }
        }
        return selectedUpgrades;
    }
}