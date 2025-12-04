using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct PlayerStats               // 기본 능력치 구조체
{
    [Header("Health")]
    public float MaxHP;                 // 초기 최대 체력: 150HP
    public float CurrentHP;             // 현재 체력
    public float MoveSpeed;             // 초기 이동속도: 5.0 m/s
    public float MagnetRange;           // 초기 자석 범위: 1.0 m

    [Header("Weapon Stats")]
    public float AttackDamage;          // 공격력: 10 DMG (기본값)
    // 공격 속도: 1.0 /s (1초에 1발, 구현 간편화를 위해 딜레이로 관리)
    public float AttackDelay;           // 공격 딜레이: 0.5f (기본값 0.5/s)
    public float CriticalChance;        // 치명타 확률: 0.1f (10%)
    public float CriticalMultiplier;    // 치명타 배율: 1.5f 배율
    public float ReloadTime;            // 장전 시간: 1.5 s (최소치 0.5 s)
    public int MaxAmmo;                 // 총 장탄수: 8 발
    public int CurrentAmmo;             // 현재 장탄수
}
[System.Serializable]
public struct WeaponUpgrades            // 특수 능력치
{
    [Header("Weapon Modifiers")]
    public int BurstShot;            // 연사 레벨 (Max Lv. 3) -> 레벨당 추가 연사 횟수
    public int SpreadShot;           // 산탄 레벨 (Max Lv. 5) -> 레벨당 추가 발사체 수
    public bool PiercingShot;           // 관통 여부 (Max Lv. 1) -> true / false로 관리
}



public class PlayerController : MonoBehaviour
{
    // 1. 자신을 참조하는 정적 변수 (Instance) 선언
    public static PlayerController Instance;

    // 2-1. 플레이어 능력치 및 현재 상태
    //      기본 능력치
    [Header("Player Stats")]
    public PlayerStats stats;
    // 2-2. 플레이어 능력치 및 현재 상태
    //      특수 능력치
    [Header("Weapon Upgrades")]
    public WeaponUpgrades weaponUpgrades;

    [Header("Animation")]
    [SerializeField] private Animator animator; // 플레이어 애니메이터

    [Header("Audio")]
    [SerializeField] private AudioSource playerAudioSource; // 플레이어 AudioSource
    [SerializeField] private AudioClip shootSound; // 발사 사운드
    [SerializeField] private AudioClip reloadSound; // 재장전 사운드

    [Header("Projectile Settings")]
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private Transform bulletSpawnPoint;
    [SerializeField] private float bulletSpeed = 10f; // 기본 총알 속도
    [SerializeField] private float burstDelay = 0.2f; // 연사 간격

    // 3. 플레이어 레벨 및 요구 경험치
    [Header("Current State")]
    public int currentLevel = 1;
    public float currentExp = 0f;
    public float nextLevelExp;
    public int pendingLevelUp = 0;  // 대기중인 레벨업 강화이벤트 수

    // 4. 재장전 및 발사 딜레이 관련 변수
    private bool isReloading = false;
    private float reloadTimer = 0f;
    private float attackTimer = 0f;
    private bool isBurstFiring = false; // 연사 중인지 추적

    // 5. 기타
    public int killCount = 0;

    // 마우스 조준을 위한 카메라 참조
    private Camera mainCamera;

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

        // 메인 카메라 참조 초기화
        mainCamera = Camera.main;

        // PlayerStats 필드 초기화
        stats.MaxHP = 150f;
        stats.AttackDelay = 0.5f;
        stats.ReloadTime = 1.5f;

        stats.CurrentHP = stats.MaxHP;
        stats.CurrentAmmo = stats.MaxAmmo;

        // 레벨 및 경험치 초기화
        currentLevel = 1;
        currentExp = 0f;
        nextLevelExp = 100f;

        // Animator 컴포넌트 초기화
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogWarning("Animator 컴포넌트를 찾을 수 없습니다! 애니메이션이 작동하지 않을 수 있습니다.");
        }

        // AudioSource 컴포넌트 초기화
        playerAudioSource = GetComponent<AudioSource>();
        if (playerAudioSource == null)
        {
            Debug.LogWarning("AudioSource 컴포넌트를 찾을 수 없습니다! 플레이어 사운드가 작동하지 않을 수 있습니다.");
        }
    }

    private void Start()
    {
        // 조준선은 Unity 에디터에서 직접 생성 및 설정해야 합니다.
    }




    // Update is called once per frame
    void Update()
    {
        if (GameController.CurrentState == GameController.GameState.Playing)
        {
            HandleMovement();
            HandleRotation();
        }

        // 게임 플레이 중에만 타이머 동작
        if (GameController.CurrentState == GameController.GameState.Playing)
        {
            // 공격 타이머 감소
            if (attackTimer > 0)
            {
                attackTimer -= Time.deltaTime;
            }

            // 마우스 좌클릭 시 총알 발사
            if (Input.GetMouseButton(0))
            {
                // 재장전이 아닐때 발사 가능
                if (!isReloading && attackTimer <= 0)
                {
                    UseAmmo();
                }
            }
        }

        
        // 재장전 로직
        if (isReloading)
        {
            // 실제 게임중에만 타이머 증가
            if (GameController.CurrentState == GameController.GameState.Playing)
            {
                reloadTimer += Time.deltaTime;

                // UIManager로 재장전 타이머값 전달
                UIManager.Instance.UpdateReloadTimer(reloadTimer);

                // 재장전 시간이 되었는지 확인
                if (reloadTimer >= stats.ReloadTime)
                {
                    // 재장전 완료
                    isReloading = false;
                    stats.CurrentAmmo = stats.MaxAmmo;
                    // UIManager에 재장전 완료 전달
                    UIManager.Instance.SetReloadingAmmo(false);
                    // Ammo UI 업데이트
                    UIManager.Instance.UpdateAmmoUI(stats.CurrentAmmo);
                    Debug.Log("재장전 완료!");
                }
            }
        }
        // J 키를 누를 때마다 10의 피해를 받음
        // (디버깅용) 나중에 지워주세요
#if UNITY_EDITOR
        if (Input.GetKeyDown(KeyCode.J))
        {
            TakeDamage(10);
        }

        // K 키를 누를 때마다 100의 경험치를 획득
        // (디버깅용) 나중에 지워주세요
        if (Input.GetKeyDown(KeyCode.K))
        {
            GainExp(100f);
        }

        // K 키를 누를 때마다 킬 카운트 1을 획득
        // (디버깅용) 나중에 지워주세요
        if (Input.GetKeyDown(KeyCode.L))
        {
            IncreaseKillCount(1);
        }
#endif
    }

    void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        if (Input.GetKey(KeyCode.W))
        {
            movement += Vector3.forward;
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement += Vector3.left;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement += Vector3.back;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += Vector3.right;
        }
        movement.Normalize();

        transform.Translate(movement * Time.deltaTime * stats.MoveSpeed, Space.World);

        // Animator가 있다면 Speed 파라미터를 업데이트하여 걷기/달리기 애니메이션 제어
        if (animator != null)
        {
            animator.SetFloat("Speed", movement.magnitude > 0.01f ? 1f : 0f);
        }
    }

    void HandleRotation()
    {
        // 1. 마우스 위치로 레이 생성
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // 2. 레이가 부딪힐 평면 생성 (플레이어의 y 위치를 기준으로)
        Plane groundPlane = new Plane(Vector3.up, new Vector3(0, transform.position.y, 0));

        // 3. 레이와 평면의 교차점 계산
        if (groundPlane.Raycast(ray, out float rayDistance))
        {
            Vector3 point = ray.GetPoint(rayDistance);
            
            // 4. 플레이어에서 교차점을 바라보는 방향 벡터 계산
            Vector3 lookDirection = point - transform.position;
            lookDirection.y = 0; // y축 회전은 없도록
            
            // 5. 해당 방향으로 플레이어 회전
            if (lookDirection.sqrMagnitude > 0.01f) // 매우 가까운 거리는 무시
            {
                Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, 0.15f);
            }
        }
    }



    public void UseAmmo(int amount = 1)
    {
        // 재장전 or 탄약이 0이라면 발사 불가
        if (isReloading || stats.CurrentAmmo == 0)
        {
            return;
        }

        // 발사 딜레이 초기화 (attackDelay)
        attackTimer = stats.AttackDelay;

        // 연사가 활성화되어 있으면 코루틴 시작, 아니면 한 발만 발사
        if (weaponUpgrades.BurstShot > 0)
        {
            if (!isBurstFiring) // 이미 연사 중이 아니어야 함
            {
                StartCoroutine(ShootBurstCoroutine());
            }
        }
        else
        {
            // 단일 발사
            FireSingleShot(bulletSpawnPoint.rotation, amount);
        }
    }

    // 단일 총알 (또는 산탄) 발사 로직을 캡슐화하는 헬퍼 메서드
    private bool FireSingleShot(Quaternion baseRotation, int ammoCost)
    {
        if (bulletPrefab == null || bulletSpawnPoint == null)
        {
            Debug.LogWarning("Bullet Prefab 또는 Bullet Spawn Point가 할당되지 않았습니다!");
            return false;
        }

        // 산탄 (SpreadShot) 처리
        int numSpreadBullets = 1 + weaponUpgrades.SpreadShot;
        float totalSpreadAngle = 0f;
        float angleIncrement = 0f;

        if (weaponUpgrades.SpreadShot > 0)
        {
            totalSpreadAngle = weaponUpgrades.SpreadShot * 20f; // SpreadShot 레벨당 20도 총 발사각
            
            if (numSpreadBullets > 1) // 발사될 총알이 여러 개일 경우에만 각도 증가량 계산
            {
                angleIncrement = totalSpreadAngle / (numSpreadBullets - 1); 
            }
            // else (numSpreadBullets == 1), angleIncrement는 0으로 유지되어 중앙 발사
        }
        
        // 총알 발사 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("Shoot");
        }
        
        // 발사 사운드 재생
        if (playerAudioSource != null && shootSound != null)
        {
            playerAudioSource.PlayOneShot(shootSound);
        }
        
        // 총알 발사
        for (int i = 0; i < numSpreadBullets; i++)
        {
            Quaternion bulletRotation = baseRotation;

            if (weaponUpgrades.SpreadShot > 0)
            {
                float currentAngle = -totalSpreadAngle / 2f + i * angleIncrement;
                bulletRotation = Quaternion.Euler(baseRotation.eulerAngles.x, baseRotation.eulerAngles.y + currentAngle, baseRotation.eulerAngles.z);
            }

            GameObject bulletObj = Instantiate(bulletPrefab, bulletSpawnPoint.position, bulletRotation);
            
            BulletController bulletController = bulletObj.GetComponent<BulletController>();
            if (bulletController != null)
            {
                bulletController.damage = stats.AttackDamage;
                bulletController.speed = bulletSpeed;
                bulletController.isPiercing = weaponUpgrades.PiercingShot;
            }
            else
            {
                Debug.LogWarning("BulletController not found on bullet prefab!");
            }
            
            Destroy(bulletObj, 3f);
        }
        
        // 총알 사용, 0 밑으로 떨어지지 않게 방지
        stats.CurrentAmmo = Mathf.Max(0, stats.CurrentAmmo - ammoCost);

        // UI 업데이트 요청
        UIManager.Instance.UpdateAmmoUI(stats.CurrentAmmo);

        if (stats.CurrentAmmo == 0)
        {
            StartReloading();
        }
        return true;
    }

    // 연사 코루틴
    private IEnumerator ShootBurstCoroutine()
    {
        isBurstFiring = true; // 연사 중 상태로 설정

        int bulletsInBurst = 1 + weaponUpgrades.BurstShot;
        for (int i = 0; i < bulletsInBurst; i++)
        {
            // 발사할 총알이 없거나 재장전 중이면 중단
            if (stats.CurrentAmmo == 0 || isReloading)
            {
                break;
            }

            // 산탄 효과가 적용된 단일 샷 발사
            FireSingleShot(bulletSpawnPoint.rotation, 0); // 코루틴에서 ammoCost는 0으로 전달하고 UseAmmo에서 한 번에 처리

            // 마지막 총알이 아니면 딜레이
            if (i < bulletsInBurst - 1)
            {
                yield return new WaitForSeconds(burstDelay);
            }
        }

        // 연사 후 남은 탄약 확인 및 소모 (전체 연사 발사 후 한번만 ammoCost를 사용)
        stats.CurrentAmmo = Mathf.Max(0, stats.CurrentAmmo - 1); // 1발 소모 (UseAmmo에서 이미 1발 소모했으므로 0으로)
        UIManager.Instance.UpdateAmmoUI(stats.CurrentAmmo);
        if (stats.CurrentAmmo == 0)
        {
            StartReloading();
        }
        
        isBurstFiring = false; // 연사 종료
    }

    public void StartReloading()
    {
        isReloading = true;
        reloadTimer = 0;
        Debug.Log("재장전 시작...");
        // 재장전 UI 요청
        UIManager.Instance.SetReloadingAmmo(true);
        // 재장전 애니메이션 트리거
        if (animator != null)
        {
            animator.SetTrigger("Reload");
        }
        // 재장전 사운드 재생
        if (playerAudioSource != null && reloadSound != null)
        {
            playerAudioSource.PlayOneShot(reloadSound);
        }
    }
    // 피격 함수
    public void TakeDamage(float damage)
    {
        // 데미지 적용
        stats.CurrentHP -= damage;
        // 0 이하로 떨어지지 않게 처리
        stats.CurrentHP = Mathf.Max(0, stats.CurrentHP);

        // UI 갱신
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHPUI(stats.CurrentHP, stats.MaxHP);
        }
        if (stats.CurrentHP <= 0)
        {
            // 사망 애니메이션 트리거
            if (animator != null)
            {
                animator.SetTrigger("Die");
            }
            GameController.Instance.GameOver();
        }
        else
        {
            // 피격 애니메이션 트리거 (사망하지 않았을 경우)
            if (animator != null)
            {
                animator.SetTrigger("Hit");
            }
        }
    }
    // 회복 함수
    public void Heal(float amount)
    {
        // 회복량 적용
        stats.CurrentHP += amount;
        // 최대 HP를 초과하지 않게 처리
        stats.CurrentHP = Mathf.Min(stats.CurrentHP, stats.MaxHP);

        // UI 갱신
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateHPUI(stats.CurrentHP, stats.MaxHP);
        }
    }
    // 킬 카운팅 함수
    // 적대몹 파괴시 이 함수를 호출하면 될것입니다
    public void IncreaseKillCount(int amount)
    {
        killCount += amount;

        // UI 갱신 요청
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UpdateKillCountUI(killCount);
        }
    }
    public void GainExp(float amount)
    {
        Debug.Log($"GainExp: +{amount} XP. Current Total XP: {currentExp + amount}");
        currentExp += amount;

        // 경험치를 획득할때마다 레벨업 가능 여부 검사
        CheckLevelUp();
    }
    public void CheckLevelUp()
    {
        Debug.Log($"CheckLevelUp: Before check - Current XP: {currentExp}, Next Level XP: {nextLevelExp}");
        bool levelUpOccurred = false;   // 레벨업이 발생했는지 확인용

        while (currentExp >= nextLevelExp) // while을 사용해 연속 레벨업 구현
        {
            // 레벨 증가
            currentLevel++;
            levelUpOccurred = true;
            pendingLevelUp++;
            Debug.Log($"Level up! Current Level: {currentLevel}, 대기중인 강화: {pendingLevelUp}");

            // 다음 레벨업 요구 경험치량 계산
            // 경험치 공식 XP = L^2 + 20L + 100
            int L = currentLevel;
            float prevLevelEXP = nextLevelExp;
            nextLevelExp = (L * L) + (20 * L) + 100;

            // 경험치 차감
            currentExp -= prevLevelEXP;
            Debug.Log($"Level up XP deducted. New Current XP: {currentExp}, New Next Level XP: {nextLevelExp}");
        }

        // 레벨업이 발생했고 이미 LevelUp 상태가 아니라면 StartLevelUp 함수 호출
        if (levelUpOccurred && GameController.CurrentState != GameController.GameState.LevelUp)
        {
            GameController.Instance.StartLevelUp();
        }
        Debug.Log($"CheckLevelUp: After check - Current XP: {currentExp}, Next Level XP: {nextLevelExp}");
    }
    // 레벨업 능력치 반영 함수
    public void ApplyUpgrade(UpgradeData data)
    {
        switch (data.type)
        {
            case UpgradeType.MaxHP:
                // 최대 체력 +20, MAX Lv. 15
                stats.MaxHP += data.amount;
                // 현재 체력도 증가
                stats.CurrentHP += data.amount;
                // UI 업데이트 요청
                UIManager.Instance.UpdateHPUI(stats.CurrentHP, stats.MaxHP);
                break;
            case UpgradeType.MoveSpeed:
                // 이동속도 + 0.1 m/s, MAX Lv. 10
                stats.MoveSpeed += data.amount;
                break;
            case UpgradeType.MagnetRange:
                // 자석 범위 + 0.5m, MAX Lv. 5
                stats.MagnetRange += data.amount;
                break;
            case UpgradeType.AttackDamage:
                // 공격력 +10% (amount는 0.1), MAX Lv. 15
                stats.AttackDamage *= (1 + data.amount);
                break;
            case UpgradeType.AttackDelay:
                // 공격 딜레이 -10% (amount는 0.1), MAX Lv. 15
                stats.AttackDelay *= (1 - data.amount);
                // 최소치 0.2f (5.0/s)
                stats.AttackDelay = Mathf.Max(stats.AttackDelay, 0.2f);
                break;
            case UpgradeType.CriticalChance:
                // 치명타 확률 +6%
                stats.CriticalChance += data.amount;
                // 확률은 0%~100%, MAX Lv. 15
                stats.CriticalChance = Mathf.Clamp01(stats.CriticalChance);
                break;
            case UpgradeType.CriticalMultiplier:
                // 치명타 배율 +0.25 배율, MAX Lv. 10
                stats.CriticalMultiplier += data.amount;
                break;
            case UpgradeType.ReloadTime:
                // 장전 시간 -10% (amount는 0.1), MAX Lv. 15
                stats.ReloadTime *= (1 - data.amount);
                // 최소치 0.5s
                stats.ReloadTime = Mathf.Max(stats.ReloadTime, 0.5f);
                break;
            case UpgradeType.BurstShot:
                // MAX Lv. 3
                weaponUpgrades.BurstShot += (int)data.amount;
                break;
            case UpgradeType.SpreadShot:
                // MAX Lv. 5
                weaponUpgrades.SpreadShot += (int)data.amount;
                break;
            case UpgradeType.PiercingShot:
                // MAX Lv. 1
                weaponUpgrades.PiercingShot = true;
                break;
        }
    }



    // UpgradeManager가 각 강화의 현재 레벨을 조회할 수 있도록 하는 메서드
    public int GetUpgradeCurrentLevel(UpgradeType type)
    {
        switch (type)
        {
            case UpgradeType.MaxHP:
                // MaxHP는 기본값이 100이고, 업그레이드당 20 증가합니다.
                // 따라서 (현재 MaxHP - 기본 MaxHP) / 20 이 레벨이 됩니다.
                // TODO: 플레이어의 기본 MaxHP를 상수로 관리하는 것이 좋습니다. (현재 stats.MaxHP는 업그레이드된 값)
                // 현재는 초기화시 100이므로, 이를 활용합니다.
                return (int)((stats.MaxHP - 100) / 20); // 임시 계산. 기본 MaxHP 100 가정.

            case UpgradeType.MoveSpeed:
                // MoveSpeed는 기본값이 5.0f이고, 업그레이드당 0.1f 증가합니다.
                return (int)((stats.MoveSpeed - 5.0f) / 0.1f); // 기본 MoveSpeed 5.0f 가정.

            case UpgradeType.MagnetRange:
                // MagnetRange는 기본값이 1.0f이고, 업그레이드당 0.5f 증가합니다.
                return (int)((stats.MagnetRange - 1.0f) / 0.5f); // 기본 MagnetRange 1.0f 가정.

            case UpgradeType.AttackDamage:
                // AttackDamage는 기본값이 10이고, 업그레이드당 10% 증가합니다.
                // 10 * (1 + 0.1 * level) 이므로, level = (AttackDamage / 10 - 1) / 0.1
                // TODO: 초기 AttackDamage를 상수로 관리하는 것이 좋습니다.
                return (int)((stats.AttackDamage / 10.0f - 1.0f) / 0.1f); // 기본 AttackDamage 10 가정.

            case UpgradeType.AttackDelay:
                // AttackDelay는 기본값이 1.0f이고, 업그레이드당 10% 감소합니다.
                // 1.0 * (1 - 0.1 * level) 이므로, level = (1 - AttackDelay / 1.0) / 0.1
                // TODO: 초기 AttackDelay를 상수로 관리하는 것이 좋습니다.
                return (int)((1.0f - stats.AttackDelay / 1.0f) / 0.1f); // 기본 AttackDelay 1.0f 가정.

            case UpgradeType.CriticalChance:
                // CriticalChance는 기본값이 0.05f (5%)이고, 업그레이드당 0.06f (6%) 증가합니다.
                return (int)((stats.CriticalChance - 0.05f) / 0.06f); // 기본 CriticalChance 0.05f 가정.

            case UpgradeType.CriticalMultiplier:
                // CriticalMultiplier는 기본값이 1.5f이고, 업그레이드당 0.25f 증가합니다.
                return (int)((stats.CriticalMultiplier - 1.5f) / 0.25f); // 기본 CriticalMultiplier 1.5f 가정.

            case UpgradeType.ReloadTime:
                // ReloadTime은 기본값이 3.0f이고, 업그레이드당 10% 감소합니다.
                // 3.0 * (1 - 0.1 * level) 이므로, level = (1 - ReloadTime / 3.0) / 0.1
                // TODO: 초기 ReloadTime을 상수로 관리하는 것이 좋습니다.
                return (int)((1.0f - stats.ReloadTime / 3.0f) / 0.1f); // 기본 ReloadTime 3.0f 가정.

            case UpgradeType.BurstShot:
                return weaponUpgrades.BurstShot;
            case UpgradeType.SpreadShot:
                return weaponUpgrades.SpreadShot;
            case UpgradeType.PiercingShot:
                return weaponUpgrades.PiercingShot ? 1 : 0; // 관통은 true/false이므로 1 또는 0
            default:
                return 0; // 알 수 없는 강화 타입
        }
    }
}
