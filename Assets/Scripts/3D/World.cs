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

    public RidgeNoise Noise;
    //public BillowNoise Noise;
    public Bias BiasObj;
    public Gain GainObj;

    public float exp;
    public float gain;
    public float offset;

    public GameObject chunk;
    public Chunk[,,] chunks;  //Changed from public GameObject[,,] chunks;
    public int chunkSize = 16;

    [Tooltip("size of siungle voxel - value must be power of 2")]
    public float voxelScale;

    // Start is called before the first frame update
    void Start()
    {
        Noise = new RidgeNoise(1);
        Noise.Exponent = exp;// 1.0f;
        Noise.Gain = gain;// 1.2f;
        Noise.Offset = offset;// 0.7f;
        //Noise = new BillowNoise(4);

        BiasObj = new Bias(Noise, -0.2f);
        GainObj = new Gain(BiasObj, -0.2f);


        chunks = new Chunk[Mathf.FloorToInt(worldX / chunkSize),
Mathf.FloorToInt(1), Mathf.FloorToInt(worldZ / chunkSize)];

        for (int x = 0; x < chunks.GetLength(0); x++)
        {
            for (int y = 0; y < 1; y++)
            {
                for (int z = 0; z < chunks.GetLength(2); z++)
                {

                    //Create a temporary Gameobject for the new chunk instead of using chunks[x,y,z]
                    GameObject newChunk = Instantiate(chunk, new Vector3(x * chunkSize - 0.5f,
                     y * chunkSize + 0.5f, z * chunkSize - 0.5f), new Quaternion(0, 0, 0, 0)) as GameObject;

                    //Now instead of using a temporary variable for the script assign it
                    //to chunks[x,y,z] and use it instead of the old \"newChunkScript\" 
                    chunks[x, y, z] = newChunk.GetComponent("Chunk") as Chunk;
                    chunks[x, y, z].worldGO = gameObject;
                    chunks[x, y, z].chunkSize = chunkSize;
                    chunks[x, y, z].chunkX = x * chunkSize;
                    chunks[x, y, z].chunkY = y * chunkSize;
                    chunks[x, y, z].chunkZ = z * chunkSize;
                    chunks[x, y, z].voxelScale = voxelScale;
                    //chunks[x, y, z].GenerateTerrain();

                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

    }

    public int PerlinNoise(int x, int y, int z, float scale, float height, float power)
    {
        float rValue;

        //rValue = Noise.GetValue(((float)x) / scale, ((float)y) / scale, ((float)z) / scale);
        //rValue = Noise.GetValue(((float)x), ((float)y), ((float)z));
        //rValue = BiasObj.GetValue(((float)x), ((float)y), ((float)z));
        try
        {
            rValue = GainObj.GetValue(((float)x / scale), ((float)y / 50), ((float)z / scale));
            if (rValue < 0)
            {
                rValue = -rValue;
            }
            rValue *= height;

            if (power != 0)
            {
                //rValue = Mathf.Pow(rValue, power);
            }
        }
        catch (System.Exception ex)
        {

            throw;
        }
        return (int)rValue;
    }

    //public byte Block(int x, int y, int z)
    //{

    //    if (x/ voxelScale >= worldX || x/ voxelScale < 0 || y/ voxelScale >= worldY || y/ voxelScale < 0 || z/ voxelScale >= worldZ || z/ voxelScale < 0)
    //    {
    //        return (byte)1;
    //    }

    //    try
    //    {
    //        return data[x, y, z];
    //    }
    //    catch (System.Exception ex)
    //    {

    //        return 0;
    //    }

    //}
}
