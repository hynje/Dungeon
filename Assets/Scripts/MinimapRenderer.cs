using System.Collections.Generic;
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
    private Texture2D minimapTexture;

    // 1. 게임 시작(던전 생성 직후) 시 1회 호출
    public void InitMinimap()
    {
        int[,] mapData = dungeonGenerator.GetMapData(); 
        width = mapData.GetLength(0);
        height = mapData.GetLength(1);

        // 텍스처 생성 및 투명하게 초기화
        minimapTexture = new Texture2D(width, height);
        minimapTexture.filterMode = FilterMode.Point; 

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                minimapTexture.SetPixel(x, y, emptyColor);
            }
        }
        minimapTexture.Apply();
        minimapDisplay.texture = minimapTexture;

        mapScaleX = minimapDisplay.rectTransform.rect.width / width;
        mapScaleY = minimapDisplay.rectTransform.rect.height / height;
    }

    // 2. 플레이어가 이동할 때마다 호출되어 시야를 밝힘
    public void UpdateExploredArea(Vector2Int playerPos)
    {
        if (minimapTexture == null) return;

        int[,] mapData = dungeonGenerator.GetMapData();
        List<RectInt> rooms = dungeonGenerator.GetRooms();
        bool[,] explored = dungeonGenerator.explored;
        
        bool isTextureChanged = false;

        // A. 플레이어 주변 3x3 칸 밝히기 (복도 시야)
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                int checkX = playerPos.x + i;
                int checkY = playerPos.y + j;
                if (RevealTile(checkX, checkY, mapData, explored)) isTextureChanged = true;
            }
        }

        // B. 방(Room) 안에 있다면 해당 방 전체 밝히기
        foreach (RectInt room in rooms)
        {
            // 플레이어가 이 방 안에 있는지 체크
            if (room.Contains(playerPos))
            {
                // 방의 바닥뿐만 아니라, 방을 둘러싼 벽 테두리(-1 ~ +1)까지 밝혀야 깔끔함
                for (int x = room.x - 1; x <= room.xMax; x++)
                {
                    for (int y = room.y - 1; y <= room.yMax; y++)
                    {
                        if (RevealTile(x, y, mapData, explored)) isTextureChanged = true;
                    }
                }
                break; // 이미 방을 찾았으니 다른 방은 검사 안 함
            }
        }

        // 변경된 픽셀이 있다면 텍스처 적용
        if (isTextureChanged)
        {
            minimapTexture.Apply();
        }
    }

    // 특정 타일을 미니맵에 그리고, 새로 밝혀졌다면 true 반환
    private bool RevealTile(int x, int y, int[,] mapData, bool[,] explored)
    {
        if (x < 0 || x >= width || y < 0 || y >= height) return false;
        
        // 이미 밝혀진 곳이면 무시
        if (explored[x, y]) return false;

        explored[x, y] = true; // 탐색 완료 처리
        Color pixelColor = emptyColor;

        if (mapData[x, y] > 0) // 바닥/복도
        {
            pixelColor = floorColor;
        }
        else if (IsWallOutline(x, y, mapData)) // 외곽선 벽
        {
            pixelColor = wallOutlineColor;
        }

        minimapTexture.SetPixel(x, y, pixelColor);
        return true;
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
        // 플레이어 아이콘 실시간 동기화 (기존 로직 유지)
        if (playerTransform != null && playerIcon != null && width > 0)
        {
            float pX = playerTransform.position.x;
            float pY = playerTransform.position.y;
            playerIcon.anchoredPosition = new Vector2(pX * mapScaleX, pY * mapScaleY);
        }
    }
}
