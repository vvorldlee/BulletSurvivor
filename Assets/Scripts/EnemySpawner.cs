using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    public static EnemySpawner Instance;

    [Header("Enemy Prefabs")]

    [SerializeField]
    private GameObject chaserPrefab;
    [SerializeField]
    private GameObject shooterPrefab;
    [SerializeField]
    private GameObject runnerPrefab;
    [SerializeField]
    private GameObject tankPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private float spawnRadiusMin = 10f; // 플레이어로부터 최소 스폰 반경
    [SerializeField] private float spawnRadiusMax = 15f; // 플레이어로부터 최대 스폰 반경
    [Header("Enemy Spawn Timings (in seconds)")]
    [SerializeField] private float shooterSpawnTime = 120f; // Shooter 출현 시간
    [SerializeField] private float runnerSpawnTime = 240f;  // Runner 출현 시간
    [SerializeField] private float tankSpawnInterval = 60f; // Tank 스폰 주기 (1분)

    [Header("Spawn Toggles")]
    [SerializeField] private bool spawnChaser = true;
    [SerializeField] private bool spawnShooter = true;
    [SerializeField] private bool spawnRunner = true;
    [SerializeField] private bool spawnTank = true;

    [Header("Time Scaling Factors")]
    [SerializeField] private float spawnIntervalReductionPerMinute = 0.15f; // 분당 스폰 빈도 감소량 (기획서: 0.15s/분)
    [SerializeField] private int spawnCountIncreasePerMinute = 5;    // 분당 스폰 수량 증가량
    [SerializeField] private float hpMultiplierPerMinute = 0.15f; // 분당 체력 배율 증가량 (기획서: 0.15배/분)

    // --- 시간 계수에 따른 변수들 ---
    private float gameTimer = 0f;       // 총 게임 시간
    private float spawnTimer = 0f;      // 다음 스폰까지의 타이머
    private float tankSpawnTimer = 0f;  // 다음 탱크 스폰까지의 타이머
    private int tanksSpawnedCount = 0; // 스폰된 탱크 수
    // 기획서 기반 초기값
    [SerializeField] private float initialSpawnInterval = 2.0f; // 초기 스폰 간격
    [SerializeField] private int initialSpawnCount = 3;      // 초기 스폰 수량
    
    [Header("Current Spawn Status")]
    [SerializeField] private float currentSpawnInterval;
    [SerializeField] private int currentSpawnCount;

    private Transform playerTransform;

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
    }

    void Start()
    {
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }
        else
        {
            Debug.LogError("PlayerController.Instance not found. EnemySpawner will not function correctly.");
        }
        tankSpawnTimer = tankSpawnInterval; 

        // 동적 계산을 위한 초기 스폰 값 설정
        currentSpawnInterval = initialSpawnInterval;
        currentSpawnCount = initialSpawnCount;
    }

    void Update()
    {
        if (playerTransform == null || GameController.CurrentState != GameController.GameState.Playing)
        {
            return;
        }

        // 게임 전체 시간 업데이트
        gameTimer += Time.deltaTime;
        spawnTimer += Time.deltaTime;
        tankSpawnTimer -= Time.deltaTime;

        // 시간 계수 계산
        CalculateTimeFactors();

        // 일반 몹 스폰 로직
        if (spawnTimer >= currentSpawnInterval)
        {
            spawnTimer = 0f;
            SpawnEnemies();
        }

        // 탱크 스폰 로직
        if (spawnTank && tankSpawnTimer <= 0)
        {
            tankSpawnTimer = tankSpawnInterval; // 설정된 주기 값으로 타이머 초기화
            SpawnTank();
        }
    }

    void CalculateTimeFactors()
    {
        float minutes = gameTimer / 60f;

        // 스폰 빈도 계산: -[spawnIntervalReductionPerMinute]s/분, 최소 0.55s
        currentSpawnInterval = Mathf.Max(0.55f, initialSpawnInterval - (spawnIntervalReductionPerMinute * minutes));

        // 스폰 수량 계산: +[spawnCountIncreasePerMinute]마리/분
        currentSpawnCount = initialSpawnCount + Mathf.FloorToInt(spawnCountIncreasePerMinute * minutes);
    }

    void SpawnEnemies()
    {
        List<GameObject> availableEnemies = GetAvailableEnemies();
        if (availableEnemies.Count == 0) return;

        for (int i = 0; i < currentSpawnCount; i++)
        {
            // 스폰할 적 랜덤 선택
            GameObject enemyToSpawn = availableEnemies[Random.Range(0, availableEnemies.Count)];

            // 스폰 위치 계산
            Vector3 spawnPos = GetRandomSpawnPosition(playerTransform, spawnRadiusMin, spawnRadiusMax, -0.2f);

            // 적 생성 및 체력 설정
            GameObject newEnemyObj = Instantiate(enemyToSpawn, spawnPos, Quaternion.identity);
            EnemyController enemyController = newEnemyObj.GetComponent<EnemyController>();
            if (enemyController != null)
            {
                ApplyEnemyHealthScaling(enemyController, gameTimer, tanksSpawnedCount, enemyController.type);
            }
        }
    }

    void SpawnTank()
    {
        if (tankPrefab == null) return;

        tanksSpawnedCount++; // 스폰된 탱크 수 증가

        // 스폰 위치 계산
        Vector3 spawnPos = GetRandomSpawnPosition(playerTransform, spawnRadiusMin, spawnRadiusMax, 0f);
        
        GameObject newEnemyObj = Instantiate(tankPrefab, spawnPos, Quaternion.identity);
        EnemyController enemyController = newEnemyObj.GetComponent<EnemyController>();
        if (enemyController != null)
        {
            ApplyEnemyHealthScaling(enemyController, gameTimer, tanksSpawnedCount, enemyController.type);
        }
    }

    List<GameObject> GetAvailableEnemies()
    {
        List<GameObject> enemies = new List<GameObject>();
        if (spawnChaser && chaserPrefab != null) enemies.Add(chaserPrefab);
        
        if (spawnShooter && gameTimer >= shooterSpawnTime && shooterPrefab != null)
        {
            enemies.Add(shooterPrefab);
        }
        
        if (spawnRunner && gameTimer >= runnerSpawnTime && runnerPrefab != null)
        {
            enemies.Add(runnerPrefab);
        }
        return enemies;
    }

    // 헬퍼 메서드: 랜덤 스폰 위치 계산
    private Vector3 GetRandomSpawnPosition(Transform player, float minRadius, float maxRadius, float yOffset)
    {
        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(minRadius, maxRadius);
        return new Vector3(player.position.x + randomCircle.x, yOffset, player.position.z + randomCircle.y);
    }

    // 헬퍼 메서드: 적 체력 스케일링 적용
    private void ApplyEnemyHealthScaling(EnemyController enemyController, float gameTime, int currentTanksSpawnedCount, EnemyController.EnemyType type)
    {
        if (enemyController == null) return;

        // ignoreHealthScaling이 true인 경우, 체력 스케일링을 적용하지 않고 기본값 유지
        if (enemyController.ignoreHealthScaling)
        {
            // 여기서 enemyController.maxHealth와 enemyController.currentHealth는 이미 Awake에서 프리팹의 maxHealth로 초기화되어 있습니다.
            return; 
        }

        float minutes = gameTime / 60f;
        float finalMaxHealth = enemyController.BaseMaxHealth; // 기본 체력에서 시작

        if (type == EnemyController.EnemyType.Tank)
        {
            // 탱크는 스폰 횟수에 따라 체력 증가 (1회당 200씩 증가)
            finalMaxHealth = enemyController.BaseMaxHealth + ((currentTanksSpawnedCount - 1) * 200f);
        }
        else // 일반 적 (Chaser, Shooter, Runner)
        {
            // 일반 적은 시간 경과에 따라 체력 배율로 증가 (1 + 분당 증가율 * 분)
            finalMaxHealth = enemyController.BaseMaxHealth * (1 + hpMultiplierPerMinute * minutes);
        }
        
        enemyController.maxHealth = finalMaxHealth;
        enemyController.currentHealth = finalMaxHealth;
    }
}