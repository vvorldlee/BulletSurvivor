using UnityEngine;

public class EnemyController : MonoBehaviour
{
    public enum EnemyType { Chaser, Shooter, Runner, Tank }

    [Header("Enemy Settings")]
    public EnemyType type;
    public float maxHealth;
    public bool ignoreHealthScaling = false; // 체력 스케일링을 무시하고 프리팹의 maxHealth를 고정으로 사용
    [SerializeField] private float baseMaxHealth; // 원본 최대 체력 (시간 계수 적용 전)
    public float BaseMaxHealth { get { return baseMaxHealth; } } // 다른 스크립트에서 읽기 전용으로 접근할 프로퍼티
    public float currentHealth;
    public float moveSpeed;
    public float damage;
    public int experienceReward;
    [SerializeField] private GameObject experienceOrbSmallPrefab; // 경험치 구슬 프리팹 (소형)
    [SerializeField] private GameObject experienceOrbMediumPrefab; // 경험치 구슬 프리팹 (중형)
    [SerializeField] private GameObject experienceOrbLargePrefab; // 경험치 구슬 프리팹 (대형)

    [Header("Shooter Settings")]
    public GameObject projectilePrefab; // 적 총알 프리팹
    public Transform projectileSpawnPoint; // 총알 발사 위치
    public float shootingRange = 10f; // 공격 사정거리
    public float fireRate = 2f; // 발사 주기(초)

    [Header("Animation")]
    [SerializeField] private Animator animator; // 적 애니메이터

    private Transform playerTransform;
    private float fireTimer; // 발사 타이머
    private Rigidbody rb; // 리지드바디 참조

    void Awake()
    {
        rb = GetComponent<Rigidbody>(); // 리지드바디 컴포넌트 가져오기

        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
        }
        else
        {
            Debug.LogError("PlayerController instance not found. Enemy will not target player.");
        }

        baseMaxHealth = maxHealth; // 원본 최대 체력 저장
        currentHealth = maxHealth;

        // Animator 컴포넌트 초기화
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning($"Enemy '{gameObject.name}' does not have an Animator component.");
        }

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }
        else
        {
            Debug.LogWarning($"Enemy '{gameObject.name}' does not have a Collider component.");
        }
    }

    void Update()
    {
        if (playerTransform == null || GameController.CurrentState != GameController.GameState.Playing)
        {
            return;
        }
        // Update는 주로 입력, AI 결정 등 물리와 독립적인 로직 처리
        // 현재는 FixedUpdate에서 모든 AI/이동을 처리하므로 비워둠
    }

    void FixedUpdate()
    {
        if (playerTransform == null || GameController.CurrentState != GameController.GameState.Playing)
        {
            return;
        }
        
        // 적 타입에 따라 다른 행동 처리
        switch (type)
        {
            case EnemyType.Shooter:
                HandleShooterAI();
                break;
            case EnemyType.Chaser:
            case EnemyType.Runner:
            case EnemyType.Tank:
                MoveTowardsPlayer();
                break;
        }
    }

    // Chaser, Runner, Tank의 기본 이동 로직
    void MoveTowardsPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.LookAt(playerTransform); // 플레이어를 바라보도록 회전
        Vector3 previousPosition = transform.position;
        rb.MovePosition(transform.position + direction * moveSpeed * Time.fixedDeltaTime);
        Vector3 currentPosition = transform.position;

        // Animator가 있다면 Speed 파라미터를 업데이트하여 걷기/달리기 애니메이션 제어
        if (animator != null)
        {
            float actualSpeed = (currentPosition - previousPosition).magnitude / Time.fixedDeltaTime;
            animator.SetFloat("Speed", actualSpeed > 0.01f ? 1f : 0f);
        }
    }

    // Shooter의 AI 로직
    void HandleShooterAI()
    {
        transform.LookAt(playerTransform); // 항상 플레이어를 바라보게 함

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        Vector3 previousPosition = transform.position; // 이전 위치 저장
        Vector3 intendedMovement = Vector3.zero;

        // 사정거리보다 가까우면 뒤로 물러남
        if (distance < shootingRange - 1.0f) 
        {
            intendedMovement = -transform.forward * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(transform.position + intendedMovement);
        }
        // 사정거리보다 멀면 다가감
        else if (distance > shootingRange + 1.0f)
        {
            intendedMovement = transform.forward * moveSpeed * Time.fixedDeltaTime;
            rb.MovePosition(transform.position + intendedMovement);
        }
        // 적정 거리에 있으면 측면으로 이동 (Strafing) - 여기서는 간단하게 정지 후 사격
        else
        {
            // 사격 로직
            fireTimer += Time.fixedDeltaTime;
            if (fireTimer >= fireRate)
            {
                fireTimer = 0f;
                Shoot();
            }
        }

        // Animator가 있다면 Speed 파라미터를 업데이트하여 걷기/달리기 애니메이션 제어
        if (animator != null)
        {
            // 실제 이동량을 계산하여 Speed를 설정
            // Shooter는 제자리에서 사격할 때도 있으므로 actualSpeed를 사용하는 것이 좋습니다.
            float actualSpeed = (transform.position - previousPosition).magnitude / Time.fixedDeltaTime;
            animator.SetFloat("Speed", actualSpeed > 0.01f ? 1f : 0f);
        }
    }

    void Shoot()
    {
        if (projectilePrefab == null || projectileSpawnPoint == null)
        {
            Debug.LogWarning($"Shooter '{gameObject.name}' is missing projectile prefab or spawn point.");
            return;
                }
        
                // 공격 애니메이션 트리거
                if (animator != null)
                {
                    animator.SetTrigger("Attack");
                }
                
                // 총알 생성
                GameObject projectile = Instantiate(projectilePrefab, projectileSpawnPoint.position, projectileSpawnPoint.rotation);        
        // EnemyProjectileController를 가져와 데미지를 설정
        EnemyProjectileController projectileController = projectile.GetComponent<EnemyProjectileController>();
        if (projectileController != null)
        {
            projectileController.damage = this.damage;
        }
        else
        {
            Debug.LogWarning("EnemyProjectileController not found on projectile prefab!");
        }
        
        // 총알 발사
        Rigidbody rb = projectile.GetComponent<Rigidbody>();
        if (rb != null)
        {
            Vector3 direction = (playerTransform.position - projectileSpawnPoint.position).normalized;
            rb.velocity = direction * 10f; // 총알 속도는 임의로 10으로 설정
        }

        Destroy(projectile, 5f); // 5초 후 총알 자동 파괴
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        GameObject selectedOrbPrefab = null;
        int orbExperienceValue = 0;

        // Tank 타입 적은 고정 경험치 구슬을 드롭
        if (type == EnemyType.Tank)
        {
            selectedOrbPrefab = experienceOrbLargePrefab; // Tank는 항상 대형 구슬을 드롭
            orbExperienceValue = this.experienceReward; // Tank의 experienceReward (500) 사용
        }
        else
        {
            // 일반 적은 확률적 경험치 구슬 생성
            int randomValue = Random.Range(0, 100); // 0부터 99까지의 난수

            if (randomValue < 60) // 60% 확률로 소형
            {
                selectedOrbPrefab = experienceOrbSmallPrefab;
                orbExperienceValue = 5;
            }
            else if (randomValue < 90) // 30% 확률로 중형 (60이상 90미만)
            {
                selectedOrbPrefab = experienceOrbMediumPrefab;
                orbExperienceValue = 20;
            }
            else // 10% 확률로 대형 (90이상 99이하)
            {
                selectedOrbPrefab = experienceOrbLargePrefab;
                orbExperienceValue = 100;
            }
        }
        
        if (selectedOrbPrefab != null)
        {
            GameObject orb = Instantiate(selectedOrbPrefab, transform.position, Quaternion.identity);
            ExperienceOrb orbController = orb.GetComponent<ExperienceOrb>();
            if (orbController != null)
            {
                orbController.experienceValue = orbExperienceValue;
            }
        }
        else
        {
            Debug.LogWarning($"Enemy '{gameObject.name}' is missing an Experience Orb Prefab assignment in the Inspector.");
        }

        PlayerController.Instance.IncreaseKillCount(1);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController.Instance.TakeDamage(damage);
        }
    }
}
