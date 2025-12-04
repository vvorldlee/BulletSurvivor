using UnityEngine;

public class BulletController : MonoBehaviour
{
    public float damage; // 총알의 공격력, PlayerController가 설정해 줌
    public float speed;  // 총알의 이동 속도
    public bool isPiercing; // 총알이 적을 관통하는지 여부

    void Update()
    {
        // 총알을 앞으로 계속 이동시킴
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        // "Enemy" 태그를 가진 오브젝트와 충돌했는지 확인
        if (other.CompareTag("Enemy"))
        {
            // 충돌한 오브젝트에서 EnemyController 컴포넌트를 가져옴
            EnemyController enemy = other.GetComponent<EnemyController>();
            if (enemy != null)
            {
                // 적에게 데미지를 줌
                enemy.TakeDamage(damage);
            }
            
            // 관통 속성이 없으면 적과 충돌 시 총알 즉시 파괴
            if (!isPiercing)
            {
                Destroy(gameObject);
            }
        }
    }
}
