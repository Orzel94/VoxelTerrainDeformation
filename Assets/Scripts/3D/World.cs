﻿using System.Collections;
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
    public int worldX = 16;
    public int worldY = 16;
    public int worldZ = 16;

    public RidgeNoise Noise;

    public GameObject chunk;
    public GameObject[,,] chunks;
    public int chunkSize = 16;
    // Start is called before the first frame update
    void Start()
    {
        Noise = new RidgeNoise(1);
        Noise.Exponent = 2;
        Noise.Gain = 1.2f;
        Noise.Offset = 0.7f;

        data = new byte[worldX, worldY, worldZ];

        for (int x = 0; x < worldX; x++)
        {
            for (int z = 0; z < worldZ; z++)
            {
                int stone = PerlinNoise(x, 0, z, 10, 3, 1.2f);
                stone += PerlinNoise(x, 300, z, 20, 4, 0) + 10;
                int dirt = PerlinNoise(x, 100, z, 50, 2, 0) + 1; //Added +1 to make sure minimum grass height is 1

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

                }
            }
        }

        chunks = new GameObject[Mathf.FloorToInt(worldX / chunkSize),
                                Mathf.FloorToInt(worldY / chunkSize),
                                Mathf.FloorToInt(worldZ / chunkSize)];

        for (int x = 0; x < chunks.GetLength(0); x++)
        {
            for (int y = 0; y < chunks.GetLength(1); y++)
            {
                for (int z = 0; z < chunks.GetLength(2); z++)
                {

                    chunks[x, y, z] = Instantiate(chunk,
                     new Vector3(x * chunkSize, y * chunkSize, z * chunkSize),
                     new Quaternion(0, 0, 0, 0)) as GameObject;

                    Chunk newChunkScript = chunks[x, y, z].GetComponent("Chunk") as Chunk;

                    newChunkScript.worldGO = gameObject;
                    newChunkScript.chunkSize = chunkSize;
                    newChunkScript.chunkX = x * chunkSize;
                    newChunkScript.chunkY = y * chunkSize;
                    newChunkScript.chunkZ = z * chunkSize;

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

        rValue = Noise.GetValue(((float)x) / scale, ((float)y) / scale, ((float)z) / scale);
        //rValue = Noise.GetValue(((float)x), ((float)y), ((float)z));
        rValue *= height;

        if (power != 0)
        {
            rValue = Mathf.Pow(rValue, power);
        }

        return (int)rValue;
    }

    public byte Block(int x, int y, int z)
    {

        if (x >= worldX || x < 0 || y >= worldY || y < 0 || z >= worldZ || z < 0)
        {
            return (byte)1;
        }

        return data[x, y, z];
    }
}
