using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    // UIManager의 싱글톤 인스턴스
    public static UIManager Instance;

    [Header("UI References")]
    [SerializeField]    // 일시정지 UI
    private GameObject pauseOverlayUI;

    [SerializeField]    // 스테이터스 UI
    private TextMeshProUGUI statsDisplay;

    [SerializeField]    // 레벨업 선택 UI
    private GameObject ChooseUpgradeUI;

    [SerializeField]    // 게임 오버 UI
    private GameObject GameOverUI;
    [SerializeField]
    private Button restartButton;

    [Header("Pause Menu Buttons")]
    [SerializeField] private Button titleButton; // 타이틀로 돌아가기 버튼
    [SerializeField] private Button quitButton; // 게임 종료 버튼

    [Header("Upgrade Card References")]
    [SerializeField]
    private UpgradeCardUI[] upgradeCards = new UpgradeCardUI[3];
    [SerializeField]
    private Button healButton;

    [Header("Ammo UI")]
    [SerializeField]
    private GameObject reloadingOverlayUI;
    [SerializeField]    
    private Image ammoFillImage;    
    [SerializeField]    // 장탄수 텍스트
    private TextMeshProUGUI AmmoText;
    // 재장전UI용 변수
    private float currentReloadTime = 0;
    private float maxReloadTime = 1;
    private bool isUIReloading = false;

    [Header("일반")]
    [SerializeField]    // 플레이어 체력 UI
    private Image HPBarUI;
    [SerializeField]    // 플레이어 체력 텍스트
    private TextMeshProUGUI PlayerHPText;
    [SerializeField]    // 플레이어 레벨 텍스트
    private TextMeshProUGUI PlayerLvText;
    [SerializeField]    // EXP 바 UI
    private Image EXPBarUI;
    [SerializeField]    // 플레이 타임 텍스트
    private TextMeshProUGUI playtimeText;
    [SerializeField]    // 킬 카운트 텍스트
    private TextMeshProUGUI KillCountText;

    private void Awake()
    {
        // 2-1. 이미 인스턴스가 존재하는지 확인 (중복 생성 방지)
        if (Instance != null && Instance != this)
        {
            // 중복된 객체라면, 스스로를 파괴하고 종료
            Destroy(gameObject);
            return;
        }

        // 2-2. 이 객체를 유일한 인스턴스로 지정
        Instance = this;

        // 불필요한 모든 오버레이 비활성화
        InitializeUIState();
    }
    private void InitializeUIState() {
        if (pauseOverlayUI != null) pauseOverlayUI.SetActive(false);
        if (ChooseUpgradeUI != null) ChooseUpgradeUI.SetActive(false);
        if (GameOverUI != null) GameOverUI.SetActive(false);
        if (reloadingOverlayUI != null) reloadingOverlayUI.SetActive(true);
    }
    void Start()
    {
        restartButton.onClick.AddListener(() => GameController.Instance.RestartGame());
        
        // Pause Menu Buttons listeners
        if (titleButton != null)
        {
            titleButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            titleButton.onClick.AddListener(() => GameController.Instance.ReturnToTitle());
        }
        if (quitButton != null)
        {
            quitButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            quitButton.onClick.AddListener(() => GameController.Instance.QuitGame());
        }

        // Heal Button listener for LevelUp panel
        if (healButton != null)
        {
            healButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
            healButton.onClick.AddListener(() => GameController.Instance.ForfeitUpgradeAndHeal());
        }
    }


    // Update is called once per frame
    void Update()
    {
        if (PlayerController.Instance != null)
        {
            EXPBarUI.fillAmount = PlayerController.Instance.currentExp / PlayerController.Instance.nextLevelExp;
            PlayerLvText.text = $"Lv. {PlayerController.Instance.currentLevel}";
        }
        else
        {
            // PlayerController.Instance가 null일 경우 (예: 타이틀 화면 등) 처리
            EXPBarUI.fillAmount = 0;
            PlayerLvText.text = "Lv. ?";
        }

        // isUIReloading일때 재장전 시각화
        if (isUIReloading)
        {
            VisualizeReloading();
        }
    }
    public void SetPauseMenuVisible(bool visible)
    {
        pauseOverlayUI.SetActive(visible);
    }
    public void DisplayUpgradeCards(List<UpgradeData> option)
    {
        ChooseUpgradeUI.SetActive(true);

        // 3개의 카드 UI를 돌며 데이터 설정
        for (int i = 0; i < upgradeCards.Length; i++)
        {
            if(i < option.Count)
            {
                // 데이터가 존재한다면 카드를 활성화하고 데이터를 설정
                upgradeCards[i].gameObject.SetActive(true);
                upgradeCards[i].SetupCard(option[i]);
            }
            else
            {
                // 데이터가 3개 미만이라면 해당 카드를 비활성화합니다.
                upgradeCards[i].gameObject.SetActive(false);
            }
        }
    }
    public void HideUpgradeCards()
    {
        ChooseUpgradeUI.SetActive(false);
    }
    // private void OnHealClicked() // 이 메서드는 더 이상 사용되지 않으므로 삭제
    // {
    //     // UpgradeManager에게 회복하라고 전달
    //     // GameController.Instance.ForfeitUpgradeAndHeal(); // 이 버튼은 레벨업 UI의 일부가 되어야 하므로 일단 주석처리
    //     Debug.Log($"체력 45% 회복 선택");
    // }
    public void GameOverVisible(bool visible)
    {
        GameOverUI.SetActive(visible);
    }
    public void UpdatePlayTime(float timeInSeconds)
    {
        // 총 시간을 정수로 변환함
        int totaltime = Mathf.FloorToInt(timeInSeconds);

        // 총 시간을 60으로 나누면 몫은 분(m)이고 나머지는 초(s)임
        int minutes = totaltime / 60;
        int seconds = totaltime % 60;
        // TextMeshProUGUI에 "mm:ss"의 형식으로 출력 (d2 포맷을 사용해 00 2자리로 출력함)
        playtimeText.text = $"{minutes:d2}:{seconds:d2}";
    }
    public void UpdateHPUI(float currentHP, float maxHP)
    {
        // HP Text 업데이트
        PlayerHPText.text = $"{PlayerController.Instance.stats.CurrentHP}/{PlayerController.Instance.stats.MaxHP}";
        // HP바 업데이트
        HPBarUI.fillAmount = currentHP / maxHP;
    }
    public void UpdateKillCountUI(int amount)
    {
        KillCountText.text = $"{amount}";
    }
    public void UpdateAmmoUI(int currentAmmo)
    {
        // fillAmount 업데이트
        int maxAmmo = PlayerController.Instance.stats.MaxAmmo;
        AmmoText.text = $"{currentAmmo} / {maxAmmo}";

        ammoFillImage.fillAmount = (float)currentAmmo/maxAmmo;
    }
    public void SetReloadingAmmo(bool visible)
    {
        isUIReloading = visible;

        // 재장전 상태가 되면 MaxReloadTime을 받아옴
        if (visible)
        {
            maxReloadTime = PlayerController.Instance.stats.ReloadTime;

            if (ammoFillImage != null)
            {
                // 재장전 시작시 fillAmount를 0으로 초기화
                ammoFillImage.fillAmount = 0;
            }
        }
    }
    public void UpdateReloadTimer(float timer)
    {
        currentReloadTime = timer;
    }
    public void VisualizeReloading()
    {
        if (maxReloadTime <= 0) return;

        int maxAmmo = PlayerController.Instance.stats.MaxAmmo;
        if (maxAmmo <= 0) return;

        // 한 발당 재장전 시간 계산
        float stepTime = maxReloadTime / maxAmmo;

        // 현재까지 재장전된 총알 수 계산
        int reloadedBullets = Mathf.FloorToInt(currentReloadTime / stepTime);

        // 계단형으로 Fill Amount 업데이트
        ammoFillImage.fillAmount = (float)reloadedBullets / maxAmmo;
        
        AmmoText.text = $"재장전 중\n({reloadedBullets}/{maxAmmo})";        
    }
    public void UpdatePlayerStatsDisplay()
    {
        int psLevel;
        // PlayerStats에서 능력치를 가져옴
        PlayerStats stats = PlayerController.Instance.stats;
        WeaponUpgrades upgrades = PlayerController.Instance.weaponUpgrades;
        if (upgrades.PiercingShot == false)
        {
            psLevel = 0;
        }
        else psLevel = 1;

        string statText =
            $"{stats.MaxHP}\n" +
            $"{stats.MoveSpeed}m/s\n" +
            $"{stats.AttackDamage}DMG\n" +
            $"{stats.AttackDelay:f1}s\n" +
            $"{stats.CriticalChance * 100}%\n" +
            $"{stats.CriticalMultiplier * 100}%\n" +
            $"{stats.ReloadTime:f1}s\n" +
            $"{stats.MagnetRange}m\n" +
            $"{upgrades.BurstShot}레벨\n" +
            $"{upgrades.SpreadShot}레벨\n" +
            $"{psLevel}레벨";

        statsDisplay.text = statText ;
    }
}