using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;

public class Voxel
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public VoxelTypeEnum type { get; set; }
}

public enum VoxelTypeEnum
{
    AIR = 0,
    STONE = 1,
    GRASS = 2
}

public class Chunk : MonoBehaviour
{

    private List<Vector3> newVertices = new List<Vector3>();
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

    private Vector2 tGrassTop = new Vector2(2, 1);

    public bool update;

    private Voxel[,,] voxels;
    public float voxelScale;

    // Start is called before the first frame update
    void Start()
    {
        mesh = GetComponent<MeshFilter>().mesh;
        col = GetComponent<MeshCollider>();
        world = worldGO.GetComponent("World") as World;

        GenerateTerrain();
    }

    // Update is called once per frame
    void Update()
    {

    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = newVertices.ToArray();
        mesh.uv = newUV.ToArray();
        mesh.triangles = newTriangles.ToArray();
        mesh.Optimize();
        mesh.RecalculateNormals();

        col.sharedMesh = null;
        col.sharedMesh = mesh;

        newVertices.Clear();
        newUV.Clear();
        newTriangles.Clear();

        faceCount = 0;
    }

    Voxel Block(int x, int y, int z)
    {
        try
        {
            return voxels[x, y, z];
        }
        catch (Exception ex)
        {
            var vo = new Voxel();
            vo.type = VoxelTypeEnum.AIR;
            return vo;
        }

        //return world.Block(x + chunkX, y + chunkY, z + chunkZ); // Don't replace the world.Block in this line!
    }

    public void GenerateTerrain()
    {
        voxels = new Voxel[(int)(chunkSize / world.voxelScale), (int)(world.worldY / world.voxelScale), /** worldYMultiplier,*/ (int)(chunkSize / world.voxelScale)];
        for (int x = 0; x < (int)(chunkSize / world.voxelScale); x++)
        {
            for (int z = 0; z < (int)(chunkSize / world.voxelScale); z++)
            {
                int stone = world.PerlinNoise(chunkX+ x, 0,chunkZ+ z, 200, (int)(world.worldY / world.voxelScale) /** world.worldYMultiplier*/, 4.2f);// + (int)(world.worldY / world.voxelScale);
                //stone += PerlinNoise(x, 300, z, 20, 4, 1.5f) + 10;
                int dirt = world.PerlinNoise(chunkX + x, 100, chunkZ + z, 200, world.worldY, 0) + 1;// + (int)(world.worldY / world.voxelScale); //Added +1 to make sure minimum grass height is 1
                //Debug.Log($"stone: {stone} , x: {x}, z: {z}");
                //for (int y = (int)(worldY / voxelScale); y < (int)(worldY / voxelScale) * worldYMultiplier; y++)
                for (int y = 0; y < (int)(world.worldY / world.voxelScale); y++)
                {
                    try
                    {
                        Voxel tmpVox = new Voxel();
                        if (y <= stone)
                        {
                            tmpVox.x = chunkX + x * world.voxelScale;
                            tmpVox.y = chunkY + y * world.voxelScale;
                            tmpVox.z = chunkZ + z * world.voxelScale;
                            tmpVox.type = VoxelTypeEnum.STONE;
                            voxels[x, y, z] = tmpVox;
                        }
                        else if (y <= dirt + stone)
                        {
                            tmpVox.x = chunkX + x * world.voxelScale;
                            tmpVox.y = chunkY + y * world.voxelScale;
                            tmpVox.z = chunkZ + z * world.voxelScale;
                            tmpVox.type = VoxelTypeEnum.GRASS;
                            voxels[x, y, z] = tmpVox;
                        }
                        else if (y == 0)
                        {
                            tmpVox.x = chunkX + x * world.voxelScale;
                            tmpVox.y = chunkY + y * world.voxelScale;
                            tmpVox.z = chunkZ + z * world.voxelScale;
                            tmpVox.type = VoxelTypeEnum.STONE;
                            voxels[x, y, z] = tmpVox;
                        }
                        else
                        {
                            tmpVox.x = chunkX + x * world.voxelScale;
                            tmpVox.y = chunkY + y * world.voxelScale;
                            tmpVox.z = chunkZ + z * world.voxelScale;
                            tmpVox.type = VoxelTypeEnum.AIR;
                            voxels[x, y, z] = tmpVox;
                        }

                    }
                    catch (System.Exception ex)
                    {

                        throw;
                    }

                }
            }
        }

        GenerateMesh();
    }

    public void GenerateMesh()
    {
        try
        {
            for (int x = 0; x < chunkSize / world.voxelScale; x++)
            {
                for (int y = 0; y < chunkSize / world.voxelScale; y++)
                {
                    for (int z = 0; z < chunkSize / world.voxelScale; z++)
                    {
                        Voxel currentVoxel = Block(x, y, z);
                        //This code will run for every block in the chunk
                        if (Block(x, y, z).type != 0)
                        {
                            //If the block is solid
                            try
                            {
                                if (Block(x, y + 1, z).type == 0)
                                {
                                    //Block above is air
                                    CubeTop(currentVoxel.x, currentVoxel.y, currentVoxel.z, currentVoxel.type);
                                }

                            }
                            catch (Exception ex)
                            {

                                throw;
                            }

                            if (Block(x, y - 1, z).type == 0)
                            {
                                //Block below is air
                                CubeBot(currentVoxel.x, currentVoxel.y, currentVoxel.z, currentVoxel.type);

                            }

                            if (Block(x + 1, y, z).type == 0)
                            {
                                //Block east is air
                                CubeEast(currentVoxel.x, currentVoxel.y, currentVoxel.z, currentVoxel.type);

                            }

                            if (Block(x - 1, y, z).type == 0)
                            {
                                //Block west is air
                                CubeWest(currentVoxel.x, currentVoxel.y, currentVoxel.z, currentVoxel.type);

                            }

                            if (Block(x, y, z + 1).type == 0)
                            {
                                //Block north is air
                                CubeNorth(currentVoxel.x, currentVoxel.y, currentVoxel.z, currentVoxel.type);

                            }

                            if (Block(x, y, z - 1).type == 0)
                            {
                                //Block south is air
                                CubeSouth(currentVoxel.x, currentVoxel.y, currentVoxel.z, currentVoxel.type);

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

        UpdateMesh();
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

        faceCount++; // Add this line
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



}
