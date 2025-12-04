using UnityEngine;

public class AimingLineController : MonoBehaviour
{
    private LineRenderer lineRenderer;

    [Header("Aiming Line Settings")]
    [SerializeField] private float lineLength = 100f; // 보조선의 최대 길이
    [SerializeField] private float startWidth = 0.05f; // 보조선 시작 두께
    [SerializeField] private float endWidth = 0.05f;   // 보조선 끝 두께
    [SerializeField] private Material lineMaterial;    // 보조선에 사용할 재질 (Inspector에서 할당 필요)

    // PlayerController의 bulletSpawnPoint에 접근하기 위한 참조
    [SerializeField] private Transform bulletSpawnPoint; 
    
    private Camera mainCamera;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null)
        {
            Debug.LogError("AimingLineController requires a LineRenderer component on the same GameObject.");
            enabled = false; // 컴포넌트가 없으면 스크립트 비활성화
            return;
        }

        SetupLineRenderer();
        mainCamera = Camera.main;
    }

    void SetupLineRenderer()
    {
        lineRenderer.positionCount = 2; // 시작점과 끝점 2개
        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
        if (lineMaterial != null)
        {
            lineRenderer.material = lineMaterial;
        }
        else
        {
            Debug.LogWarning("AimingLineController: Line Material is not assigned. Using default.");
        }
        lineRenderer.useWorldSpace = true; // 월드 좌표계 사용
        lineRenderer.enabled = true; // 기본적으로 활성화
    }

    void Update()
    {
        if (bulletSpawnPoint == null || mainCamera == null)
        {
            lineRenderer.enabled = false;
            return;
        }

        // 마우스 위치로 레이 캐스팅
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        Vector3 targetPoint;

        // 레이가 어떤 물체에 닿았을 경우
        if (Physics.Raycast(ray, out hit, lineLength))
        {
            targetPoint = hit.point;
        }
        else // 닿지 않았을 경우 최대 길이까지
        {
            targetPoint = ray.origin + ray.direction * lineLength;
        }

        // LineRenderer의 시작점과 끝점 설정
        lineRenderer.SetPosition(0, bulletSpawnPoint.position);
        lineRenderer.SetPosition(1, targetPoint);
    }
}
