using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using CoherentNoise.Generation.Fractal;
using CoherentNoise.Generation.Modification;
using System.Diagnostics;
using Debug = UnityEngine.Debug;
using Unity.IL2CPP.CompilerServices;
using System.Linq;
using System.CodeDom;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Collections.Concurrent;

public enum VoxelTypeEnum
{
    AIR = 0,
    STONE = 1,
    GRASS = 2
}


[Il2CppSetOption(Option.ArrayBoundsChecks, false)]
public class Chunk : MonoBehaviour
{
    public Vector3 ChunkIndex { get; set; }

    private List<Vector3> newVertices = new List<Vector3>();
    private List<Vec3> newVerticesV2 = new List<Vec3>();
    private List<int> newTriangles = new List<int>();
    private List<Vector2> newUV = new List<Vector2>();

    private float tUnit = 0.25f;
    private Vector2 tStone = new Vector2(1, 0);
    private Vector2 tGrass = new Vector2(0, 1);

    private Mesh mesh;
    private MeshCollider col;

    private int faceCount;

    public GameObject worldGO;
    private World world;

    public int chunkSize;

    public int chunkX;
    public int chunkY;
    public int chunkZ;
    public float rBias;
    public float rGain;
    private Vector2 tGrassTop = new Vector2(2, 1);

    public bool update;

    public VoxelTypeEnum[,,] voxels;
    public float voxelScale;
    public bool terrainGenerationEnded;
    public bool meshUpdateNeeded;

    public RidgeNoise Noise;
    //public BillowNoise Noise;
    public Bias BiasObj;
    public Gain GainObj;

    public float exp;
    public float gain;
    public float offset;

    public int brushRadius;
    public int iterations;
    public bool deformInprogress;
    private bool chunkCreated = false;
    private Vector3[] vm;
    public class Vec3
    {
        public float x;
        public float y;
        public float z;
        public Vec3()
        {
        }
        public Vec3(float X, float Y, float Z)
        {
            x = X;
            y = Y;
            z = Z;
        }
    }
    private class VoxelMesh
    {
        public List<Vec3> vertices { get; set; }
        public List<int> triangles { get; set; }
        public List<Vector2> uvs { get; set; }
        public VoxelMesh()
        {
            vertices = new List<Vec3>();
            triangles = new List<int>();
            uvs = new List<Vector2>();
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1, 1, 0, 0.75F);
        for (int x = 0; x < (int)(chunkSize / world.voxelScale) + 1; x++)
        {
            for (int z = 0; z < (int)(chunkSize / world.voxelScale) + 1; z++)
            {
                for (int y = (int)((world.worldY / world.voxelScale) - 1); y > 0; y--)
                {

                    if (Block(x, y, z) != VoxelTypeEnum.AIR && Block(x, y + 1, z) == VoxelTypeEnum.AIR)
                    {
                        //Block above is air
                        Vector3 position = new Vector3(2 * chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, 2 * chunkZ + z * world.voxelScale);
                        Gizmos.DrawWireSphere(position, 0.1f);
                        break;
                    }
                }
            }
        }
    }



    internal void DeformGeometric(DefScript.Shape selectedShape, int size, double lnMultiplier, Vector3 position)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();


        float x = Mathf.RoundToInt(position.x / world.voxelScale) * world.voxelScale;
        float y = Mathf.RoundToInt(position.y / world.voxelScale) * world.voxelScale;
        float z = Mathf.RoundToInt(position.z / world.voxelScale) * world.voxelScale;

        int updateX = Mathf.FloorToInt(x / world.chunkSize);
        int updateY = Mathf.FloorToInt(y / world.chunkSize);
        int updateZ = Mathf.FloorToInt(z / world.chunkSize);

        print("Updating: " + updateX + ", " + updateY + ", " + updateZ);
        int voxXCenter = Mathf.FloorToInt((x - (updateX * world.chunkSize)) / world.voxelScale);
        int voxYCenter = Mathf.FloorToInt(y / world.voxelScale);
        int voxZCenter = Mathf.FloorToInt((z - (updateZ * world.chunkSize)) / world.voxelScale);
        //choosen voxel on terrain
        //int chunkX = Mathf.FloorToInt(x / world.chunkSize);
        //int chunkY = Mathf.FloorToInt(y / world.chunkSize);
        //int chunkZ = Mathf.FloorToInt(z / world.chunkSize);

        for (int voxX = voxXCenter - size; voxX < voxXCenter + size; voxX++)
        {
            for (int voxZ = voxZCenter - size; voxZ < voxZCenter + size; voxZ++)
            {
                if (voxX >= 0 && voxX < voxels.GetLength(0) && voxZ >= 0 && voxZ < voxels.GetLength(2))
                {
                    int xDist = Math.Abs(voxX - voxXCenter);
                    int zDist = Math.Abs(voxZ - voxZCenter);

                    if (selectedShape == DefScript.Shape.Circle)
                    {


                        double distance = Math.Sqrt((xDist * xDist) + (zDist * zDist));
                        //if (distance==0)
                        //{
                        //    continue;
                        //}
                        if (distance <= size)
                        {
                            try
                            {


                                double ylog = Math.Log(size - distance + 1) * lnMultiplier;
                                int targetHeight = voxYCenter + Mathf.RoundToInt((float)ylog);
                                for (int voxY = 0; voxY < (int)((world.worldY / world.voxelScale) - 1); voxY++)
                                {
                                    if (voxels[voxX, voxY, voxZ] == VoxelTypeEnum.AIR && voxY <= targetHeight)
                                    {
                                        voxels[voxX, voxY, voxZ] = VoxelTypeEnum.GRASS;
                                    }
                                }
                            }
                            catch (Exception ex)
                            {

                                throw;
                            }
                        }
                    }
                    else if (selectedShape == DefScript.Shape.Square)
                    {
                        int targetHeight = voxYCenter + Mathf.RoundToInt(((size - Math.Abs(xDist + zDist) / 2) / 2 + 1) * Convert.ToSingle(lnMultiplier));
                        for (int voxY = 0; voxY < (int)((world.worldY / world.voxelScale) - 1); voxY++)
                        {
                            if (voxels[voxX, voxY, voxZ] == VoxelTypeEnum.AIR && voxY <= targetHeight)
                            {
                                voxels[voxX, voxY, voxZ] = VoxelTypeEnum.GRASS;
                            }
                        }
                    }
                }
            }
        }
        stopwatch.Stop();
        world.logs.Enqueue($"Voxel deformation [ms]; {stopwatch.ElapsedMilliseconds};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale; {world.voxelScale}");
        Task.Factory.StartNew(() =>
        {
            deformInprogress = true;
            //UnityEngine.//Debug.Log($"deformed");
            draw();
            meshUpdateNeeded = true;
            //UnityEngine.//Debug.Log($"drawn");
        });

        //                    int maxX = (brushRadius + x) < (int)(chunkSize / world.voxelScale) ? (brushRadius + x) : (int)(chunkSize / world.voxelScale);
        //                    int maxY = (brushRadius + y) < (int)(world.worldY / world.voxelScale) ? (brushRadius + y) : (int)(world.worldY / world.voxelScale);
        //                    int maxZ = (brushRadius + z) < (int)(chunkSize / world.voxelScale) ? (brushRadius + z) : (int)(chunkSize / world.voxelScale);
        //                    int minX = (-brushRadius + x > 0) ? (-brushRadius + x) : 0;
        //                    int minY = (-brushRadius + y > 0) ? (-brushRadius + y) : 0;
        //                    int minZ = (-brushRadius + z > 0) ? (-brushRadius + z) : 0;
        //                    for (int l = minX; l < maxX; l++)
        //                    {
        //                        for (int j = minY; j < maxY; j++)
        //                        {
        //                            for (int k = minZ; k < maxZ; k++)
        //                            {
        //if (Math.Sqrt(double.Parse((xDiff * xDiff + yDiff * yDiff + zDiff * zDiff).ToString())) <= brushRadius)
    }

    // Start is called before the first frame update
    void Start()
    {
        //world.logs = new ConcurrentQueue<string>();
        deformInprogress = false;
        Noise = new RidgeNoise(1);

        //Noise = new BillowNoise(4);

        BiasObj = new Bias(Noise, rBias);
        GainObj = new Gain(BiasObj, rGain);
        terrainGenerationEnded = false;
        meshUpdateNeeded = true;
        mesh = GetComponent<MeshFilter>().mesh;
        col = GetComponent<MeshCollider>();
        world = worldGO.GetComponent("World") as World;
        Noise.Exponent = world.rnExp;// 1.0f;
        Noise.Gain = world.rnGain;// 1.2f;
        Noise.Offset = world.rnOffset;// 0.7f;

        chunkCreated = true;
        //Task.Factory.StartNew(() =>
        //{
        //    var stopwatch = new Stopwatch();
        //    stopwatch.Start();

        //    GenerateTerrain();
        //    terrainGenerationEnded = true;

        //    stopwatch.Stop();
        //    //UnityEngine.//Debug.Log($"Elapsed seconds: {stopwatch.ElapsedMilliseconds / 1000.0f};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale: {world.voxelScale}");

        //});



        //Task.WhenAll()
    }
    public Task GenAsync()
    {
        return Task.Factory.StartNew(() =>
        {
            while (true)
            {
                if (!chunkCreated)
                {
                    continue;
                }
                try
                {

                    var stopwatch = new Stopwatch();
                    stopwatch.Start();

                    GenerateTerrain();
                    terrainGenerationEnded = true;

                    stopwatch.Stop();
                    world.logs.Enqueue($"Elapsed seconds: {stopwatch.ElapsedMilliseconds};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale: {world.voxelScale}");
                }
                catch (Exception ex)
                {
                    throw;
                }
                break;
            }
        });
    }

    // Update is called once per frame
    void Update()
    {
        if (meshUpdateNeeded && terrainGenerationEnded)
        {
            UpdateMesh();
            meshUpdateNeeded = false;
            deformInprogress = false;
        }
    }

    void UpdateMesh()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        mesh.Clear();
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.vertices = newVertices.ToArray();
        var xxx = mesh.vertices[0];
        var xxx2 = newVertices[0];
        //Debug.Log($"vertices count {newVertices.Count}; list max size {newVertices.Capacity}; chunkX: {chunkX}  ChunkZ: {chunkZ}");
        //Debug.Log($"vertices count {mesh.vertexCount}; list max size--------; chunkX: {chunkX}  ChunkZ: {chunkZ}");
        mesh.uv = newUV.ToArray();
        mesh.triangles = newTriangles.ToArray();
        //Debug.Log($"111vertices count {mesh.vertexCount}; list max size--------; chunkX: {chunkX}  ChunkZ: {chunkZ}");
        mesh.Optimize();
        //Debug.Log($"222vertices count {mesh.vertexCount}; list max size--------; chunkX: {chunkX}  ChunkZ: {chunkZ}");
        mesh.RecalculateNormals();

        col.sharedMesh = null;
        col.sharedMesh = mesh;

        newVertices.Clear();
        newUV.Clear();
        newTriangles.Clear();
        //Debug.Log($"333vertices count {mesh.vertexCount}; list max size--------; chunkX: {chunkX}  ChunkZ: {chunkZ}");
        faceCount = 0;
        stopwatch.Stop();
        //world.logs.Enqueue($"render: {stopwatch.ElapsedMilliseconds};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale: {world.voxelScale}");
    }

    VoxelTypeEnum Block(int x, int y, int z, bool calculatedNeighbourValue = false)
    {
        try
        {
            return voxels[x, y, z];
        }
        catch (Exception ex)
        {
            if (calculatedNeighbourValue)
            {
                if (x < 0)
                {
                    return world.chunks[(int)ChunkIndex.x - 1, (int)ChunkIndex.y, (int)ChunkIndex.z].Block((int)(x + (chunkSize / voxelScale)), y, z, true);
                }
                else if (x >= chunkSize)
                {
                    return world.chunks[(int)ChunkIndex.x + 1, (int)ChunkIndex.y, (int)ChunkIndex.z].Block((int)(x - (chunkSize / voxelScale)), y, z, true);
                }
                else if (z < 0)
                {
                    return world.chunks[(int)ChunkIndex.x, (int)ChunkIndex.y, (int)ChunkIndex.z - 1].Block(x, y, (int)(z + (chunkSize / voxelScale)), true);
                }
                else if (z >= chunkSize)
                {
                    return world.chunks[(int)ChunkIndex.x, (int)ChunkIndex.y, (int)ChunkIndex.z + 1].Block(x, y, (int)(z - (chunkSize / voxelScale)), true);
                }
                //int stone = PerlinNoise(2 * chunkX + x * world.voxelScale, 0, 2 * chunkZ + z * world.voxelScale, 200, (int)(world.worldY / world.voxelScale) /** world.worldYMultiplier*/, 4.2f);// + (int)(world.worldY / world.voxelScale);
                //int dirt = PerlinNoise(2 * chunkX + x * world.voxelScale, 100, 2 * chunkZ + z * world.voxelScale, 200, world.worldY, 0) + 1;// + (int)(world.worldY / world.voxelScale); //Added +1 to make sure minimum grass height is 1

                //if (y <= stone)
                //{
                //    return VoxelTypeEnum.STONE;
                //}
                //else if (y <= dirt + stone)
                //{
                //    return VoxelTypeEnum.GRASS;
                //}
                //else if (y == 0)
                //{
                //    return VoxelTypeEnum.STONE;
                //}
                //else
                //{
                //    return VoxelTypeEnum.AIR;
                //}

            }
            return VoxelTypeEnum.AIR;
        }

        //return world.Block(x + chunkX, y + chunkY, z + chunkZ); // Don't replace the world.Block in this line!
    }

    public void GenerateTerrain()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        voxels = new VoxelTypeEnum[(int)(chunkSize / world.voxelScale) + 1, (int)(world.worldY / world.voxelScale), /** worldYMultiplier,*/ (int)(chunkSize / world.voxelScale) + 1];
        for (int x = 0; x < (int)(chunkSize / world.voxelScale) + 1; x++)
        {
            for (int z = 0; z < (int)(chunkSize / world.voxelScale) + 1; z++)
            {
                int stone = PerlinNoise(2 * chunkX + x * world.voxelScale, 0, 2 * chunkZ + z * world.voxelScale, 200, (int)(world.worldY / world.voxelScale) /** world.worldYMultiplier*/, 4.2f);// + (int)(world.worldY / world.voxelScale);
                //stone += PerlinNoise(x, 300, z, 20, 4, 1.5f) + 10;
                int dirt = PerlinNoise(2 * chunkX + x * world.voxelScale, 100, 2 * chunkZ + z * world.voxelScale, 200, world.worldY, 0) + 1;// + (int)(world.worldY / world.voxelScale); //Added +1 to make sure minimum grass height is 1
                ////Debug.Log($"stone: {stone} , x: {x}, z: {z}");
                //for (int y = (int)(worldY / voxelScale); y < (int)(worldY / voxelScale) * worldYMultiplier; y++)
                for (int y = 0; y < (int)(world.worldY / world.voxelScale); y++)
                {
                    try
                    {
                        if (y == 2)
                        {
                            voxels[x, y, z] = VoxelTypeEnum.AIR;
                            continue;
                        }

                        if (y <= stone)
                        {
                            voxels[x, y, z] = VoxelTypeEnum.STONE;
                        }
                        else if (y <= dirt + stone)
                        {
                            voxels[x, y, z] = VoxelTypeEnum.GRASS;
                        }
                        else if (y == 0)
                        {
                            voxels[x, y, z] = VoxelTypeEnum.STONE;
                        }
                        else
                        {
                            voxels[x, y, z] = VoxelTypeEnum.AIR;
                        }

                    }
                    catch (System.Exception ex)
                    {

                        throw;
                    }

                }
            }
        }
        stopwatch.Stop();
        world.logs.Enqueue($"|| Chunk[{ChunkIndex.x},{ChunkIndex.y},{ChunkIndex.z}]; terrain generation [ms]; {stopwatch.ElapsedMilliseconds}; voxel count; {voxels.GetLength(0) * voxels.GetLength(1) * voxels.GetLength(2)}; Chunk size; {chunkSize}; voxel scale; {world.voxelScale};");

        // world.logs.Enqueue($"terrain generation: {stopwatch.ElapsedMilliseconds};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale: {world.voxelScale}");
        //Task.Factory.StartNew(() =>
        //{
        draw();
        //GenerateMesh();
        //    terrainGenerationEnded = true;
        //});

    }

    public void GenerateMesh()
    {
        try
        {
            for (int x = 0; x < chunkSize / world.voxelScale + 1; x++)
            {
                for (int y = 0; y < world.worldY / world.voxelScale; y++)
                {
                    for (int z = 0; z < chunkSize / world.voxelScale + 1; z++)
                    {
                        VoxelTypeEnum currentVoxel = Block(x, y, z);

                        if (currentVoxel != VoxelTypeEnum.STONE)
                        {
                            break;
                        }
                        //This code will run for every block in the chunk
                        if (Block(x, y, z) != VoxelTypeEnum.AIR)
                        {
                            //If the block is solid

                            if (Block(x, y + 1, z) == VoxelTypeEnum.AIR)
                            {
                                //Block above is air
                                CubeTop(chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, chunkZ + z * world.voxelScale, currentVoxel);
                            }

                            if (Block(x, y - 1, z) == VoxelTypeEnum.AIR)
                            {
                                //Block below is air
                                CubeBot(chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, chunkZ + z * world.voxelScale, currentVoxel);

                            }

                            if (Block(x + 1, y, z) == VoxelTypeEnum.AIR)
                            {
                                //Block east is air
                                CubeEast(chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, chunkZ + z * world.voxelScale, currentVoxel);

                            }

                            if (Block(x - 1, y, z) == VoxelTypeEnum.AIR)
                            {
                                //Block west is air
                                CubeWest(chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, chunkZ + z * world.voxelScale, currentVoxel);

                            }

                            if (Block(x, y, z + 1) == VoxelTypeEnum.AIR)
                            {
                                //Block north is air
                                CubeNorth(chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, chunkZ + z * world.voxelScale, currentVoxel);

                            }

                            if (Block(x, y, z - 1) == VoxelTypeEnum.AIR)
                            {
                                //Block south is air
                                CubeSouth(chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, chunkZ + z * world.voxelScale, currentVoxel);

                            }

                        }

                    }
                }
            }

        }
        catch (System.Exception ex)
        {

            throw;
        }

        //UpdateMesh();
    }

    void Cube(Vector2 texturePos)
    {

        newTriangles.Add(faceCount * 4); //1
        newTriangles.Add(faceCount * 4 + 1); //2
        newTriangles.Add(faceCount * 4 + 2); //3
        newTriangles.Add(faceCount * 4); //1
        newTriangles.Add(faceCount * 4 + 2); //3
        newTriangles.Add(faceCount * 4 + 3); //4

        newUV.Add(new Vector2(tUnit * texturePos.x + tUnit, tUnit * texturePos.y));
        newUV.Add(new Vector2(tUnit * texturePos.x + tUnit, tUnit * texturePos.y + tUnit));
        newUV.Add(new Vector2(tUnit * texturePos.x, tUnit * texturePos.y + tUnit));
        newUV.Add(new Vector2(tUnit * texturePos.x, tUnit * texturePos.y));

        //faceCount++; // Add this line
    }

    void CubeTop(float x, float y, float z, VoxelTypeEnum block)
    {
        newVertices.Add(new Vector3(x, y, z + voxelScale));
        newVertices.Add(new Vector3(x + voxelScale, y, z + voxelScale));
        newVertices.Add(new Vector3(x + voxelScale, y, z));
        newVertices.Add(new Vector3(x, y, z));

        Vector2 texturePos = new Vector2(0, 0);

        if (block == VoxelTypeEnum.STONE)
        {
            texturePos = tStone;
        }
        else if (block == VoxelTypeEnum.GRASS)
        {
            texturePos = tGrassTop;
        }

        Cube(texturePos);
    }

    void CubeNorth(float x, float y, float z, VoxelTypeEnum block)
    {
        newVertices.Add(new Vector3(x + voxelScale, y - voxelScale, z + voxelScale));
        newVertices.Add(new Vector3(x + voxelScale, y, z + voxelScale));
        newVertices.Add(new Vector3(x, y, z + voxelScale));
        newVertices.Add(new Vector3(x, y - voxelScale, z + voxelScale));

        Vector2 texturePos = new Vector2(0, 0);

        if (block == VoxelTypeEnum.STONE)
        {
            texturePos = tStone;
        }
        else if (block == VoxelTypeEnum.GRASS)
        {
            texturePos = tGrassTop;
        }

        Cube(texturePos);
    }

    void CubeEast(float x, float y, float z, VoxelTypeEnum block)
    {
        newVertices.Add(new Vector3(x + voxelScale, y - voxelScale, z));
        newVertices.Add(new Vector3(x + voxelScale, y, z));
        newVertices.Add(new Vector3(x + voxelScale, y, z + voxelScale));
        newVertices.Add(new Vector3(x + voxelScale, y - voxelScale, z + voxelScale));

        Vector2 texturePos = new Vector2(0, 0);

        if (block == VoxelTypeEnum.STONE)
        {
            texturePos = tStone;
        }
        else if (block == VoxelTypeEnum.GRASS)
        {
            texturePos = tGrassTop;
        }

        Cube(texturePos);
    }

    void CubeSouth(float x, float y, float z, VoxelTypeEnum block)
    {
        newVertices.Add(new Vector3(x, y - voxelScale, z));
        newVertices.Add(new Vector3(x, y, z));
        newVertices.Add(new Vector3(x + voxelScale, y, z));
        newVertices.Add(new Vector3(x + voxelScale, y - voxelScale, z));

        Vector2 texturePos = new Vector2(0, 0);

        if (block == VoxelTypeEnum.STONE)
        {
            texturePos = tStone;
        }
        else if (block == VoxelTypeEnum.GRASS)
        {
            texturePos = tGrassTop;
        }

        Cube(texturePos);
    }

    void CubeWest(float x, float y, float z, VoxelTypeEnum block)
    {
        newVertices.Add(new Vector3(x, y - voxelScale, z + voxelScale));
        newVertices.Add(new Vector3(x, y, z + voxelScale));
        newVertices.Add(new Vector3(x, y, z));
        newVertices.Add(new Vector3(x, y - voxelScale, z));

        Vector2 texturePos = new Vector2(0, 0);

        if (block == VoxelTypeEnum.STONE)
        {
            texturePos = tStone;
        }
        else if (block == VoxelTypeEnum.GRASS)
        {
            texturePos = tGrassTop;
        }

        Cube(texturePos);
    }

    void CubeBot(float x, float y, float z, VoxelTypeEnum block)
    {
        newVertices.Add(new Vector3(x, y - voxelScale, z));
        newVertices.Add(new Vector3(x + voxelScale, y - voxelScale, z));
        newVertices.Add(new Vector3(x + voxelScale, y - voxelScale, z + voxelScale));
        newVertices.Add(new Vector3(x, y - voxelScale, z + voxelScale));

        Vector2 texturePos;

        texturePos = tStone;

        Cube(texturePos);
    }


    public int PerlinNoise(float x, float y, float z, float scale, float height, float power)
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


    public void draw()
    {
        newVerticesV2.Clear();
        newTriangles.Clear();
        newUV.Clear();

        var stopwatch = new Stopwatch();

        stopwatch.Start();
        try
        {
            int zB = 0;
            if (chunkZ / (world.chunkSize / 2) == 0)
            {
                zB--;
            }
            int addZ = 0;
            if (chunkZ / (world.chunkSize / 2) == world.chunks.GetLength(2) - 1)
            {
                addZ++;
            }
            //VoxelMesh[,,] voxelMesh = new VoxelMesh[(int)(chunkSize / world.voxelScale), (int)(world.worldY / world.voxelScale), (int)(chunkSize / world.voxelScale)];
            for (int z = zB; z < chunkSize / world.voxelScale + addZ; z++)
            {
                int xB = 0;
                if (chunkX / (world.chunkSize / 2) == 0)
                {
                    xB--;
                }
                int addX = 0;
                if (chunkX / (world.chunkSize / 2) == world.chunks.GetLength(0) - 1)
                {
                    addX++;
                }
                for (int x = xB; x < chunkSize / world.voxelScale + addX; x++)
                {
                    for (int y = 0; y < world.worldY / world.voxelScale - 1; y++)
                    {

                        byte lookup = 0;
                        //foo2.AddRange(voxels as List<VoxelTypeEnum>);
                        // 7 -- x + y*this->size_y + z * this->size_y * this->size_z
                        if (Block(x, y, z) == VoxelTypeEnum.AIR) lookup |= 128;
                        // 6 -- (x + 1) + y*this->size_y + z * this->size_y * this->size_z
                        if (Block(x + 1, y, z) == VoxelTypeEnum.AIR) lookup |= 64;
                        // 2 -- (x + 1) + (y + 1)*this->size_y + z * this->size_y * this->size_z
                        if (Block(x + 1, y + 1, z) == VoxelTypeEnum.AIR) lookup |= 4;
                        // 3 -- x + (y + 1)*this->size_y + z * this->size_y * this->size_z
                        if (Block(x, y + 1, z) == VoxelTypeEnum.AIR) lookup |= 8;
                        // 4 -- x + y*this->size_y + (z + 1) * this->size_y * this->size_z
                        if (Block(x, y, z + 1) == VoxelTypeEnum.AIR) lookup |= 16;
                        // 5 -- (x + 1) + y*this->size_y + (z + 1) * this->size_y * this->size_z
                        if (Block(x + 1, y, z + 1) == VoxelTypeEnum.AIR) lookup |= 32;
                        // 1 -- (x + 1) + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                        if (Block(x + 1, y + 1, z + 1) == VoxelTypeEnum.AIR) lookup |= 2;
                        // 0 -- x + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                        if (Block(x, y + 1, z + 1) == VoxelTypeEnum.AIR) lookup |= 1;
                        int tmp = ~lookup;
                        lookup = (byte)tmp;
                        /* hvis ikke alle punktene er utenfor eller innenfor, sĺ vil vi se nćrmere pĺ ting */
                        if ((lookup != 0) && (lookup != 255))
                        {
                            Vec3[] verts = new Vec3[12];
                            Vec3 position = new Vec3(chunkX + x * world.voxelScale, chunkY + y * world.voxelScale, chunkZ + z * world.voxelScale);

                            // 0 - 1
                            if ((edgeTable[lookup] & 1) != 0)
                                // x + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                                // (x + 1) + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                                verts[0] = Interpolate(position.x, position.y + voxelScale, position.z + voxelScale,
                                                    position.x + voxelScale, position.y + voxelScale, position.z + voxelScale);

                            // 1 - 2
                            if ((edgeTable[lookup] & 2) != 0)
                                // (x + 1) + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                                // (x + 1) + (y + 1)*this->size_y + z * this->size_y * this->size_z
                                verts[1] = Interpolate(position.x + voxelScale, position.y + voxelScale, position.z + voxelScale,
                                                    position.x + voxelScale, position.y + voxelScale, position.z);

                            // 2 - 3
                            if ((edgeTable[lookup] & 4) != 0)
                                // (x + 1) + (y + 1)*this->size_y + z * this->size_y * this->size_z
                                // x + (y + 1)*this->size_y + z * this->size_y * this->size_z
                                verts[2] = Interpolate(position.x + voxelScale, position.y + voxelScale, position.z,
                                                    position.x, position.y + voxelScale, position.z);

                            // 3 - 0
                            if ((edgeTable[lookup] & 8) != 0)
                                // x + (y + 1)*this->size_y + z * this->size_y * this->size_z
                                // x + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                                verts[3] = Interpolate(position.x, position.y + voxelScale, position.z,
                                                    position.x, position.y + voxelScale, position.z + voxelScale);

                            // 4 - 5
                            if ((edgeTable[lookup] & 16) != 0)
                                // x + y*this->size_y + (z + 1) * this->size_y * this->size_z
                                // (x + 1) + y*this->size_y + (z + 1) * this->size_y * this->size_z
                                verts[4] = Interpolate(position.x, position.y, position.z + voxelScale,
                                                    position.x + voxelScale, position.y, position.z + voxelScale);

                            // 5 - 6
                            if ((edgeTable[lookup] & 32) != 0)
                                // (x + 1) + y*this->size_y + (z + 1) * this->size_y * this->size_z
                                // (x + 1) + y*this->size_y + z * this->size_y * this->size_z
                                verts[5] = Interpolate(position.x + voxelScale, position.y, position.z + voxelScale,
                                                    position.x + voxelScale, position.y, position.z);

                            // 6 - 7
                            if ((edgeTable[lookup] & 64) != 0)
                                // (x + 1) + y*this->size_y + z * this->size_y * this->size_z
                                // x + y*this->size_y + z * this->size_y * this->size_z
                                verts[6] = Interpolate(position.x + voxelScale, position.y, position.z,
                                                    position.x, position.y, position.z);

                            // 7 - 4
                            if ((edgeTable[lookup] & 128) != 0)
                                // x + y*this->size_y + z * this->size_y * this->size_z
                                // x + y*this->size_y + (z + 1) * this->size_y * this->size_z
                                verts[7] = Interpolate(position.x, position.y, position.z,
                                                    position.x, position.y, position.z + voxelScale);

                            // 0 - 4
                            if ((edgeTable[lookup] & 256) != 0)
                                // x + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                                // x + y*this->size_y + (z + 1) * this->size_y * this->size_z
                                verts[8] = Interpolate(position.x, position.y + voxelScale, position.z + voxelScale,
                                                    position.x, position.y, position.z + voxelScale);

                            // 1 - 5
                            if ((edgeTable[lookup] & 512) != 0)
                                // (x + 1) + (y + 1)*this->size_y + (z + 1) * this->size_y * this->size_z
                                // (x + 1) + y*this->size_y + (z + 1) * this->size_y * this->size_z
                                verts[9] = Interpolate(position.x + voxelScale, position.y + voxelScale, position.z + voxelScale,
                                                    position.x + voxelScale, position.y, position.z + voxelScale);

                            // 2 - 6
                            if ((edgeTable[lookup] & 1024) != 0)
                                // (x + 1) + (y + 1)*this->size_y + z * this->size_y * this->size_z
                                // (x + 1) + y*this->size_y + z * this->size_y * this->size_z
                                verts[10] = Interpolate(position.x + voxelScale, position.y + voxelScale, position.z,
                                                    position.x + voxelScale, position.y, position.z);

                            // 3 - 7
                            if ((edgeTable[lookup] & 2048) != 0)
                                // x + (y + 1)*this->size_y + z * this->size_y * this->size_z
                                // x + y*this->size_y + z * this->size_y * this->size_z
                                verts[11] = Interpolate(position.x, position.y + voxelScale, position.z,
                                                    position.x, position.y, position.z);

                            /* alle punktene vĺre skal ha full fargeverdi */
                            //glColor3f(1.0f, 1.0f, 1.0f);

                            /* looper igjennom entryene i triTable og velger ut de punktene vi skal tegne trianglene mellom, punkt for punkt */
                            VoxelMesh voxel = new VoxelMesh();
                            int i, j;
                            try
                            {
                                //int[,] vertDic = new int[16, 2];
                                //Dictionary<int, int> vertDic = new Dictionary<int, int>();
                                for (i = 0; triTable[lookup, i] != -1; i += 3)
                                {


                                    for (j = i; j < (i + 3); j++)
                                    {
                                        try
                                        {
                                            int vertIndex = triTable[lookup, j];
                                            int existingIndex = VertexIndexOf(newVerticesV2, verts[vertIndex]);
                                            //int jVal;
                                            //bool exist = vertDic.TryGetValue(vertIndex, out jVal);
                                            if (existingIndex != -1)
                                            {
                                                ///////
                                                //voxel.triangles.Add(faceCount + jVal);
                                                //////////
                                                newTriangles.Add(existingIndex);
                                            }
                                            else
                                            {
                                                Vector2 texturePos = tGrass;
                                                ////////
                                                //voxel.triangles.Add(faceCount + vertDic.Count);
                                                //voxel.vertices.Add(verts[vertIndex]);
                                                //voxel.uvs.Add(new Vector2(tUnit * texturePos.x + tUnit, tUnit * texturePos.y));
                                                ////////

                                                newTriangles.Add(newVerticesV2.Count);
                                                //vertDic.Add(vertIndex, vertDic.Count);



                                                newVerticesV2.Add(verts[vertIndex]);

                                                newUV.Add(new Vector2(tUnit * texturePos.x + tUnit, tUnit * texturePos.y));
                                            }
                                        }
                                        catch (System.Exception ex)
                                        {

                                            throw;
                                        }
                                    }

                                }
                                //faceCount += vertDic.Count;
                                //voxelMesh[x, y, z] = voxel;
                            }
                            catch (System.Exception ex)
                            {

                                throw;
                            }
                        }

                        /* gjřr lookup-tabellen klar til neste runde */
                    }
                }
            }
            Vector2 texturePos2 = tGrass;
            try
            {
                if (newUV.Count != newVerticesV2.Count)
                {
                    for (int i = newUV.Count; i < newVerticesV2.Count; i++)
                    {
                        newUV.Add(new Vector2(tUnit * texturePos2.x + tUnit, tUnit * texturePos2.y));
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }

            stopwatch.Stop();
            world.logs.Enqueue($"|| Chunk[{ChunkIndex.x},{ChunkIndex.y},{ChunkIndex.z}]; Marching Cubes [ms]; {stopwatch.ElapsedMilliseconds}; voxel count; {voxels.GetLength(0) * voxels.GetLength(1) * voxels.GetLength(2)};Chunk size; {chunkSize}; vertices count; {newVerticesV2.Count}; triangles count; {newTriangles.Count / 3};");

            stopwatch = new Stopwatch();
            stopwatch.Start();
            //for (int c = 0; c < 5; c++)
            //{


            //    for (int i = 0; i < newVerticesV2.Count; i++)
            //    {
            //        float avgAdjX = 0;
            //        float avgAdjY = 0;
            //        float avgAdjZ = 0;
            //        var x = FindAdjacentVertices(i);
            //        foreach (var item in x)
            //        {
            //            try
            //            {
            //                var vert = newVerticesV2[newTriangles[item]];
            //                avgAdjX += vert.x;
            //                avgAdjY += vert.y;
            //                avgAdjZ += vert.z;
            //            }
            //            catch (Exception ex)
            //            {

            //                throw;
            //            }

            //        }
            //        avgAdjX = avgAdjX / x.Count;
            //        avgAdjY = avgAdjY / x.Count;
            //        avgAdjZ = avgAdjZ / x.Count;
            //        var targetVertex = newVerticesV2[i];
            //        targetVertex.x = (targetVertex.x / 2.0f) + (avgAdjX / 2.0f);
            //        targetVertex.y = (targetVertex.y / 2.0f) + (avgAdjY / 2.0f);
            //        targetVertex.z = (targetVertex.z / 2.0f) + (avgAdjZ / 2.0f);
            //    }
            //}
            foreach (var item in newVerticesV2)
            {
                newVertices.Add(new Vector3(item.x, item.y, item.z));
            }
            //var vs = newVertices.ToArray();
            //vm = newVertices.ToArray();
            //var tr = newTriangles.ToArray();
            //for (int i = 0; i < 5; i++)
            //    vm = SmoothFilter.laplacianFilter(vm, tr);
            //vm = SmoothFilter.hcFilter(vs, vm, tr, 0.0f, 0.5f);



            //world.logs.Enqueue($"Smoothing; {stopwatch.ElapsedMilliseconds};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale: {world.voxelScale}");

            //SmoothDataSet(ref vm, /*this.brushRadius*/5, /*this.iterations*/1);
            for (int t = 0; t < 10; t++)
            {


                int ddd = 0;
                for (int i = 0; i < newVertices.Count; i++)
                {
                    var vert = newVertices[i];
                    if (vert.x == chunkX + chunkSize || vert.x == chunkX || vert.z == chunkZ + chunkSize || vert.z == chunkZ)
                    {
                        ddd++;
                    }
                    else
                    {
                        var adjacent = FindAdjacentVertices(i);
                        float avgAdjY = 0.0f;
                        float avgAdjX = 0.0f;
                        float avgAdjZ = 0.0f;

                        foreach (var item in adjacent)
                        {
                            try
                            {
                                var adjVert = newVerticesV2[newTriangles[item]];
                                avgAdjY += adjVert.y;
                                avgAdjX += adjVert.x;
                                avgAdjZ += adjVert.z;
                            }
                            catch (Exception ex)
                            {

                                throw;
                            }

                        }
                        avgAdjY = avgAdjY / adjacent.Count;
                        //newVertices[i].y = (vert.y / 2.0f) + (avgAdjY / 2.0f);
                        avgAdjX = avgAdjX / adjacent.Count;
                        //newVertices[i].x = (vert.x / 2.0f) + (avgAdjX / 2.0f);
                        avgAdjZ = avgAdjZ / adjacent.Count;
                        //newVertices[i].z = (vert.z / 2.0f) + (avgAdjZ / 2.0f);
                        newVertices[i] = new Vector3((vert.x / 2.0f) + (avgAdjX / 2.0f), (vert.y / 2.0f) + (avgAdjY / 2.0f), (vert.z / 2.0f) + (avgAdjZ / 2.0f));
                    }

                }
            }
            stopwatch.Stop();
            world.logs.Enqueue($"|| Chunk[{ChunkIndex.x},{ChunkIndex.y},{ChunkIndex.z}]; Laplatian smoothing[ms]; {stopwatch.ElapsedMilliseconds}; voxel count; {voxels.GetLength(0) * voxels.GetLength(1) * voxels.GetLength(2)} ;Chunk size; {chunkSize};vertices count; {newVerticesV2.Count}; triangles count; {newTriangles.Count / 3} ;");

            //////////////////////////////////////////////
            //int brushRadius = 3;
            //int iterations = 2;
            //for (int i = 0; i < iterations; i++)
            //{
            //    for (int z = 0; z < chunkSize / world.voxelScale - 1; z++)
            //    {
            //        for (int x = 0; x < chunkSize / world.voxelScale - 1; x++)
            //        {
            //            for (int y = 0; y < world.worldY / world.voxelScale - 1; y++)
            //            {
            //                if (voxelMesh[x, y, z] != null)
            //                {
            //                    var vox = voxelMesh[x, y, z];
            //                    //SmoothVoxel(ref voxelMesh, ref vox, x, y, z, brushRadius);
            //                    //////////////////////////////////////////////
            //                    HashSet<Vec3> distinctVertices = new HashSet<Vec3>();
            //                    int maxX = (brushRadius + x) < (int)(chunkSize / world.voxelScale) ? (brushRadius + x) : (int)(chunkSize / world.voxelScale);
            //                    int maxY = (brushRadius + y) < (int)(world.worldY / world.voxelScale) ? (brushRadius + y) : (int)(world.worldY / world.voxelScale);
            //                    int maxZ = (brushRadius + z) < (int)(chunkSize / world.voxelScale) ? (brushRadius + z) : (int)(chunkSize / world.voxelScale);
            //                    int minX = (-brushRadius + x > 0) ? (-brushRadius + x) : 0;
            //                    int minY = (-brushRadius + y > 0) ? (-brushRadius + y) : 0;
            //                    int minZ = (-brushRadius + z > 0) ? (-brushRadius + z) : 0;
            //                    for (int l = minX; l < maxX; l++)
            //                    {
            //                        for (int j = minY; j < maxY; j++)
            //                        {
            //                            for (int k = minZ; k < maxZ; k++)
            //                            {
            //                                int xDiff = x - l;
            //                                int yDiff = y - j;
            //                                int zDiff = z - k;
            //                                if (Math.Sqrt(double.Parse((xDiff * xDiff + yDiff * yDiff + zDiff * zDiff).ToString())) <= brushRadius)
            //                                {

            //                                    if (/*i >= 0 && j >= 0 && k >= 0 && i < (int)(chunkSize / world.voxelScale) && j < (int)(world.worldY / world.voxelScale) && k < (int)(chunkSize / world.voxelScale) &&*/ voxelMesh[l, j, k] != null/* && Math.Sqrt(i * i + j * j + k * k) <= brushRadius*/)
            //                                    {
            //                                        for (int c = 0; c < voxelMesh[l, j, k].vertices.Count; c++)
            //                                        {
            //                                            distinctVertices.Add(voxelMesh[l, j, k].vertices[c]);
            //                                        }
            //                                    }
            //                                }

            //                            }
            //                        }
            //                    }
            //                    Vector3 avgVert = new Vector3();
            //                    if (distinctVertices.Count != 0)
            //                    {
            //                        foreach (var item in distinctVertices)
            //                        {
            //                            avgVert.x += item.x;
            //                            avgVert.y += item.y;
            //                            avgVert.z += item.z;
            //                        }
            //                        avgVert.x = (1 / (2.0f * distinctVertices.Count)) * avgVert.x;
            //                        avgVert.y = (1 / (2.0f * distinctVertices.Count)) * avgVert.y;
            //                        avgVert.z = (1 / (2.0f * distinctVertices.Count)) * avgVert.z;
            //                    }

            //                    for (int m = 0; m < voxelMesh[x, y, z].vertices.Count; m++)
            //                    {
            //                        voxelMesh[x, y, z].vertices[m].x = 0.5f * voxelMesh[x, y, z].vertices[m].x + avgVert.x;
            //                        voxelMesh[x, y, z].vertices[m].y = 0.5f * voxelMesh[x, y, z].vertices[m].y + avgVert.y;
            //                        voxelMesh[x, y, z].vertices[m].z = 0.5f * voxelMesh[x, y, z].vertices[m].z + avgVert.z;
            //                    }
            //                    //////////////////////////////////////////////
            //                    var vox2 = voxelMesh[x, y, z];
            //                }
            //            }
            //        }
            //    }
            //}

            //////////////////////////////////////////////
            //foreach (var item in voxelMesh)
            //{
            //for (int z = 0; z < chunkSize / world.voxelScale - 1; z++)
            //{
            //    for (int x = 0; x < chunkSize / world.voxelScale - 1; x++)
            //    {
            //        for (int y = 0; y < world.worldY / world.voxelScale - 1; y++)
            //        {
            //            if (voxelMesh[x, y, z] != null)
            //            {
            //                for (int i = 0; i < voxelMesh[x, y, z].triangles.Count; i++)
            //                {
            //                    newTriangles.Add(voxelMesh[x, y, z].triangles[i]);
            //                }
            //                for (int i = 0; i < voxelMesh[x, y, z].vertices.Count; i++)
            //                {
            //                    newVertices.Add(new Vector3(voxelMesh[x, y, z].vertices[i].x, voxelMesh[x, y, z].vertices[i].y, voxelMesh[x, y, z].vertices[i].z));
            //                }
            //                for (int i = 0; i < voxelMesh[x, y, z].uvs.Count; i++)
            //                {
            //                    newUV.Add(voxelMesh[x, y, z].uvs[i]);
            //                }
            //                //newTriangles.AddRange(item.triangles);
            //                //newVertices.AddRange(item.vertices);
            //                //newUV.AddRange(item.uvs);
            //            }

            //        }
            //    }
            //}
        }
        catch (System.Exception ex)
        {

            throw;
        }
    }
    public int VertexIndexOf(List<Vec3> vertices, Vec3 searchedVertex)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            var vertex = vertices[i];
            if (vertex.x == searchedVertex.x && vertex.y == searchedVertex.y && vertex.z == searchedVertex.z)
            {
                return i;
            }
        }
        return -1;
    }
    public Vec3 Interpolate(float x, float y, float z, float x2, float y2, float z2)
    {
        Vec3 res = new Vec3();
        res.x = (x + x2) / 2;
        res.y = (y + y2) / 2;
        res.z = (z + z2) / 2;
        return res;
    }

    public List<int> FindAdjacentVertices(int vertexIndex)
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        List<int> adjVertIndexes = new List<int>();
        try
        {



            List<int> v = newTriangles.Select((b, i) => b == vertexIndex ? i : -1).Where(i => i != -1).ToList();
            foreach (var item in v)
            {
                var x = item % 3;
                if (x == 0)
                {
                    adjVertIndexes.Add(item + 1);
                    adjVertIndexes.Add(item + 2);
                }
                else if (x == 1)
                {
                    adjVertIndexes.Add(item - 1);
                    adjVertIndexes.Add(item + 1);
                }
                else
                {
                    adjVertIndexes.Add(item - 1);
                    adjVertIndexes.Add(item - 2);
                }
            }
        }
        catch (Exception ex)
        {

            throw;
        }
        stopwatch.Stop();
        //world.logs.Enqueue($"|| Chunk[{ChunkIndex.x},{ChunkIndex.y},{ChunkIndex.z}]; Laplatian smoothing; {stopwatch.ElapsedMilliseconds} ms; voxel count: {voxels.GetLength(0) * voxels.GetLength(1) * voxels.GetLength(2)} ; vertices count: {newVerticesV2.Count} ; triangles count: {newTriangles.Count / 3} ;");

        //world.logs.Enqueue($"finding adjacent vertices; {stopwatch.ElapsedMilliseconds};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale: {world.voxelScale}");

        return adjVertIndexes;
    }

    private void SmoothDataSet(ref VoxelMesh[,,] voxelMesh, int brushRadius, int iterations)
    {
        for (int i = 0; i < iterations; i++)
        {
            for (int z = 0; z < chunkSize / world.voxelScale - 1; z++)
            {
                for (int x = 0; x < chunkSize / world.voxelScale - 1; x++)
                {
                    for (int y = 0; y < world.worldY / world.voxelScale - 1; y++)
                    {
                        if (voxelMesh[x, y, z] != null)
                        {
                            var vox = voxelMesh[x, y, z];
                            SmoothVoxel(ref voxelMesh, ref vox, x, y, z, brushRadius);
                            //////////////////////////////////////////////
                            //HashSet<Vec3> distinctVertices = new HashSet<Vec3>();
                            //int maxX = (brushRadius + x) < (int)(chunkSize / world.voxelScale) ? (brushRadius + x) : (int)(chunkSize / world.voxelScale);
                            //int maxY = (brushRadius + y) < (int)(world.worldY / world.voxelScale) ? (brushRadius + y) : (int)(world.worldY / world.voxelScale);
                            //int maxZ = (brushRadius + z) < (int)(chunkSize / world.voxelScale) ? (brushRadius + z) : (int)(chunkSize / world.voxelScale);
                            //int minX = (-brushRadius + x > 0) ? (-brushRadius + x) : 0;
                            //int minY = (-brushRadius + y > 0) ? (-brushRadius + y) : 0;
                            //int minZ = (-brushRadius + z > 0) ? (-brushRadius + z) : 0;
                            //for (int l = minX; l < maxX; l++)
                            //{
                            //    for (int j = minY; j < maxY; j++)
                            //    {
                            //        for (int k = minZ; k < maxZ; k++)
                            //        {
                            //            int xDiff = x - l;
                            //            int yDiff = y - j;
                            //            int zDiff = z - k;
                            //            if (Math.Sqrt(double.Parse((xDiff * xDiff + yDiff * yDiff + zDiff * zDiff).ToString())) <= brushRadius)
                            //            {

                            //                if (/*i >= 0 && j >= 0 && k >= 0 && i < (int)(chunkSize / world.voxelScale) && j < (int)(world.worldY / world.voxelScale) && k < (int)(chunkSize / world.voxelScale) &&*/ voxelMesh[l, j, k] != null/* && Math.Sqrt(i * i + j * j + k * k) <= brushRadius*/)
                            //                {
                            //                    for (int c = 0; c < voxelMesh[l, j, k].vertices.Count; c++)
                            //                    {
                            //                        distinctVertices.Add(voxelMesh[l, j, k].vertices[c]);
                            //                    }
                            //                }



                            //            }

                            //        }
                            //    }
                            //}
                            //Vector3 avgVert = new Vector3();
                            //if (distinctVertices.Count != 0)
                            //{
                            //    foreach (var item in distinctVertices)
                            //    {
                            //        avgVert.x += item.x;
                            //        avgVert.y += item.y;
                            //        avgVert.z += item.z;
                            //    }
                            //    avgVert.x = (1 / (2.0f * distinctVertices.Count)) * avgVert.x;
                            //    avgVert.y = (1 / (2.0f * distinctVertices.Count)) * avgVert.y;
                            //    avgVert.z = (1 / (2.0f * distinctVertices.Count)) * avgVert.z;
                            //}

                            //for (int m = 0; m < voxelMesh[x, y, z].vertices.Count; m++)
                            //{
                            //    var vox = voxelMesh[x, y, z].vertices[m];
                            //    vox.x = 0.5f * vox.x + avgVert.x;
                            //    vox.y = 0.5f * vox.y + avgVert.y;
                            //    vox.z = 0.5f * vox.z + avgVert.z;
                            //    voxelMesh[x, y, z].vertices[m] = vox;
                            //}
                            //////////////////////////////////////////////
                        }
                    }
                }
            }
        }
    }

    private void SmoothVoxel(ref VoxelMesh[,,] voxelMesh, ref VoxelMesh voxel, int x, int y, int z, int brushRadius)
    {
        HashSet<Vec3> distinctVertices = new HashSet<Vec3>();
        int maxX = (brushRadius + x) < (int)(chunkSize / world.voxelScale) ? (brushRadius + x) : (int)(chunkSize / world.voxelScale);
        int maxY = (brushRadius + y) < (int)(world.worldY / world.voxelScale) ? (brushRadius + y) : (int)(world.worldY / world.voxelScale);
        int maxZ = (brushRadius + z) < (int)(chunkSize / world.voxelScale) ? (brushRadius + z) : (int)(chunkSize / world.voxelScale);
        int minX = (-brushRadius + x > 0) ? (-brushRadius + x) : 0;
        int minY = (-brushRadius + y > 0) ? (-brushRadius + y) : 0;
        int minZ = (-brushRadius + z > 0) ? (-brushRadius + z) : 0;
        for (int i = minX; i < maxX; i++)
        {
            for (int j = minY; j < maxY; j++)
            {
                for (int k = minZ; k < maxZ; k++)
                {
                    int xDiff = x - i;
                    int yDiff = y - j;
                    int zDiff = z - k;
                    if (Math.Sqrt(double.Parse((xDiff * xDiff + yDiff * yDiff + zDiff * zDiff).ToString())) <= brushRadius)
                    {

                        if (/*i >= 0 && j >= 0 && k >= 0 && i < (int)(chunkSize / world.voxelScale) && j < (int)(world.worldY / world.voxelScale) && k < (int)(chunkSize / world.voxelScale) &&*/ voxelMesh[i, j, k] != null/* && Math.Sqrt(i * i + j * j + k * k) <= brushRadius*/)
                        {
                            for (int c = 0; c < voxelMesh[i, j, k].vertices.Count; c++)
                            {
                                distinctVertices.Add(voxelMesh[i, j, k].vertices[c]);
                            }
                        }



                    }

                }
            }
        }
        Vector3 avgVert = new Vector3();
        if (distinctVertices.Count != 0)
        {
            foreach (var item in distinctVertices)
            {
                avgVert.x += item.x;
                avgVert.y += item.y;
                avgVert.z += item.z;
            }
            avgVert.x = (1 / (2.0f * distinctVertices.Count)) * avgVert.x;
            avgVert.y = (1 / (2.0f * distinctVertices.Count)) * avgVert.y;
            avgVert.z = (1 / (2.0f * distinctVertices.Count)) * avgVert.z;
        }

        for (int i = 0; i < voxel.vertices.Count; i++)
        {
            var vox = voxel.vertices[i];
            vox.x = 0.5f * vox.x + avgVert.x;
            vox.y = 0.5f * vox.y + avgVert.y;
            //vox.z = 0.5f * vox.z + avgVert.z;
            voxel.vertices[i] = vox;
        }

    }
    ////private void ApplyForNeighbours(VoxelMesh[,,] voxelMesh, int x, int y, int z, Vector3 searchedVertex, List<Vector3> newValues)
    ////{
    //    for (int i = -1; i < 2; i++)
    //    {
    //        for (int j = -1; j < 2; j++)
    //        {
    //            for (int k = -1; k < 2; k++)
    //            {
    //                if (i != 0 && j != 0 && k != 0)
    //                {
    //                    foreach (var item in voxelMesh[i, j, k].vertices)
    //                    {

    //                    }
    //                }

    //            }
    //        }
    //    }
    //}

    //private void FindVerticesInNeighbours(VoxelMesh[,,] voxelMesh, int x, int y, int z, VoxelMesh voxel, ref Vector3 vertex, List<Vector3> newValues)
    //{
    //    for (int i = -1; i < 2; i++)
    //    {
    //        for (int j = -1; j < 2; j++)
    //        {
    //            for (int k = -1; k < 2; k++)
    //            {
    //                foreach (var item in voxelMesh[i, j, k].vertices)
    //                {
    //                    ///////////
    //                    voxel.vertices.Where(vert => vert.x == item.x && vert.y == item.y && vert.z == item.z).Single();
    //                }
    //            }
    //        }
    //    }
    //}

    public static readonly int[] edgeTable = {
    0x0  , 0x109, 0x203, 0x30a, 0x406, 0x50f, 0x605, 0x70c,
    0x80c, 0x905, 0xa0f, 0xb06, 0xc0a, 0xd03, 0xe09, 0xf00,
    0x190, 0x99 , 0x393, 0x29a, 0x596, 0x49f, 0x795, 0x69c,
    0x99c, 0x895, 0xb9f, 0xa96, 0xd9a, 0xc93, 0xf99, 0xe90,
    0x230, 0x339, 0x33 , 0x13a, 0x636, 0x73f, 0x435, 0x53c,
    0xa3c, 0xb35, 0x83f, 0x936, 0xe3a, 0xf33, 0xc39, 0xd30,
    0x3a0, 0x2a9, 0x1a3, 0xaa , 0x7a6, 0x6af, 0x5a5, 0x4ac,
    0xbac, 0xaa5, 0x9af, 0x8a6, 0xfaa, 0xea3, 0xda9, 0xca0,
    0x460, 0x569, 0x663, 0x76a, 0x66 , 0x16f, 0x265, 0x36c,
    0xc6c, 0xd65, 0xe6f, 0xf66, 0x86a, 0x963, 0xa69, 0xb60,
    0x5f0, 0x4f9, 0x7f3, 0x6fa, 0x1f6, 0xff , 0x3f5, 0x2fc,
    0xdfc, 0xcf5, 0xfff, 0xef6, 0x9fa, 0x8f3, 0xbf9, 0xaf0,
    0x650, 0x759, 0x453, 0x55a, 0x256, 0x35f, 0x55 , 0x15c,
    0xe5c, 0xf55, 0xc5f, 0xd56, 0xa5a, 0xb53, 0x859, 0x950,
    0x7c0, 0x6c9, 0x5c3, 0x4ca, 0x3c6, 0x2cf, 0x1c5, 0xcc ,
    0xfcc, 0xec5, 0xdcf, 0xcc6, 0xbca, 0xac3, 0x9c9, 0x8c0,
    0x8c0, 0x9c9, 0xac3, 0xbca, 0xcc6, 0xdcf, 0xec5, 0xfcc,
    0xcc , 0x1c5, 0x2cf, 0x3c6, 0x4ca, 0x5c3, 0x6c9, 0x7c0,
    0x950, 0x859, 0xb53, 0xa5a, 0xd56, 0xc5f, 0xf55, 0xe5c,
    0x15c, 0x55 , 0x35f, 0x256, 0x55a, 0x453, 0x759, 0x650,
    0xaf0, 0xbf9, 0x8f3, 0x9fa, 0xef6, 0xfff, 0xcf5, 0xdfc,
    0x2fc, 0x3f5, 0xff , 0x1f6, 0x6fa, 0x7f3, 0x4f9, 0x5f0,
    0xb60, 0xa69, 0x963, 0x86a, 0xf66, 0xe6f, 0xd65, 0xc6c,
    0x36c, 0x265, 0x16f, 0x66 , 0x76a, 0x663, 0x569, 0x460,
    0xca0, 0xda9, 0xea3, 0xfaa, 0x8a6, 0x9af, 0xaa5, 0xbac,
    0x4ac, 0x5a5, 0x6af, 0x7a6, 0xaa , 0x1a3, 0x2a9, 0x3a0,
    0xd30, 0xc39, 0xf33, 0xe3a, 0x936, 0x83f, 0xb35, 0xa3c,
    0x53c, 0x435, 0x73f, 0x636, 0x13a, 0x33 , 0x339, 0x230,
    0xe90, 0xf99, 0xc93, 0xd9a, 0xa96, 0xb9f, 0x895, 0x99c,
    0x69c, 0x795, 0x49f, 0x596, 0x29a, 0x393, 0x99 , 0x190,
    0xf00, 0xe09, 0xd03, 0xc0a, 0xb06, 0xa0f, 0x905, 0x80c,
    0x70c, 0x605, 0x50f, 0x406, 0x30a, 0x203, 0x109, 0x0   };

    /* inneholder en oversikt over hvilke punkter vi skal tegne dersom en gitt kombinasjon er funnet i edgeTable */
    public static readonly int[,] triTable =
        {{-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 8, 3, 9, 8, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 2, 10, 0, 2, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 8, 3, 2, 10, 8, 10, 9, 8, -1, -1, -1, -1, -1, -1, -1},
    {3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 11, 2, 8, 11, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 11, 2, 1, 9, 11, 9, 8, 11, -1, -1, -1, -1, -1, -1, -1},
    {3, 10, 1, 11, 10, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 10, 1, 0, 8, 10, 8, 11, 10, -1, -1, -1, -1, -1, -1, -1},
    {3, 9, 0, 3, 11, 9, 11, 10, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 3, 0, 7, 3, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 1, 9, 4, 7, 1, 7, 3, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 4, 7, 3, 0, 4, 1, 2, 10, -1, -1, -1, -1, -1, -1, -1},
    {9, 2, 10, 9, 0, 2, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
    {2, 10, 9, 2, 9, 7, 2, 7, 3, 7, 9, 4, -1, -1, -1, -1},
    {8, 4, 7, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 4, 7, 11, 2, 4, 2, 0, 4, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 1, 8, 4, 7, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
    {4, 7, 11, 9, 4, 11, 9, 11, 2, 9, 2, 1, -1, -1, -1, -1},
    {3, 10, 1, 3, 11, 10, 7, 8, 4, -1, -1, -1, -1, -1, -1, -1},
    {1, 11, 10, 1, 4, 11, 1, 0, 4, 7, 11, 4, -1, -1, -1, -1},
    {4, 7, 8, 9, 0, 11, 9, 11, 10, 11, 0, 3, -1, -1, -1, -1},
    {4, 7, 11, 4, 11, 9, 9, 11, 10, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 5, 4, 1, 5, 0, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 5, 4, 8, 3, 5, 3, 1, 5, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 1, 2, 10, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
    {5, 2, 10, 5, 4, 2, 4, 0, 2, -1, -1, -1, -1, -1, -1, -1},
    {2, 10, 5, 3, 2, 5, 3, 5, 4, 3, 4, 8, -1, -1, -1, -1},
    {9, 5, 4, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 11, 2, 0, 8, 11, 4, 9, 5, -1, -1, -1, -1, -1, -1, -1},
    {0, 5, 4, 0, 1, 5, 2, 3, 11, -1, -1, -1, -1, -1, -1, -1},
    {2, 1, 5, 2, 5, 8, 2, 8, 11, 4, 8, 5, -1, -1, -1, -1},
    {10, 3, 11, 10, 1, 3, 9, 5, 4, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 5, 0, 8, 1, 8, 10, 1, 8, 11, 10, -1, -1, -1, -1},
    {5, 4, 0, 5, 0, 11, 5, 11, 10, 11, 0, 3, -1, -1, -1, -1},
    {5, 4, 8, 5, 8, 10, 10, 8, 11, -1, -1, -1, -1, -1, -1, -1},
    {9, 7, 8, 5, 7, 9, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 3, 0, 9, 5, 3, 5, 7, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 7, 8, 0, 1, 7, 1, 5, 7, -1, -1, -1, -1, -1, -1, -1},
    {1, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 7, 8, 9, 5, 7, 10, 1, 2, -1, -1, -1, -1, -1, -1, -1},
    {10, 1, 2, 9, 5, 0, 5, 3, 0, 5, 7, 3, -1, -1, -1, -1},
    {8, 0, 2, 8, 2, 5, 8, 5, 7, 10, 5, 2, -1, -1, -1, -1},
    {2, 10, 5, 2, 5, 3, 3, 5, 7, -1, -1, -1, -1, -1, -1, -1},
    {7, 9, 5, 7, 8, 9, 3, 11, 2, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 7, 9, 7, 2, 9, 2, 0, 2, 7, 11, -1, -1, -1, -1},
    {2, 3, 11, 0, 1, 8, 1, 7, 8, 1, 5, 7, -1, -1, -1, -1},
    {11, 2, 1, 11, 1, 7, 7, 1, 5, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 8, 8, 5, 7, 10, 1, 3, 10, 3, 11, -1, -1, -1, -1},
    {5, 7, 0, 5, 0, 9, 7, 11, 0, 1, 0, 10, 11, 10, 0, -1},
    {11, 10, 0, 11, 0, 3, 10, 5, 0, 8, 0, 7, 5, 7, 0, -1},
    {11, 10, 5, 7, 11, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 1, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 8, 3, 1, 9, 8, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 5, 2, 6, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 5, 1, 2, 6, 3, 0, 8, -1, -1, -1, -1, -1, -1, -1},
    {9, 6, 5, 9, 0, 6, 0, 2, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 9, 8, 5, 8, 2, 5, 2, 6, 3, 2, 8, -1, -1, -1, -1},
    {2, 3, 11, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 0, 8, 11, 2, 0, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 1, 9, 2, 9, 11, 2, 9, 8, 11, -1, -1, -1, -1},
    {6, 3, 11, 6, 5, 3, 5, 1, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 11, 0, 11, 5, 0, 5, 1, 5, 11, 6, -1, -1, -1, -1},
    {3, 11, 6, 0, 3, 6, 0, 6, 5, 0, 5, 9, -1, -1, -1, -1},
    {6, 5, 9, 6, 9, 11, 11, 9, 8, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 3, 0, 4, 7, 3, 6, 5, 10, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 5, 10, 6, 8, 4, 7, -1, -1, -1, -1, -1, -1, -1},
    {10, 6, 5, 1, 9, 7, 1, 7, 3, 7, 9, 4, -1, -1, -1, -1},
    {6, 1, 2, 6, 5, 1, 4, 7, 8, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 5, 5, 2, 6, 3, 0, 4, 3, 4, 7, -1, -1, -1, -1},
    {8, 4, 7, 9, 0, 5, 0, 6, 5, 0, 2, 6, -1, -1, -1, -1},
    {7, 3, 9, 7, 9, 4, 3, 2, 9, 5, 9, 6, 2, 6, 9, -1},
    {3, 11, 2, 7, 8, 4, 10, 6, 5, -1, -1, -1, -1, -1, -1, -1},
    {5, 10, 6, 4, 7, 2, 4, 2, 0, 2, 7, 11, -1, -1, -1, -1},
    {0, 1, 9, 4, 7, 8, 2, 3, 11, 5, 10, 6, -1, -1, -1, -1},
    {9, 2, 1, 9, 11, 2, 9, 4, 11, 7, 11, 4, 5, 10, 6, -1},
    {8, 4, 7, 3, 11, 5, 3, 5, 1, 5, 11, 6, -1, -1, -1, -1},
    {5, 1, 11, 5, 11, 6, 1, 0, 11, 7, 11, 4, 0, 4, 11, -1},
    {0, 5, 9, 0, 6, 5, 0, 3, 6, 11, 6, 3, 8, 4, 7, -1},
    {6, 5, 9, 6, 9, 11, 4, 7, 9, 7, 11, 9, -1, -1, -1, -1},
    {10, 4, 9, 6, 4, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 10, 6, 4, 9, 10, 0, 8, 3, -1, -1, -1, -1, -1, -1, -1},
    {10, 0, 1, 10, 6, 0, 6, 4, 0, -1, -1, -1, -1, -1, -1, -1},
    {8, 3, 1, 8, 1, 6, 8, 6, 4, 6, 1, 10, -1, -1, -1, -1},
    {1, 4, 9, 1, 2, 4, 2, 6, 4, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 1, 2, 9, 2, 4, 9, 2, 6, 4, -1, -1, -1, -1},
    {0, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 3, 2, 8, 2, 4, 4, 2, 6, -1, -1, -1, -1, -1, -1, -1},
    {10, 4, 9, 10, 6, 4, 11, 2, 3, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 2, 2, 8, 11, 4, 9, 10, 4, 10, 6, -1, -1, -1, -1},
    {3, 11, 2, 0, 1, 6, 0, 6, 4, 6, 1, 10, -1, -1, -1, -1},
    {6, 4, 1, 6, 1, 10, 4, 8, 1, 2, 1, 11, 8, 11, 1, -1},
    {9, 6, 4, 9, 3, 6, 9, 1, 3, 11, 6, 3, -1, -1, -1, -1},
    {8, 11, 1, 8, 1, 0, 11, 6, 1, 9, 1, 4, 6, 4, 1, -1},
    {3, 11, 6, 3, 6, 0, 0, 6, 4, -1, -1, -1, -1, -1, -1, -1},
    {6, 4, 8, 11, 6, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 10, 6, 7, 8, 10, 8, 9, 10, -1, -1, -1, -1, -1, -1, -1},
    {0, 7, 3, 0, 10, 7, 0, 9, 10, 6, 7, 10, -1, -1, -1, -1},
    {10, 6, 7, 1, 10, 7, 1, 7, 8, 1, 8, 0, -1, -1, -1, -1},
    {10, 6, 7, 10, 7, 1, 1, 7, 3, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 6, 1, 6, 8, 1, 8, 9, 8, 6, 7, -1, -1, -1, -1},
    {2, 6, 9, 2, 9, 1, 6, 7, 9, 0, 9, 3, 7, 3, 9, -1},
    {7, 8, 0, 7, 0, 6, 6, 0, 2, -1, -1, -1, -1, -1, -1, -1},
    {7, 3, 2, 6, 7, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 11, 10, 6, 8, 10, 8, 9, 8, 6, 7, -1, -1, -1, -1},
    {2, 0, 7, 2, 7, 11, 0, 9, 7, 6, 7, 10, 9, 10, 7, -1},
    {1, 8, 0, 1, 7, 8, 1, 10, 7, 6, 7, 10, 2, 3, 11, -1},
    {11, 2, 1, 11, 1, 7, 10, 6, 1, 6, 7, 1, -1, -1, -1, -1},
    {8, 9, 6, 8, 6, 7, 9, 1, 6, 11, 6, 3, 1, 3, 6, -1},
    {0, 9, 1, 11, 6, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 8, 0, 7, 0, 6, 3, 11, 0, 11, 6, 0, -1, -1, -1, -1},
    {7, 11, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 8, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 9, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 1, 9, 8, 3, 1, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
    {10, 1, 2, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 3, 0, 8, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
    {2, 9, 0, 2, 10, 9, 6, 11, 7, -1, -1, -1, -1, -1, -1, -1},
    {6, 11, 7, 2, 10, 3, 10, 8, 3, 10, 9, 8, -1, -1, -1, -1},
    {7, 2, 3, 6, 2, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {7, 0, 8, 7, 6, 0, 6, 2, 0, -1, -1, -1, -1, -1, -1, -1},
    {2, 7, 6, 2, 3, 7, 0, 1, 9, -1, -1, -1, -1, -1, -1, -1},
    {1, 6, 2, 1, 8, 6, 1, 9, 8, 8, 7, 6, -1, -1, -1, -1},
    {10, 7, 6, 10, 1, 7, 1, 3, 7, -1, -1, -1, -1, -1, -1, -1},
    {10, 7, 6, 1, 7, 10, 1, 8, 7, 1, 0, 8, -1, -1, -1, -1},
    {0, 3, 7, 0, 7, 10, 0, 10, 9, 6, 10, 7, -1, -1, -1, -1},
    {7, 6, 10, 7, 10, 8, 8, 10, 9, -1, -1, -1, -1, -1, -1, -1},
    {6, 8, 4, 11, 8, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 6, 11, 3, 0, 6, 0, 4, 6, -1, -1, -1, -1, -1, -1, -1},
    {8, 6, 11, 8, 4, 6, 9, 0, 1, -1, -1, -1, -1, -1, -1, -1},
    {9, 4, 6, 9, 6, 3, 9, 3, 1, 11, 3, 6, -1, -1, -1, -1},
    {6, 8, 4, 6, 11, 8, 2, 10, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 3, 0, 11, 0, 6, 11, 0, 4, 6, -1, -1, -1, -1},
    {4, 11, 8, 4, 6, 11, 0, 2, 9, 2, 10, 9, -1, -1, -1, -1},
    {10, 9, 3, 10, 3, 2, 9, 4, 3, 11, 3, 6, 4, 6, 3, -1},
    {8, 2, 3, 8, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1},
    {0, 4, 2, 4, 6, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 9, 0, 2, 3, 4, 2, 4, 6, 4, 3, 8, -1, -1, -1, -1},
    {1, 9, 4, 1, 4, 2, 2, 4, 6, -1, -1, -1, -1, -1, -1, -1},
    {8, 1, 3, 8, 6, 1, 8, 4, 6, 6, 10, 1, -1, -1, -1, -1},
    {10, 1, 0, 10, 0, 6, 6, 0, 4, -1, -1, -1, -1, -1, -1, -1},
    {4, 6, 3, 4, 3, 8, 6, 10, 3, 0, 3, 9, 10, 9, 3, -1},
    {10, 9, 4, 6, 10, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 5, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 4, 9, 5, 11, 7, 6, -1, -1, -1, -1, -1, -1, -1},
    {5, 0, 1, 5, 4, 0, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
    {11, 7, 6, 8, 3, 4, 3, 5, 4, 3, 1, 5, -1, -1, -1, -1},
    {9, 5, 4, 10, 1, 2, 7, 6, 11, -1, -1, -1, -1, -1, -1, -1},
    {6, 11, 7, 1, 2, 10, 0, 8, 3, 4, 9, 5, -1, -1, -1, -1},
    {7, 6, 11, 5, 4, 10, 4, 2, 10, 4, 0, 2, -1, -1, -1, -1},
    {3, 4, 8, 3, 5, 4, 3, 2, 5, 10, 5, 2, 11, 7, 6, -1},
    {7, 2, 3, 7, 6, 2, 5, 4, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 5, 4, 0, 8, 6, 0, 6, 2, 6, 8, 7, -1, -1, -1, -1},
    {3, 6, 2, 3, 7, 6, 1, 5, 0, 5, 4, 0, -1, -1, -1, -1},
    {6, 2, 8, 6, 8, 7, 2, 1, 8, 4, 8, 5, 1, 5, 8, -1},
    {9, 5, 4, 10, 1, 6, 1, 7, 6, 1, 3, 7, -1, -1, -1, -1},
    {1, 6, 10, 1, 7, 6, 1, 0, 7, 8, 7, 0, 9, 5, 4, -1},
    {4, 0, 10, 4, 10, 5, 0, 3, 10, 6, 10, 7, 3, 7, 10, -1},
    {7, 6, 10, 7, 10, 8, 5, 4, 10, 4, 8, 10, -1, -1, -1, -1},
    {6, 9, 5, 6, 11, 9, 11, 8, 9, -1, -1, -1, -1, -1, -1, -1},
    {3, 6, 11, 0, 6, 3, 0, 5, 6, 0, 9, 5, -1, -1, -1, -1},
    {0, 11, 8, 0, 5, 11, 0, 1, 5, 5, 6, 11, -1, -1, -1, -1},
    {6, 11, 3, 6, 3, 5, 5, 3, 1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 10, 9, 5, 11, 9, 11, 8, 11, 5, 6, -1, -1, -1, -1},
    {0, 11, 3, 0, 6, 11, 0, 9, 6, 5, 6, 9, 1, 2, 10, -1},
    {11, 8, 5, 11, 5, 6, 8, 0, 5, 10, 5, 2, 0, 2, 5, -1},
    {6, 11, 3, 6, 3, 5, 2, 10, 3, 10, 5, 3, -1, -1, -1, -1},
    {5, 8, 9, 5, 2, 8, 5, 6, 2, 3, 8, 2, -1, -1, -1, -1},
    {9, 5, 6, 9, 6, 0, 0, 6, 2, -1, -1, -1, -1, -1, -1, -1},
    {1, 5, 8, 1, 8, 0, 5, 6, 8, 3, 8, 2, 6, 2, 8, -1},
    {1, 5, 6, 2, 1, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 3, 6, 1, 6, 10, 3, 8, 6, 5, 6, 9, 8, 9, 6, -1},
    {10, 1, 0, 10, 0, 6, 9, 5, 0, 5, 6, 0, -1, -1, -1, -1},
    {0, 3, 8, 5, 6, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {10, 5, 6, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 5, 10, 7, 5, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {11, 5, 10, 11, 7, 5, 8, 3, 0, -1, -1, -1, -1, -1, -1, -1},
    {5, 11, 7, 5, 10, 11, 1, 9, 0, -1, -1, -1, -1, -1, -1, -1},
    {10, 7, 5, 10, 11, 7, 9, 8, 1, 8, 3, 1, -1, -1, -1, -1},
    {11, 1, 2, 11, 7, 1, 7, 5, 1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 1, 2, 7, 1, 7, 5, 7, 2, 11, -1, -1, -1, -1},
    {9, 7, 5, 9, 2, 7, 9, 0, 2, 2, 11, 7, -1, -1, -1, -1},
    {7, 5, 2, 7, 2, 11, 5, 9, 2, 3, 2, 8, 9, 8, 2, -1},
    {2, 5, 10, 2, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1},
    {8, 2, 0, 8, 5, 2, 8, 7, 5, 10, 2, 5, -1, -1, -1, -1},
    {9, 0, 1, 5, 10, 3, 5, 3, 7, 3, 10, 2, -1, -1, -1, -1},
    {9, 8, 2, 9, 2, 1, 8, 7, 2, 10, 2, 5, 7, 5, 2, -1},
    {1, 3, 5, 3, 7, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 7, 0, 7, 1, 1, 7, 5, -1, -1, -1, -1, -1, -1, -1},
    {9, 0, 3, 9, 3, 5, 5, 3, 7, -1, -1, -1, -1, -1, -1, -1},
    {9, 8, 7, 5, 9, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {5, 8, 4, 5, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1},
    {5, 0, 4, 5, 11, 0, 5, 10, 11, 11, 3, 0, -1, -1, -1, -1},
    {0, 1, 9, 8, 4, 10, 8, 10, 11, 10, 4, 5, -1, -1, -1, -1},
    {10, 11, 4, 10, 4, 5, 11, 3, 4, 9, 4, 1, 3, 1, 4, -1},
    {2, 5, 1, 2, 8, 5, 2, 11, 8, 4, 5, 8, -1, -1, -1, -1},
    {0, 4, 11, 0, 11, 3, 4, 5, 11, 2, 11, 1, 5, 1, 11, -1},
    {0, 2, 5, 0, 5, 9, 2, 11, 5, 4, 5, 8, 11, 8, 5, -1},
    {9, 4, 5, 2, 11, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 5, 10, 3, 5, 2, 3, 4, 5, 3, 8, 4, -1, -1, -1, -1},
    {5, 10, 2, 5, 2, 4, 4, 2, 0, -1, -1, -1, -1, -1, -1, -1},
    {3, 10, 2, 3, 5, 10, 3, 8, 5, 4, 5, 8, 0, 1, 9, -1},
    {5, 10, 2, 5, 2, 4, 1, 9, 2, 9, 4, 2, -1, -1, -1, -1},
    {8, 4, 5, 8, 5, 3, 3, 5, 1, -1, -1, -1, -1, -1, -1, -1},
    {0, 4, 5, 1, 0, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {8, 4, 5, 8, 5, 3, 9, 0, 5, 0, 3, 5, -1, -1, -1, -1},
    {9, 4, 5, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 11, 7, 4, 9, 11, 9, 10, 11, -1, -1, -1, -1, -1, -1, -1},
    {0, 8, 3, 4, 9, 7, 9, 11, 7, 9, 10, 11, -1, -1, -1, -1},
    {1, 10, 11, 1, 11, 4, 1, 4, 0, 7, 4, 11, -1, -1, -1, -1},
    {3, 1, 4, 3, 4, 8, 1, 10, 4, 7, 4, 11, 10, 11, 4, -1},
    {4, 11, 7, 9, 11, 4, 9, 2, 11, 9, 1, 2, -1, -1, -1, -1},
    {9, 7, 4, 9, 11, 7, 9, 1, 11, 2, 11, 1, 0, 8, 3, -1},
    {11, 7, 4, 11, 4, 2, 2, 4, 0, -1, -1, -1, -1, -1, -1, -1},
    {11, 7, 4, 11, 4, 2, 8, 3, 4, 3, 2, 4, -1, -1, -1, -1},
    {2, 9, 10, 2, 7, 9, 2, 3, 7, 7, 4, 9, -1, -1, -1, -1},
    {9, 10, 7, 9, 7, 4, 10, 2, 7, 8, 7, 0, 2, 0, 7, -1},
    {3, 7, 10, 3, 10, 2, 7, 4, 10, 1, 10, 0, 4, 0, 10, -1},
    {1, 10, 2, 8, 7, 4, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 1, 4, 1, 7, 7, 1, 3, -1, -1, -1, -1, -1, -1, -1},
    {4, 9, 1, 4, 1, 7, 0, 8, 1, 8, 7, 1, -1, -1, -1, -1},
    {4, 0, 3, 7, 4, 3, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {4, 8, 7, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {9, 10, 8, 10, 11, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 9, 3, 9, 11, 11, 9, 10, -1, -1, -1, -1, -1, -1, -1},
    {0, 1, 10, 0, 10, 8, 8, 10, 11, -1, -1, -1, -1, -1, -1, -1},
    {3, 1, 10, 11, 3, 10, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 2, 11, 1, 11, 9, 9, 11, 8, -1, -1, -1, -1, -1, -1, -1},
    {3, 0, 9, 3, 9, 11, 1, 2, 9, 2, 11, 9, -1, -1, -1, -1},
    {0, 2, 11, 8, 0, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {3, 2, 11, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 8, 2, 8, 10, 10, 8, 9, -1, -1, -1, -1, -1, -1, -1},
    {9, 10, 2, 0, 9, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {2, 3, 8, 2, 8, 10, 0, 1, 8, 1, 10, 8, -1, -1, -1, -1},
    {1, 10, 2, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {1, 3, 8, 9, 1, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 9, 1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {0, 3, 8, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1},
    {-1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1}
};

}
