using UnityEngine;

public class CameraController : MonoBehaviour
{
    [Header("Target")]
    public Transform playerTransform; // 플레이어의 Transform을 연결

    [Header("Settings")]
    public float smoothSpeed = 5f;    // 카메라가 따라가는 속도 (높을수록 빠름)
    public Vector3 offset = new Vector3(0, 0, -10f); // 카메라와 플레이어 간의 거리 유지

    void LateUpdate() // 플레이어 이동 로직(Update)이 끝난 후 카메라가 움직여야 덜덜거림이 없음
    {
        if (playerTransform == null) return;

        // 1. 목표 위치 계산 (플레이어 위치 + 오프셋)
        Vector3 targetPosition = playerTransform.position + offset;

        // 2. 현재 위치에서 목표 위치까지 부드럽게 이동 (Lerp 사용)
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);

        // 3. 카메라 위치 적용
        transform.position = smoothedPosition;
    }
}