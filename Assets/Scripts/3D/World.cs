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
    public byte[,,] data;
    public int worldX;
    public int worldY;
    public int worldZ;

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
        
        data = new byte[worldX, worldY, worldZ];

        for (int x = 0; x < worldX; x++)
        {
            for (int z = 0; z < worldZ; z++)
            {
                int stone = PerlinNoise(x, 0, z, 200, worldY, 4.2f);
                //stone += PerlinNoise(x, 300, z, 20, 4, 1.5f) + 10;
                int dirt = PerlinNoise(x, 100, z, 200, worldY, 0) + 1; //Added +1 to make sure minimum grass height is 1
                //Debug.Log($"stone: {stone} , x: {x}, z: {z}");
                for (int y = 0; y < worldY; y++)
                {
                    if (y <= stone)
                    {
                        data[x, y, z] = 1;
                    }
                    else if (y <= dirt + stone)
                    { 
                        data[x, y, z] = 2;
                    }
                    else if (y==0)
                    {
                        data[x, y, z] = 1;
                    }

                }
            }
        }

        chunks = new Chunk[Mathf.FloorToInt(worldX / chunkSize),
Mathf.FloorToInt(worldY / chunkSize), Mathf.FloorToInt(worldZ / chunkSize)];

        for (int x = 0; x < chunks.GetLength(0); x++)
        {
            for (int y = 0; y < chunks.GetLength(1); y++)
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

                }
            }
        }

    }

    // Update is called once per frame
    void Update()
    {

    }

    int PerlinNoise(int x, int y, int z, float scale, float height, float power)
    {
        float rValue;

        //rValue = Noise.GetValue(((float)x) / scale, ((float)y) / scale, ((float)z) / scale);
        //rValue = Noise.GetValue(((float)x), ((float)y), ((float)z));
        //rValue = BiasObj.GetValue(((float)x), ((float)y), ((float)z));
        rValue = GainObj.GetValue(((float)x/scale), ((float)y/50), ((float)z/scale));
        if (rValue<0)
        {
            rValue = -rValue;
        }
        rValue *= height;

        if (power != 0)
        {
            //rValue = Mathf.Pow(rValue, power);
        }

        return (int)rValue;
    }

    public byte Block(int x, int y, int z)
    {

        if (x >= worldX || x < 0 || y >= worldY || y < 0 || z >= worldZ || z < 0)
        {
            return (byte)1;
        }

        try
        {
            return data[x, y, z];
        }
        catch (System.Exception)
        {

            return 0;
        }
        
    }
}
