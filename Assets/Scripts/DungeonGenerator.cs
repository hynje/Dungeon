using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class DungeonGenerator : MonoBehaviour
{
    // --- 유니티 에디터 설정 ---
    [Header("Map Settings")]
    public int mapWidth = 50;
    public int mapHeight = 50;
    public int minRoomSize = 6;
    public int maxRoomSize = 12;

    [Header("Seed Settings")]
    public bool useRandomSeed = true;
    public int seed;

    [Header("Tilemap References")]
    public Tilemap tilemap;
    public TileBase floorTile;
    public TileBase wallTile;

    // --- 내부 변수 ---
    private System.Random prng; // 시드 기반 난수 생성기
    private int[,] mapData; // 논리적인 맵 데이터 (0: 벽, 1: 바닥)
    
    // 방 정보를 담는 Rect 리스트
    private List<RectInt> rooms = new List<RectInt>(); 

    // RectInt를 사용하여 BSP 노드 정의
    public class Node
    {
        public RectInt bounds;
        public Node leftChild;
        public Node rightChild;
        public RectInt room; // 이 노드에 최종적으로 생성될 방의 크기

        public Node(RectInt bounds)
        {
            this.bounds = bounds;
        }
    }

    // 최상위 노드
    private Node rootNode; 
    
    // =========================================================================
    
    void Start()
    {
        GenerateDungeon();
    }
    
    public void GenerateDungeon()
    {
        // 1. 시드 설정 및 난수 생성기 초기화
        if (useRandomSeed)
        {
            seed = System.DateTime.Now.Ticks.GetHashCode();
        }
        prng = new System.Random(seed);
        Debug.Log($"Dungeon Seed: {seed}");

        // 2. 맵 데이터 초기화 (모든 타일을 벽으로 시작)
        mapData = new int[mapWidth, mapHeight];
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                mapData[x, y] = 0; // 0 = Wall
            }
        }
        
        // 3. BSP 분할 시작
        rootNode = new Node(new RectInt(0, 0, mapWidth, mapHeight));
        SplitNode(rootNode);

        // 4. 방 생성 및 복도 연결
        rooms.Clear();
        CreateRoomsAndCorridors(rootNode);
        
        // [테스트용] 텍스트로 맵 출력
        // PrintDungeonToConsole(); 

        // 5. Tilemap 시각화는 잠시 주석 처리
        RenderMap();
    }

    // =========================================================================
    // BSP 로직
    // =========================================================================

    private void SplitNode(Node node)
    {
        // 수정 1: 분할을 멈추는 조건을 수정 
        // 방을 만들 때 -2(양쪽 벽)를 하므로, 노드는 최소 (minRoomSize + 2) 크기여야 안전함
        // 따라서 분할하려면 그 2배인 (minRoomSize + 2) * 2 이상이어야 함
        int requiredSpace = minRoomSize + 2;

        if (node.bounds.width < requiredSpace * 2 || node.bounds.height < requiredSpace * 2)
        {
            return;
        }

        bool splitH = prng.Next(0, 100) < 50;

        if (node.bounds.width > node.bounds.height && node.bounds.width / node.bounds.height > 1.5f)
        {
            splitH = false;
        }
        else if (node.bounds.height > node.bounds.width && node.bounds.height / node.bounds.width > 1.5f)
        {
            splitH = true;
        }

        if (splitH) // 가로 분할
        {
            // 수정 2: 분할 좌표를 정할 때도 여백(+2)을 고려해서 범위를 잡음
            // 이렇게 해야 자식 노드들이 무조건 (minRoomSize + 2) 이상의 크기를 가짐
            int splitY = prng.Next(node.bounds.y + requiredSpace, node.bounds.yMax - requiredSpace);

            node.leftChild = new Node(new RectInt(node.bounds.x, node.bounds.y, node.bounds.width, splitY - node.bounds.y));
            node.rightChild = new Node(new RectInt(node.bounds.x, splitY, node.bounds.width, node.bounds.yMax - splitY));
        }
        else // 세로 분할
        {
            // 수정 2: 분할 좌표를 정할 때도 여백(+2)을 고려해서 범위를 잡음
            int splitX = prng.Next(node.bounds.x + requiredSpace, node.bounds.xMax - requiredSpace);

            node.leftChild = new Node(new RectInt(node.bounds.x, node.bounds.y, splitX - node.bounds.x, node.bounds.height));
            node.rightChild = new Node(new RectInt(splitX, node.bounds.y, node.bounds.xMax - splitX, node.bounds.height));
        }

        SplitNode(node.leftChild);
        SplitNode(node.rightChild);
    }
    
    // =========================================================================
    // 방 및 복도 생성 로직
    // =========================================================================

    private void CreateRoomsAndCorridors(Node node)
    {
        // Leaf Node (더 이상 쪼개지지 않은 노드)에 방 생성
        if (node.leftChild == null && node.rightChild == null)
        {
            CreateRoom(node);
        }
        else // 부모 노드는 자식 노드들을 연결
        {
            CreateRoomsAndCorridors(node.leftChild);
            CreateRoomsAndCorridors(node.rightChild);
            
            // 두 자식 노드에서 생성된 방들을 복도로 연결
            ConnectRooms(GetRandomRoom(node.leftChild), GetRandomRoom(node.rightChild));
        }
    }

    private void CreateRoom(Node node)
    {
        // 노드 경계 내에서 임의의 방 크기 및 위치 결정
        int roomW = prng.Next(minRoomSize, Mathf.Min(maxRoomSize, node.bounds.width - 2));
        int roomH = prng.Next(minRoomSize, Mathf.Min(maxRoomSize, node.bounds.height - 2));

        int roomX = prng.Next(node.bounds.x + 1, node.bounds.xMax - roomW - 1);
        int roomY = prng.Next(node.bounds.y + 1, node.bounds.yMax - roomH - 1);

        node.room = new RectInt(roomX, roomY, roomW, roomH);
        rooms.Add(node.room); // 생성된 방을 리스트에 추가

        // 맵 데이터에 방 바닥 표시 (1)
        for (int x = node.room.x; x < node.room.xMax; x++)
        {
            for (int y = node.room.y; y < node.room.yMax; y++)
            {
                mapData[x, y] = 1; // 1 = Floor
            }
        }
    }

    // 자식 노드에서 생성된 방 중 하나를 반환 (복도 연결용)
    private RectInt GetRandomRoom(Node node)
    {
        if (node.leftChild == null && node.rightChild == null)
        {
            return node.room;
        }
        
        // 재귀적으로 내려가서 최종적으로 방을 가진 노드 중 하나를 반환
        return prng.Next(0, 2) == 0 ? GetRandomRoom(node.leftChild) : GetRandomRoom(node.rightChild);
    }

    private void ConnectRooms(RectInt room1, RectInt room2)
    {
        Vector2Int center1 = room1.center.ToInt();
        Vector2Int center2 = room2.center.ToInt();

        // 1. 첫 번째 방의 중심점에서 시작하여 수직(Y) 이동
        while (center1.y != center2.y)
        {
            if (center1.y < center2.y) center1.y++;
            else center1.y--;
            
            // 방(1)이 아닌 곳만 복도(2)로 표시
            if(IsWithinBounds(center1) && mapData[center1.x, center1.y] != 1) 
            {
                mapData[center1.x, center1.y] = 2; 
            }
        }

        // 2. 수직 이동이 끝난 지점에서 수평(X) 이동 (ㄱ자 복도 생성)
        while (center1.x != center2.x)
        {
            if (center1.x < center2.x) center1.x++;
            else center1.x--;
            
            // 방(1)이 아닌 곳만 복도(2)로 표시
            if(IsWithinBounds(center1) && mapData[center1.x, center1.y] != 1) 
            {
                mapData[center1.x, center1.y] = 2; 
            }
        }
    }

    // =========================================================================
    // 렌더링 로직
    // =========================================================================

    // System.Text 네임스페이스 필요 (맨 위에 using System.Text; 추가)
    private void PrintDungeonToConsole()
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        // 위쪽(Y가 큰 쪽)부터 아래로 출력해야 우리가 보는 지도 방향과 맞음
        for (int y = mapHeight - 1; y >= 0; y--)
        {
            for (int x = 0; x < mapWidth; x++)
            {
                int tileType = mapData[x, y];
                
                if (tileType == 0) sb.Append("0 ");      // 벽
                else if (tileType == 1) sb.Append("1 "); // 방
                else if (tileType == 2) sb.Append("2 "); // 복도
            }
            sb.Append("\n"); // 줄바꿈
        }
        
        // 유니티 콘솔에 출력
        Debug.Log(sb.ToString());
    }
    
    private void RenderMap()
    {
        tilemap.ClearAllTiles();
        for (int x = 0; x < mapWidth; x++)
        {
            for (int y = 0; y < mapHeight; y++)
            {
                Vector3Int pos = new Vector3Int(x, y, 0);
                
                if (mapData[x, y] == 1) // 바닥
                {
                    tilemap.SetTile(pos, floorTile);
                }
                else if (mapData[x, y] == 2)
                {
                    tilemap.SetTile(pos, floorTile);
                }
                else // 벽
                {
                    // if (CheckIfWallShouldBeDrawn(x, y))
                    // {
                    //     tilemap.SetTile(pos, wallTile);
                    // }
                    tilemap.SetTile(pos, wallTile);
                }
            }
        }
    }

    // 최적화: 바닥(1)에 인접한 벽(0)만 실제로 렌더링
    private bool CheckIfWallShouldBeDrawn(int x, int y)
    {
        for (int offsetX = -1; offsetX <= 1; offsetX++)
        {
            for (int offsetY = -1; offsetY <= 1; offsetY++)
            {
                if (offsetX == 0 && offsetY == 0) continue;

                int checkX = x + offsetX;
                int checkY = y + offsetY;

                if (IsWithinBounds(checkX, checkY) && mapData[checkX, checkY] == 1)
                {
                    return true;
                }
            }
        }
        return false;
    }
    
    private bool IsWithinBounds(int x, int y)
    {
        return x >= 0 && x < mapWidth && y >= 0 && y < mapHeight;
    }
    
    private bool IsWithinBounds(Vector2Int p)
    {
        return p.x >= 0 && p.x < mapWidth && p.y >= 0 && p.y < mapHeight;
    }
}

public static class RectIntExtension
{
    // RectInt의 중심점을 Vector2Int로 변환하는 확장 메서드
    public static Vector2Int ToInt(this Vector2 center)
    {
        return new Vector2Int(Mathf.RoundToInt(center.x), Mathf.RoundToInt(center.y));
    }
}
