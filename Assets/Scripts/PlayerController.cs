using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f; // 이동 속도
    public float snapDistance = 0.01f; // 목표 지점 도달 판정 거리

    [Header("References")]
    public DungeonGenerator dungeonGenerator;
    public Animator animator;
    public MinimapRenderer minimap;

    private Vector2Int gridPosition;
    private bool isMoving = false;
    private Vector2Int inputVector; // 현재 입력된 방향

    void Start()
    {
        // 초기화 필요 시 사용
    }

    public void Spawn(Vector2Int startPos)
    {
        gridPosition = startPos;
        transform.position = new Vector3(startPos.x + 0.5f, startPos.y + 0.5f, 0);
        
        // 태어난 곳 주변 시야 밝히기
        if (minimap != null) minimap.UpdateExploredArea(gridPosition);
    }

    void Update()
    {
        // 내 턴이 아니면 입력 자체를 차단
        if (!TurnManager.Instance.IsPlayerTurn) return;

        // 1. 입력 감지 (이동 중이어도 입력은 계속 받음 -> 선입력 효과)
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        // 대각선 이동 방지
        // if (x != 0) y = 0;

        // 입력이 있으면 벡터 갱신, 없으면 (0,0)
        if (x != 0 || y != 0) inputVector = new Vector2Int((int)x, (int)y);
        else inputVector = Vector2Int.zero;

        // 2. 이동 중이 아니고, 입력값이 있다면 이동 시작
        if (!isMoving && inputVector != Vector2Int.zero)
        {
            // 이동 시도
            AttemptMove(inputVector);
        }
    }

    private void AttemptMove(Vector2Int direction)
    {
        Vector2Int targetPos = gridPosition + direction;

        if (IsWalkable(targetPos))
        {
            // 코루틴 시작
            StartCoroutine(MoveRoutine(targetPos));
        }
        else
        {
            // 벽에 막힘: 이동하지 않더라도 방향은 바라보게 하기
            UpdateFacingDirection(direction);
        }
    }

    private bool IsWalkable(Vector2Int pos)
    {
        int[,] mapData = dungeonGenerator.GetMapData();
        int width = mapData.GetLength(0);
        int height = mapData.GetLength(1);

        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return false;
        return mapData[pos.x, pos.y] > 0;
    }

    private void UpdateFacingDirection(Vector2Int dir)
    {
        // (0, 0)일 때는 방향을 업데이트하지 않아야 
        // 마지막에 바라보던 방향을 유지한 채로 Idle 상태가 됩니다.
        if (dir == Vector2Int.zero) return;

        // 애니메이터에 파라미터 전달 (Blend Tree용)
        animator.SetFloat("PosX", dir.x);
        animator.SetFloat("PosY", dir.y);
    }

    private IEnumerator MoveRoutine(Vector2Int targetPos)
    {
        isMoving = true;
        animator.SetBool("IsMoving", true);
        
        // 방향 전환
        UpdateFacingDirection(targetPos - gridPosition);

        Vector3 targetWorldPos = new Vector3(targetPos.x + 0.5f, targetPos.y + 0.5f, 0);

        // [핵심] while 루프를 사용하여 목표에 '거의' 도달할 때까지 이동
        while ((targetWorldPos - transform.position).sqrMagnitude > float.Epsilon)
        {
            // MoveTowards를 사용하면 정확한 지점에 딱 멈춥니다 (Lerp보다 끊김 제어 유리)
            transform.position = Vector3.MoveTowards(transform.position, targetWorldPos, moveSpeed * Time.deltaTime);
            yield return null; // 한 프레임 대기
        }

        // 좌표 확정
        transform.position = targetWorldPos;
        gridPosition = targetPos;

        // 이동을 마친 후 새 위치에서 미니맵 시야 갱신
        if (minimap != null) minimap.UpdateExploredArea(gridPosition);
        
        // 턴 종료 처리 (적이 있다면 여기서 적의 움직임을 기다림)
        TurnManager.Instance.EndPlayerTurn();

        // [핵심 로직: 연속 이동]
        // 턴이 끝났는데 키를 계속 누르고 있고(inputVector != 0), 
        // 다시 내 턴이 바로 돌아왔다면(적이 없어서) -> 즉시 다음 이동 시작
        if (TurnManager.Instance.IsPlayerTurn && inputVector != Vector2Int.zero && IsWalkable(gridPosition + inputVector))
        {
            // Idle 상태로 가지 않고 바로 다음 코루틴 실행
            StartCoroutine(MoveRoutine(gridPosition + inputVector));
        }
        else
        {
            // 키를 뗐거나 막혔다면 멈춤
            isMoving = false;
            animator.SetBool("IsMoving", false);
        }
    }
}