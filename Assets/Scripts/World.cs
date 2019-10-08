using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class World : MonoBehaviour
{
    public int chunkSize = 8;

    public int worldWidth = 5;
    public int worldHeight = 5;
    public int worldDepth = 5;

    public float isolevel;

    public float renderDistance = 3.0f;

    public int seed;

    public GameObject chunkPrefab;

    public GameObject player;

    public Dictionary<Vector3Int, Chunk> chunks;
    public Dictionary<Vector3Int, Chunk> unloadedChunks;

    private Bounds worldBounds;

    public DensityGenerator densityGenerator;

    private void Awake()
    {
        densityGenerator = new DensityGenerator(seed);
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
    }

    private void Update()
    {
        CheckChunks();
        UpdateBounds();
    }

    public void Initialise()
    {

        worldBounds = new Bounds();
        UpdateBounds();

        chunks = new Dictionary<Vector3Int, Chunk>();
        unloadedChunks = new Dictionary<Vector3Int, Chunk>();
        CreateChunks();
    }

    private void Start()
    {
        Initialise();
    }

    public void CreateChunks()
    {
        for (int x = 0; x < worldWidth; x++)
        {
            for (int y = 0; y < worldHeight; y++)
            {
                for (int z = 0; z < worldDepth; z++)
                {
                    CreateChunk(x * chunkSize, y * chunkSize, z * chunkSize);
                }
            }
        }
    }

    private Chunk GetChunk(Vector3Int pos)
    {
        return GetChunk(pos.x, pos.y, pos.z);
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        int newX = Utils.FloorToNearestX(x, chunkSize);
        int newY = Utils.FloorToNearestX(y, chunkSize);
        int newZ = Utils.FloorToNearestX(z, chunkSize);
        var allChunks = chunks.Concat(unloadedChunks).ToDictionary(a => a.Key, a => a.Value);
        return allChunks[new Vector3Int(newX, newY, newZ)];
    }

    public float GetDensity(int x, int y, int z)
    {
        Point p = GetPoint(x, y, z);

        return p.density;
    }

    public float GetDensity(Vector3Int pos)
    {
        return GetDensity(pos.x, pos.y, pos.z);
    }

    public Point GetPoint(int x, int y, int z)
    {
        Chunk chunk = GetChunk(x, y, z);

        Point p = chunk.GetPoint(x.Mod(chunkSize),
                                 y.Mod(chunkSize),
                                 z.Mod(chunkSize));

        return p;
    }

    public void SetDensity(float density, int worldPosX, int worldPosY, int worldPosZ, bool setReadyForUpdate, Chunk[] initChunks)
    {
        Vector3Int dp = new Vector3Int(worldPosX, worldPosY, worldPosZ);

        Vector3Int lastChunkPos = dp.FloorToNearestX(chunkSize);

        for (int i = 0; i < 8; i++)
        {
            Vector3Int chunkPos = (dp - MarchingCubes.CubePoints[i]).FloorToNearestX(chunkSize);

            if (i != 0 && chunkPos == lastChunkPos)
            {
                continue;
            }

            Chunk chunk = GetChunk(chunkPos);
            
            lastChunkPos = chunk.position;

            Vector3Int localPos = (dp - chunk.position).Mod(chunkSize + 1);

            chunk.SetDensity(density, localPos);
            if (setReadyForUpdate) 
                chunk.readyForUpdate = true;
        }
    }

    public void SetDensity(float density, Vector3Int pos, bool setReadyForUpdate, Chunk[] initChunks)
    {
        SetDensity(density, pos.x, pos.y, pos.z, setReadyForUpdate, initChunks);
    }

    private void UpdateBounds()
    {
        float middleX = player.transform.position.x * chunkSize / 2f;
        float middleY = player.transform.position.y * chunkSize / 2f;
        float middleZ = player.transform.position.z * chunkSize / 2f;
        
        Vector3 midPos = new Vector3(middleX, middleY, middleZ);

        Vector3Int size = new Vector3Int(
            worldWidth * chunkSize,
            worldHeight * chunkSize,
            worldDepth * chunkSize);

        worldBounds.center = midPos;
        worldBounds.size = size;
    }

    public bool IsPointInsideWorld(int x, int y, int z)
    {
        return true;// IsPointInsideWorld(new Vector3Int(x, y, z));
    }

    public bool IsPointInsideWorld(Vector3Int point)
    {
        return true;// worldBounds.Contains(point);
    }

    private void CreateChunk(int x, int y, int z)
    {
        int newX = Utils.FloorToNearestX(x, chunkSize);
        int newY = Utils.FloorToNearestX(y, chunkSize);
        int newZ = Utils.FloorToNearestX(z, chunkSize);
        Vector3Int position = new Vector3Int(newX, newY, newZ);
        
        if (!chunks.ContainsKey(position))
        {
            Chunk chunk = Instantiate(chunkPrefab, position, Quaternion.identity).GetComponent<Chunk>();
            chunk.Initialize(this, chunkSize, position);
            chunks.Add(position, chunk);
        }
    }

    private void LoadChunks() {
        Vector3 playerPos = player.transform.position; //player centre
        int xPos = (int)playerPos.x;
        int yPos = (int)playerPos.y;
        int zPos = (int)playerPos.z;
        int newX = Utils.FloorToNearestX(xPos, chunkSize);
        int newY = Utils.FloorToNearestX(yPos, chunkSize);
        int newZ = Utils.FloorToNearestX(zPos, chunkSize);

        for (int i = newX - chunkSize; i < newX + (renderDistance * chunkSize); i += chunkSize) {
            for (int j = newY - chunkSize; j < newY + (renderDistance * chunkSize); j += chunkSize) {
                for (int k = newZ - chunkSize; k < newZ + (renderDistance * chunkSize); k += chunkSize)
                {
                    Vector3Int position = new Vector3Int(i, j, k);
                    if (!chunks.ContainsKey(position))
                    {
                        if (unloadedChunks.ContainsKey(position))
                        {
                            LoadChunk(GetChunk(i, j, k));
                            return;
                        }
                        CreateChunk(i, j, k);
                    }
                }
                
            }
        }



    }

    private void CheckChunks()
    {
        LoadChunks();
        List<Chunk> chunkList = new List<Chunk>(chunks.Values);
        List<Chunk> unloadedChunkList = new List<Chunk>(unloadedChunks.Values);
        Queue<Chunk> chunksToDelete = new Queue<Chunk>();
        
        for (int i =0; i< chunkList.Count; i++)
        {
            float distance = Vector3.Distance(player.transform.position, chunkList[i].transform.position);

            if (distance >= renderDistance * chunkSize)
            {
                UnloadChunk(chunkList[i]);
            }
        }

    }

    private void LoadChunk(Chunk c)
    {
        c.gameObject.SetActive(true);
        c.enabled = true;
        //Instantiate(chunkPrefab, c.position, Quaternion.identity);
        unloadedChunks.Remove(c.position);
        chunks.Add(c.position, c);
    }

    private void UnloadChunk(Chunk c) {
        chunks.Remove(Vector3Int.CeilToInt(c.position));
        unloadedChunks.Add(Vector3Int.CeilToInt(c.position), c);
        c.gameObject.SetActive(false);
        c.enabled = false;
        //Destroy(c.gameObject);
    }
}
