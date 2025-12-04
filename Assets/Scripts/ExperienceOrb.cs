using UnityEngine;

public class ExperienceOrb : MonoBehaviour
{
    public int experienceValue = 10; // 이 구슬이 주는 경험치 양

    private Transform playerTransform;
    private PlayerController playerController;
    private bool isFollowing = false; // 플레이어를 따라가기 시작했는지 여부
    private float followSpeed = 8f; // 플레이어를 따라가는 속도

    void Start()
    {
        // 플레이어 정보를 찾아서 저장
        if (PlayerController.Instance != null)
        {
            playerTransform = PlayerController.Instance.transform;
            playerController = PlayerController.Instance;
        }
    }

    void Update()
    {
        if (playerTransform == null) return;

        // 플레이어와의 거리 계산
        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // 자석 범위 안에 들어오면 따라가기 시작
        if (!isFollowing && distance <= playerController.stats.MagnetRange)
        {
            isFollowing = true;
        }

        // isFollowing이 true이면 플레이어를 향해 이동
        if (isFollowing)
        {
            Vector3 direction = (playerTransform.position - transform.position).normalized;
            transform.position += direction * followSpeed * Time.deltaTime;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // 플레이어와 충돌했는지 확인
        if (other.CompareTag("Player"))
        {
            // 플레이어에게 경험치 전달
            if (playerController != null)
            {
                playerController.GainExp(experienceValue);
            }
            
            // 구슬 오브젝트 파괴
            Destroy(gameObject);
        }
    }
}
