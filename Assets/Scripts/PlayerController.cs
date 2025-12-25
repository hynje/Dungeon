using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Settings")]
    public float moveSpeed = 5f; // 이동 속도 (클수록 빠름)
    
    [Header("References")]
    public DungeonGenerator dungeonGenerator; // 맵 데이터 참조용
    public Animator animator;

    private Vector2Int gridPosition; // 현재 그리드 좌표
    private bool isMoving = false;   // 이동 중인지 체크
    private Vector2Int inputVector;  // 입력 방향

    void Start()
    {
        // 게임 시작 시 플레이어를 랜덤한 방에 배치 (나중에 구현 필요)
        // 일단은 (0,0)이나 안전한 곳으로 초기화한다고 가정
    }

    // 던전 생성 후 플레이어 초기 위치를 잡을 때 호출할 함수
    public void Spawn(Vector2Int startPos)
    {
        gridPosition = startPos;
        transform.position = new Vector3(startPos.x, startPos.y, 0);
    }

    void Update()
    {
        // 1. 내 턴이 아니거나 이미 움직이는 중이면 입력 무시
        if (!TurnManager.Instance.IsPlayerTurn || isMoving) return;

        // 2. 입력 감지와 방향 전환 (수평/수직 우선순위 없이 동시 입력 방지 로직)
        float x = Input.GetAxisRaw("Horizontal");
        float y = Input.GetAxisRaw("Vertical");

        float posX = x;
        float posY = y;
        animator.SetFloat("PosX", posX);
        animator.SetFloat("PosY", posY);
        
        if (x != 0 || y != 0)
        {
            inputVector = new Vector2Int((int)x, (int)y);
            AttemptMove(inputVector);
        }
    }

    private void AttemptMove(Vector2Int direction)
    {
        Vector2Int targetPos = gridPosition + direction;

        // 3. 이동 가능한지 검사 (벽 체크)
        if (IsWalkable(targetPos))
        {
            StartCoroutine(MoveRoutine(targetPos));
        }
        else
        {
            // 벽에 막힘 (이동 애니메이션 없이 턴만 소비할지, 아예 막을지 결정)
            // PMD는 벽에 부딪히면 이동하지 않고 제자리 걸음 소리만 나고 턴 소비 안 함
        }
    }

    // 맵 데이터를 통해 이동 가능 여부 판단
    private bool IsWalkable(Vector2Int pos)
    {
        int[,] mapData = dungeonGenerator.GetMapData();
        int width = mapData.GetLength(0);
        int height = mapData.GetLength(1);

        // 맵 범위 밖 체크
        if (pos.x < 0 || pos.x >= width || pos.y < 0 || pos.y >= height) return false;

        // 0은 벽, 1 이상(1, 2)은 바닥
        return mapData[pos.x, pos.y] > 0;
    }

    private IEnumerator MoveRoutine(Vector2Int targetPos)
    {
        isMoving = true;
        animator.SetBool("IsMoving", true); // 걷기 애니메이션 시작

        Vector3 startWorldPos = transform.position;
        Vector3 targetWorldPos = new Vector3(targetPos.x, targetPos.y, 0);
        float elapsedTime = 0;

        // 이동하는 데 걸리는 시간 (거리 / 속도)
        // PMD 특유의 톡, 톡 끊어지는 느낌을 주려면 고정 시간을 쓰는 게 좋습니다 (예: 0.2초)
        float duration = 1f / moveSpeed; 

        while (elapsedTime < duration)
        {
            transform.position = Vector3.Lerp(startWorldPos, targetWorldPos, elapsedTime / duration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        // 5. 이동 완료 후 좌표 보정
        transform.position = targetWorldPos;
        gridPosition = targetPos;
        
        animator.SetBool("IsMoving", false); // Idle로 복귀
        isMoving = false;

        // 6. 턴 종료 알림 -> TurnManager가 적들을 움직이게 함
        TurnManager.Instance.EndPlayerTurn();
    }
}
