using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance;
    public bool IsPlayerTurn { get; private set; } = true;

    // 턴이 끝났음을 알리는 이벤트
    public event Action OnPlayerTurnEnded;

    void Awake()
    {
        Instance = this;
    }

    public void EndPlayerTurn()
    {
        IsPlayerTurn = false;
        // 여기에 적들의 이동 로직을 호출하는 코드가 들어갑니다.
        // 예: StartCoroutine(EnemyTurnRoutine());
        
        Debug.Log("플레이어 턴 종료 -> 적 턴 시작");
        
        // (임시) 적 턴이 없으니 0.1초 뒤 바로 플레이어 턴으로 복귀
        Invoke("StartPlayerTurn", 0.1f);
    }

    public void StartPlayerTurn()
    {
        IsPlayerTurn = true;
        Debug.Log("플레이어 턴 시작");
    }
}
