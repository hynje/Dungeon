using UnityEngine;
using UnityEngine.UI;

public class MinimapRenderer : MonoBehaviour
{
    [Header("References")]
    public DungeonGenerator dungeonGenerator; // 맵 데이터를 가져올 곳
    public RawImage minimapDisplay;           // 미니맵을 그릴 UI
    public RectTransform playerIcon;          // 플레이어 점 (UI)
    public Transform playerTransform;         // 실제 플레이어 게임오브젝트

    [Header("Settings")]
    public Color wallOutlineColor = Color.white; // 벽 테두리 색
    public Color floorColor = new Color(0, 0, 1, 0.5f); // 바닥 색 (반투명 파랑)
    public Color emptyColor = Color.clear;       // 빈 공간 색 (투명)

    private float mapScaleX;
    private float mapScaleY;
    private int width, height;

    // 던전 생성이 끝난 뒤 호출해주세요.
    public void DrawMinimap()
    {
        int[,] mapData = dungeonGenerator.GetMapData(); // ※ DungeonGenerator에 Getter 필요
        width = mapData.GetLength(0);
        height = mapData.GetLength(1);

        // 1. 맵 크기에 맞는 텍스처 생성
        Texture2D texture = new Texture2D(width, height);
        texture.filterMode = FilterMode.Point; // 도트가 뭉개지지 않게 설정

        // 2. 픽셀 하나하나 색칠하기
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Color pixelColor = emptyColor;

                if (mapData[x, y] > 0) // 바닥(1)이나 복도(2)
                {
                    pixelColor = floorColor;
                }
                else // 벽(0)
                {
                    // [핵심 로직] 벽이지만 '바닥과 인접한 벽'만 흰색으로 칠함
                    if (IsWallOutline(x, y, mapData))
                    {
                        pixelColor = wallOutlineColor;
                    }
                }
                
                texture.SetPixel(x, y, pixelColor);
            }
        }

        texture.Apply(); // 변경사항 적용
        minimapDisplay.texture = texture; // UI에 텍스처 적용

        // 미니맵 UI 크기에 따른 비율 계산 (플레이어 아이콘 이동용)
        mapScaleX = minimapDisplay.rectTransform.rect.width / width;
        mapScaleY = minimapDisplay.rectTransform.rect.height / height;
    }

    // 8방향 중 하나라도 바닥이 있으면 '외곽선'으로 취급
    private bool IsWallOutline(int x, int y, int[,] mapData)
    {
        int w = mapData.GetLength(0);
        int h = mapData.GetLength(1);

        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0) continue;

                int checkX = x + i;
                int checkY = y + j;

                // 맵 범위 안이고, 해당 위치가 바닥(>0)이라면 -> 나는 외곽선이다!
                if (checkX >= 0 && checkX < w && checkY >= 0 && checkY < h)
                {
                    if (mapData[checkX, checkY] > 0) return true;
                }
            }
        }
        return false;
    }

    void Update()
    {
        // 플레이어 아이콘 실시간 동기화
        if (playerTransform != null && playerIcon != null && width > 0)
        {
            // 맵 좌표(Grid)를 UI 좌표(AnchoredPosition)로 변환
            // RawImage는 Pivot이 (0,1) Top-Left 기준이라고 가정하면 좌표계산이 조금 복잡할 수 있음.
            // 가장 쉬운 방법: RawImage와 PlayerIcon 모두 Pivot을 (0,0) Bottom-Left로 맞추는 것.
            // 여기서는 RawImage 내에서의 상대 좌표 비율을 사용합니다.
            
            float pX = playerTransform.position.x;
            float pY = playerTransform.position.y;

            // 미니맵 상에서의 위치 계산
            Vector2 iconPos = new Vector2(pX * mapScaleX, pY * mapScaleY);
            
            // PlayerIcon의 앵커가 Bottom-Left(0,0)라고 가정할 때:
            playerIcon.anchoredPosition = iconPos;
        }
    }
}
