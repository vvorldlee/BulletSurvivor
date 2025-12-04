using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 씬 관리용

public class GameController : MonoBehaviour
{
    public static GameController Instance;

    public enum GameState {
        Ready,      // 게임 준비
        Playing,    // 게임 플레이 중
        Paused,     // 일시 정지
        LevelUp,    // 레벨업 강화 선택 중
        GameOver    // 게임 오버
    }

    public static GameState CurrentState = GameState.Ready;

    [Header("Audio")]
    [SerializeField] private AudioSource backgroundMusicSource; // 배경음악 AudioSource
    [SerializeField] private AudioClip backgroundMusicClip;     // 배경음악 AudioClip

    [Header("UI References")]
    [SerializeField] private GameObject levelUpPanel; // 레벨업 UI 패널 (활성화/비활성화)

    private float gameTimer = 0f; // 게임 시간 추적용

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

        // AudioSource 컴포넌트 초기화
        backgroundMusicSource = GetComponent<AudioSource>();
        if (backgroundMusicSource == null)
        {
            Debug.LogWarning("GameController: AudioSource 컴포넌트를 찾을 수 없습니다! 배경음악이 재생되지 않을 수 있습니다.");
        }
    }

    void Start()
    {
        // 게임 시작 시 초기 상태 설정
        CurrentState = GameState.Playing;
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false); // 시작 시 레벨업 패널은 비활성화
        }
        gameTimer = 0f; // 게임 시간 초기화

        // 배경음악 재생 및 루프 설정
        if (backgroundMusicSource != null && backgroundMusicClip != null)
        {
            backgroundMusicSource.clip = backgroundMusicClip;
            backgroundMusicSource.loop = true;
            backgroundMusicSource.Play();
        }
        else
        {
            Debug.LogWarning("GameController: 배경음악 AudioSource 또는 AudioClip이 할당되지 않았습니다. 배경음악이 재생되지 않습니다.");
        }
    }

    void Update()
    {
        // Esc 키로 일시정지 토글
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (CurrentState == GameState.Playing)
            {
                PauseGame();
            }
            else if (CurrentState == GameState.Paused)
            {
                ResumeGame();
            }
            // LevelUp 상태에서는 Esc 키로 바로 나가지 않음 (강제 선택 방지)
            // GameOver 상태에서는 Esc 키 작동 안 함
        }

        if (CurrentState == GameState.Playing)
        {
            gameTimer += Time.deltaTime;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.UpdatePlayTime(gameTimer);
            }
        }
    }

    public void PauseGame()
    {
        if (CurrentState == GameState.Playing) // 게임 플레이 중일 때만 일시 정지 가능
        {
            CurrentState = GameState.Paused;
            Time.timeScale = 0f; // 게임 일시 정지
            UIManager.Instance.SetPauseMenuVisible(true); // 일시정지 UI 표시
            // TODO: 플레이어 능력치 창 업데이트 로직 추가 (UIManager)
            UIManager.Instance.UpdatePlayerStatsDisplay(); // 현재 능력치 표시
        }
    }

    public void ResumeGame()
    {
        if (CurrentState == GameState.Paused) // 일시 정지 중일 때만 재개 가능
        {
            CurrentState = GameState.Playing;
            Time.timeScale = 1f; // 게임 재개
            UIManager.Instance.SetPauseMenuVisible(false); // 일시정지 UI 숨기기
        }
    }

    // 레벨업 시작 시 호출 (PlayerController에서 호출)
    public void StartLevelUp()
    {
        CurrentState = GameState.LevelUp;
        Time.timeScale = 0f; // 게임 일시 정지

        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(true); // 레벨업 패널 활성화
        }

        DisplayUpgradeCards(); // 강화 카드 표시
    }

    // 강화 카드 표시 로직
    void DisplayUpgradeCards()
    {
        // UpgradeManager에서 3개의 랜덤 강화 가져오기
        List<UpgradeData> upgrades = UpgradeManager.Instance.GetRandomUpgrades(3);
        UIManager.Instance.DisplayUpgradeCards(upgrades);
    }

    // 강화 카드 선택 시 호출 (UpgradeCardUI에서 호출)
    public void SelectUpgrade(UpgradeData selectedUpgrade)
    {
        // 플레이어에게 강화 적용
        PlayerController.Instance.ApplyUpgrade(selectedUpgrade);

        ResumeGameFromLevelUp();
    }

    // 강화 포기 및 체력 회복 선택 시 호출
    public void ForfeitUpgradeAndHeal()
    {
        PlayerController.Instance.Heal(PlayerController.Instance.stats.MaxHP * 0.45f); // 최대 체력의 45% 회복
        ResumeGameFromLevelUp();
    }

    // 레벨업 화면에서 게임 재개 시 공통 로직
    private void ResumeGameFromLevelUp()
    {
        // 레벨업 패널 비활성화 및 게임 재개
        if (levelUpPanel != null)
        {
            levelUpPanel.SetActive(false);
        }
        UIManager.Instance.HideUpgradeCards(); // UIManager에게 카드 UI 비활성화 요청

        Time.timeScale = 1f; // 게임 재개
        CurrentState = GameState.Playing; // 게임 상태 변경

        // 연속 레벨업 처리
        PlayerController.Instance.pendingLevelUp--;
        if (PlayerController.Instance.pendingLevelUp > 0)
        {
            StartLevelUp(); // 아직 대기중인 레벨업이 있으면 즉시 다시 시작
        }
        else
        {
            // 모든 레벨업 처리가 끝나면 상태를 Playing으로
            CurrentState = GameState.Playing;
        }
    }

    // 게임 오버 시 호출 (PlayerController에서 호출)
    public void GameOver()
    {
        CurrentState = GameState.GameOver;
        Time.timeScale = 0f; // 게임 일시 정지
        UIManager.Instance.GameOverVisible(true); // 게임 오버 UI 표시
        Debug.Log("Game Over!");
    }

    // 게임 재시작 (RestartGame)
    public void RestartGame()
    {
        Time.timeScale = 1f; // 게임 속도 정상화 (필수)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name); // 현재 씬 다시 로드
    }

    // 메인 게임 씬 로드 (타이틀 씬에서 START 버튼 클릭 시 호출)
    public void LoadMainGame()
    {
        Time.timeScale = 1f; // 게임 속도 정상화 (필수)
        // TODO: "MainGame" 씬 이름을 실제 메인 게임 씬 이름으로 변경해야 합니다.
        SceneManager.LoadScene("MainGame"); 
    }

    // 타이틀 화면으로 돌아가기 (ReturnToTitle)
    public void ReturnToTitle()
    {
        Time.timeScale = 1f; // 게임 속도 정상화 (필수)
        // TODO: "TitleScene" 이름을 실제 타이틀 씬 이름으로 변경해야 합니다.
        SceneManager.LoadScene("TitleScene"); 
    }

    // 게임 종료 (QuitGame)
    public void QuitGame()
    {
        Time.timeScale = 1f; // 게임 속도 정상화 (필수)
        Application.Quit();
        // Unity Editor에서는 Application.Quit()이 작동하지 않으므로 Debug.Log로 알림
#if UNITY_EDITOR
        Debug.Log("Editor: Quit Game");
#endif
    }
}