using UnityEngine;

public class EnemyProjectileController : MonoBehaviour
{
    public float damage; // 이 총알의 공격력, EnemyController가 설정해 줍니다.

    void OnTriggerEnter(Collider other)
    {
        // "Player" 태그를 가진 오브젝트와 충돌했는지 확인
        if (other.CompareTag("Player"))
        {
            // 플레이어에게 데미지 전달
            if (PlayerController.Instance != null)
            {
                 PlayerController.Instance.TakeDamage(damage);
            }

            // 플레이어와 충돌 시 총알 즉시 파괴
            Destroy(gameObject);
        }
        // "Wall" 또는 "Ground" 태그와 충돌 시에도 파괴되도록 설정할 수 있습니다.
        // else if (other.CompareTag("Wall"))
        // {
        //     Destroy(gameObject);
        // }
    }
}
