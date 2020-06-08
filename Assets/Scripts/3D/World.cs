using System.Collections;
using System.Collections.Generic;
using CoherentNoise.Generation;
using CoherentNoise.Generation.Displacement;
using CoherentNoise.Generation.Fractal;
using CoherentNoise.Generation.Modification;
using CoherentNoise.Generation.Patterns;
using CoherentNoise.Texturing;
using UnityEngine;

public class World : MonoBehaviour
{
    //public byte[,,] data;
    public int worldX;
    public int worldY;
    public int worldZ;
    public int worldYMultiplier = 2;



    public float exp;
    public float gain;
    public float offset; 

    public GameObject chunk;
    public Chunk[,,] chunks;  //Changed from public GameObject[,,] chunks;
    public int chunkSize = 16;

    [Tooltip("size of siungle voxel - value must be power of 2")]
    public float voxelScale;

    private bool terrainGenerated;
    public bool globalUpdateNeeded;
    // Start is called before the first frame update
    void Start()
    {
        terrainGenerated = false;
        globalUpdateNeeded = true;

        chunks = new Chunk[Mathf.FloorToInt(worldX / chunkSize),
Mathf.FloorToInt(1), Mathf.FloorToInt(worldZ / chunkSize)];

        for (int x = 0; x < chunks.GetLength(0); x++)
        {
            for (int y = 0; y < 1; y++)
            {
                for (int z = 0; z < chunks.GetLength(2); z++)
                {

                    //Create a temporary Gameobject for the new chunk instead of using chunks[x,y,z]
                    GameObject newChunk = Instantiate(chunk, new Vector3(x * chunkSize/2,
                     y * chunkSize, z * chunkSize/2), new Quaternion(0, 0, 0, 0)) as GameObject;

                    //Now instead of using a temporary variable for the script assign it
                    //to chunks[x,y,z] and use it instead of the old \"newChunkScript\" 
                    chunks[x, y, z] = newChunk.GetComponent("Chunk") as Chunk;
                    chunks[x, y, z].worldGO = gameObject;
                    chunks[x, y, z].chunkSize = chunkSize;
                    chunks[x, y, z].chunkX = x * chunkSize/2;
                    chunks[x, y, z].chunkY = y * chunkSize;
                    chunks[x, y, z].chunkZ = z * chunkSize/2;
                    chunks[x, y, z].voxelScale = voxelScale;
                    chunks[x, y, z].SetParentWorld(this);
                    //chunks[x, y, z].GenerateTerrain();

                }
            }
        }

    }

    public VoxelTypeEnum SearchBlock(int x, int y, int z)
    {
        return VoxelTypeEnum.AIR;
        var chunk = chunks[Mathf.FloorToInt(x / (chunkSize / 2)), 1, Mathf.FloorToInt(z / (chunkSize / 2))];
        return chunk.Block((int)((x-chunk.chunkX)/voxelScale), (int)((y - chunk.chunkY) / voxelScale), (int)((z - chunk.chunkZ) / voxelScale));
    }

    // Update is called once per frame
    void Update()
    {
        //if (terrainGenerated == false && globalUpdateNeeded)
        //{
        //    bool generated = true;
        //    foreach (var item in chunks)
        //    {
        //        if (item.terrainGenerated == false)
        //        {
        //            generated = false;
        //            break;
        //        }
        //    }
        //    if (generated)
        //    {
        //        terrainGenerated = true;
        //    }
        //}
        //else if (terrainGenerated && globalUpdateNeeded)
        //{
        //    foreach (var item in chunks)
        //    {
        //        //item.marchCubes(Chunk.marchingCubeMode.edges);
        //    }
        //    foreach (var item in chunks)
        //    {
        //        //item.SmoothVertices();
        //        //item.smoothingDone = true;
        //        //item.meshUpdateNeeded = true;
        //    }
        //    foreach (var item in chunks)
        //    {
        //        //item.SmoothVertices();
        //        item.smoothingDone = true;
        //        item.meshUpdateNeeded = true;
        //    }
        //    globalUpdateNeeded = false;
        //}
    }

}
