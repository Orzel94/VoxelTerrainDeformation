using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
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


    public float rBias;
    public float rGain;
    public float rnExp;
    public float rnGain;
    public float rnOffset;

    public GameObject chunk;
    public Chunk[,,] chunks;  //Changed from public GameObject[,,] chunks;
    public List<GameObject> chunksObjects;
    public int chunkSize;

    public ConcurrentQueue<string> logs;

    [Tooltip("size of single voxel - value must be power of 2")]
    public float voxelScale;

    // Start is called before the first frame update
    void Start()
    {
        logs = new ConcurrentQueue<string>();
        Task.Factory.StartNew(() =>
        {
            ////UnityEngine.//Debug.Log($"saving thread started");
           // Console.WriteLine("dsafdsafafsddasf");
            WriteLogAsync($"logs-{DateTime.Now.ToString("dd-MM-yyyy")}.csv");

        });




    }

    public async Task WriteLogAsync(string fileName)
    {
        string log;
        try
        {
            var path = Path.Combine(fileName);
            File.AppendAllText(path, $"------------------||App started||{DateTime.Now.ToString()}||-----------------");
            using (StreamWriter writer = File.AppendText(path))
            {
                while (true)
                {
                    if (logs.TryDequeue(out log))
                    {

                        await writer.WriteLineAsync(log);

                    }

                }
            }
        }
        catch (Exception ex)
        {

            throw;
        }
    }

    // Update is called once per frame
    void Update()
    {

    }


    public void GenerateWorld()
    {
        if (chunksObjects != null && chunksObjects.Count > 0)
        {
            foreach (var item in chunksObjects)
            {
                Destroy(item);
            }
        }
        chunksObjects = new List<GameObject>();
        chunks = new Chunk[Mathf.FloorToInt(worldX / chunkSize),
        Mathf.FloorToInt(1), Mathf.FloorToInt(worldZ / chunkSize)];

        logs.Enqueue($"||--------------New Terrain generation; Terrain size (X/Y/Z); {worldX}/{worldY}/{worldZ}; voxel scale; {voxelScale}; chunk size; {chunkSize}; voxel count; {worldX * worldY * worldZ / voxelScale};");
        var tasks = new List<Task>();
        var stopwatchforTerrain = new Stopwatch();
        stopwatchforTerrain.Start();
        for (int x = 0; x < chunks.GetLength(0); x++)
        {
            for (int y = 0; y < 1; y++)
            {
                for (int z = 0; z < chunks.GetLength(2); z++)
                {

                    //Create a temporary Gameobject for the new chunk instead of using chunks[x,y,z]
                    GameObject newChunk = Instantiate(chunk, new Vector3(x * chunkSize / 2,
                     y * chunkSize, z * chunkSize / 2), new Quaternion(0, 0, 0, 0)) as GameObject;
                    chunksObjects.Add(newChunk);
                    //Now instead of using a temporary variable for the script assign it
                    //to chunks[x,y,z] and use it instead of the old \"newChunkScript\" 
                    chunks[x, y, z] = newChunk.GetComponent("Chunk") as Chunk;
                    chunks[x, y, z].worldGO = gameObject;
                    chunks[x, y, z].chunkSize = chunkSize;
                    chunks[x, y, z].chunkX = x * chunkSize / 2;
                    chunks[x, y, z].chunkY = y * chunkSize;
                    chunks[x, y, z].chunkZ = z * chunkSize / 2;
                    chunks[x, y, z].voxelScale = voxelScale;
                    chunks[x, y, z].rBias = rBias;
                    chunks[x, y, z].rGain = rGain;
                    chunks[x, y, z].ChunkIndex = new Vector3(x, y, z);
                    //chunks[x, y, z].GenerateTerrain();
                    var pX = x;
                    var pY = y;
                    var pZ = z;
                    var chunkTMP = chunks[x, y, z];
                    
                    //tasks.Add(Task.Factory.StartNew(() =>
                    //{
                    //    var c = chunkTMP;
                    //    var stopwatch = new Stopwatch();
                    //    stopwatch.Start();
                    //try {
                    //        c.GenerateTerrain();
                    //        c.terrainGenerationEnded = true;
                    //} catch(Exception ex)
                    //    {
                    //        throw;
                    //    }

                    //    stopwatch.Stop();
                    //    logs.Enqueue($"|| Chunk whole terrain generated; Elapsed seconds: {stopwatch.ElapsedMilliseconds / 1000.0f};  Terrain size (X/Y/Z); {worldX}/{worldY}/{worldZ};Chunk size; {chunkSize}; voxel scale: {voxelScale}");

                    //}));
                    try { 
                    tasks.Add(chunkTMP.GenAsync());
                    }
                    catch (Exception ex)
                    {
                        throw;
                    }

                }
            }
        }
        Task.Factory.ContinueWhenAll(tasks.ToArray(), wordCountTasks => {
            stopwatchforTerrain.Stop();
            logs.Enqueue($"||-----WHOLE Terrain generated;Elapsed seconds; {stopwatchforTerrain.ElapsedMilliseconds};  Terrain size (X/Y/Z); {worldX}/{worldY}/{worldZ}; Chunk size; {chunkSize}; voxel scale: {voxelScale}");

        });
    }

    public void DeformChunk(DefScript.Shape selectedShape, int size, double lnMultiplier, Vector3 position)
    {
        //chunks[0, 0, 0].DeformGeometric(selectedShape, size, lnMultiplier, position);
        //foreach (var item in chunks)
        //{
        //    item.DeformGeometric(selectedShape, size, lnMultiplier, position);
        //}

        float x = Mathf.RoundToInt(position.x / voxelScale) * voxelScale;
        float y = Mathf.RoundToInt(position.y / voxelScale) * voxelScale;
        float z = Mathf.RoundToInt(position.z / voxelScale) * voxelScale;

        int updateX = Mathf.FloorToInt(x / chunkSize);
        int updateY = Mathf.FloorToInt(y / chunkSize);
        int updateZ = Mathf.FloorToInt(z / chunkSize);

        int voxXCenter = Mathf.FloorToInt((x - (updateX * chunkSize)) / voxelScale);
        int voxYCenter = Mathf.FloorToInt(y / voxelScale);
        int voxZCenter = Mathf.FloorToInt((z - (updateZ * chunkSize)) / voxelScale);

        //for (int i = 0; i < chunkSize-2; i++)
        //{
        //    chunks[updateX,updateY,updateZ].voxels[voxXCenter,i,voxYCenter]= VoxelTypeEnum.GRASS;
        //}
        //try
        //{
        //    chunks[updateX, updateY, updateZ].deformInprogress = true;
        //    chunks[updateX, updateY, updateZ].draw();
        //    chunks[updateX, updateY, updateZ].meshUpdateNeeded = true;
        //}catch(Exception ex)
        //{
        //    throw;
        //}
        ////////////////////////////////
        ///
        var stopwatch = new Stopwatch();
        stopwatch.Start();

        //choosen voxel on terrain
        //int chunkX = Mathf.FloorToInt(x / world.chunkSize);
        //int chunkY = Mathf.FloorToInt(y / world.chunkSize);
        //int chunkZ = Mathf.FloorToInt(z / world.chunkSize);
        HashSet<Vector3> usedChunks = new HashSet<Vector3>();
        usedChunks.Add(new Vector3(updateX, updateY, updateZ));
        for (int voxX = voxXCenter - size; voxX < voxXCenter + size; voxX++)
        {
            int ChunkXOffset = 0;
            int voxXindex = voxX;
            if (voxX < 0)
            {
                ChunkXOffset--;
                ChunkXOffset -= Mathf.FloorToInt(size / (chunkSize / voxelScale));
                voxXindex = (int)(chunkSize / voxelScale) + voxX;
            }
            else if (voxX > chunkSize / voxelScale)
            {
                ChunkXOffset++;
                ChunkXOffset += Mathf.FloorToInt(size / (chunkSize / voxelScale));
                voxXindex = voxX - (int)(chunkSize / voxelScale);
            }

            for (int voxZ = voxZCenter - size; voxZ < voxZCenter + size; voxZ++)
            {
                int ChunkZOffset = 0;
                int voxZindex = voxZ;
                if (voxZ < 0)
                {
                    ChunkZOffset--;
                    ChunkZOffset -= Mathf.FloorToInt(size / (chunkSize / voxelScale));
                    voxZindex = (int)(chunkSize / voxelScale) + voxZ;
                }
                else if (voxZ > chunkSize / voxelScale)
                {
                    ChunkZOffset++;
                    ChunkZOffset += Mathf.FloorToInt(size / (chunkSize / voxelScale));
                    voxZindex = voxZ - (int)(chunkSize / voxelScale);
                }

                try
                {
                    //if (updateX + ChunkXOffset < 0)
                    //{
                    //    continue;
                    //}
                    //if (updateZ + ChunkZOffset < 0)
                    //{
                    //    continue;
                    //}

                    usedChunks.Add(new Vector3(updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset));
                    //if (voxX >= 0 && voxX < chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels.GetLength(0) && voxZ >= 0 && voxZ < chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels.GetLength(2))
                    //{
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
                                for (int voxY = 0; voxY < (int)((worldY / voxelScale) - 1); voxY++)
                                {
                                    var sssdasd = voxX - (int)(ChunkXOffset * (chunkSize / voxelScale));
                                    var fgggg = voxZ - (int)(ChunkZOffset * (chunkSize / voxelScale));
                                    if (voxY <= targetHeight)
                                    {
                                        int arrayMaxIndex = (int)(chunkSize / voxelScale);
                                        //if (ChunkXOffset == 0 && ChunkZOffset == 0)
                                        //{
                                        if (ChunkXOffset == 0 && ChunkZOffset == 0)
                                        {


                                            if (voxXindex == 0)
                                            {
                                                if (voxZindex == 0)
                                                {
                                                    ///
                                                    if (updateX - 1 >= 0) chunks[updateX - 1, updateY, updateZ].voxels[arrayMaxIndex, voxY, 0] = VoxelTypeEnum.GRASS;
                                                    if (updateZ - 1 >= 0) chunks[updateX, updateY, updateZ - 1].voxels[0, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                    if (updateX - 1 >= 0 && updateZ - 1 >= 0) chunks[updateX - 1, updateY, updateZ - 1].voxels[arrayMaxIndex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                    chunks[updateX, updateY, updateZ].voxels[0, voxY, 0] = VoxelTypeEnum.GRASS;

                                                }
                                                else if (voxZindex == arrayMaxIndex)
                                                {
                                                    if (updateX - 1 >= 0) chunks[updateX - 1, updateY, updateZ].voxels[arrayMaxIndex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                    if (updateZ + 1 < chunks.GetLength(2)) chunks[updateX, updateY, updateZ + 1].voxels[0, voxY, 0] = VoxelTypeEnum.GRASS;
                                                    if (updateX - 1 >= 0 && updateZ + 1 < chunks.GetLength(2)) chunks[updateX - 1, updateY, updateZ + 1].voxels[arrayMaxIndex, voxY, 0] = VoxelTypeEnum.GRASS;
                                                    chunks[updateX, updateY, updateZ].voxels[0, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                }
                                                else
                                                {
                                                    chunks[updateX, updateY, updateZ].voxels[0, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                                    if (updateX - 1 >= 0) chunks[updateX - 1, updateY, updateZ].voxels[arrayMaxIndex, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                                }
                                            }
                                            else if (voxXindex == arrayMaxIndex)
                                            {
                                                if (voxZindex == 0)
                                                {
                                                    if (updateZ - 1 >= 0) chunks[updateX, updateY, updateZ - 1].voxels[arrayMaxIndex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                    if (updateX + 1 < chunks.GetLength(0)) chunks[updateX + 1, updateY, updateZ].voxels[0, voxY, 0] = VoxelTypeEnum.GRASS;
                                                    if (updateZ - 1 >= 0 && updateX + 1 < chunks.GetLength(0)) chunks[updateX + 1, updateY, updateZ - 1].voxels[0, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                    chunks[updateX, updateY, updateZ].voxels[arrayMaxIndex, voxY, 0] = VoxelTypeEnum.GRASS;
                                                }
                                                else if (voxZindex == arrayMaxIndex)
                                                {
                                                    /////
                                                    if (updateZ + 1 < chunks.GetLength(2)) chunks[updateX, updateY, updateZ + 1].voxels[arrayMaxIndex, voxY, 0] = VoxelTypeEnum.GRASS;
                                                    if (updateX + 1 < chunks.GetLength(0)) chunks[updateX + 1, updateY, updateZ].voxels[0, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                    if (updateZ + 1 < chunks.GetLength(2) && updateX + 1 < chunks.GetLength(0)) chunks[updateX + 1, updateY, updateZ + 1].voxels[0, voxY, 0] = VoxelTypeEnum.GRASS;
                                                    chunks[updateX, updateY, updateZ].voxels[arrayMaxIndex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                }
                                                else
                                                {
                                                    chunks[updateX, updateY, updateZ].voxels[arrayMaxIndex, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                                    if (updateX + 1 < chunks.GetLength(0)) chunks[updateX + 1, updateY, updateZ].voxels[0, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                                }
                                            }
                                            else if (voxZindex == 0)
                                            {
                                                if (updateZ - 1 >= 0) chunks[updateX, updateY, updateZ - 1].voxels[voxXindex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                                chunks[updateX, updateY, updateZ].voxels[voxXindex, voxY, 0] = VoxelTypeEnum.GRASS;
                                            }
                                            else if (voxZindex == arrayMaxIndex)
                                            {
                                                if (updateZ + 1 < chunks.GetLength(2)) chunks[updateX, updateY, updateZ + 1].voxels[voxXindex, voxY, 0] = VoxelTypeEnum.GRASS;
                                                chunks[updateX, updateY, updateZ].voxels[voxXindex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;

                                            }
                                            else
                                            {
                                                chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels[voxXindex, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                            }
                                        }
                                        else
                                        {
                                            chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels[voxXindex, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                            if (ChunkXOffset == 1 && voxZindex == arrayMaxIndex)
                                            {
                                                chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset + 1].voxels[voxXindex, voxY, 0] = VoxelTypeEnum.GRASS;
                                            }
                                            if (ChunkZOffset == 1 && voxXindex == arrayMaxIndex)
                                            {
                                                chunks[updateX + ChunkXOffset + 1, updateY, updateZ + ChunkZOffset].voxels[0, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                            }

                                            if (ChunkXOffset == -1 && voxZindex == 0)
                                            {
                                                chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset - 1].voxels[voxXindex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                            }
                                            if (ChunkZOffset == -1 && voxXindex == 0)
                                            {
                                                chunks[updateX + ChunkXOffset - 1, updateY, updateZ + ChunkZOffset].voxels[arrayMaxIndex, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                            }

                                            if (ChunkXOffset == -1 && voxZindex == arrayMaxIndex)
                                            {
                                                chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset + 1].voxels[voxXindex, voxY, 0] = VoxelTypeEnum.GRASS;
                                            }
                                            if (ChunkZOffset == -1 && voxXindex == arrayMaxIndex)
                                            {
                                                chunks[updateX + ChunkXOffset + 1, updateY, updateZ + ChunkZOffset].voxels[0, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                            }

                                            if (ChunkXOffset == 1 && voxZindex == 0)
                                            {
                                                chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset - 1].voxels[voxXindex, voxY, arrayMaxIndex] = VoxelTypeEnum.GRASS;
                                            }
                                            if (ChunkZOffset == 1 && voxXindex == 0)
                                            {
                                                chunks[updateX + ChunkXOffset - 1, updateY, updateZ + ChunkZOffset].voxels[arrayMaxIndex, voxY, voxZindex] = VoxelTypeEnum.GRASS;
                                            }
                                        }
                                        //}
                                        //TODO:fix
                                    }





                                    //try
                                    //{
                                    //    if (chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels[voxX - (int)(ChunkXOffset * (chunkSize / voxelScale)), voxY, voxZ - (int)(ChunkZOffset * (chunkSize / voxelScale))] == VoxelTypeEnum.AIR && voxY <= targetHeight)
                                    //    {
                                    //        chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels[voxX - (int)(ChunkXOffset * (chunkSize / voxelScale)), voxY, voxZ - (int)(ChunkZOffset * (chunkSize / voxelScale))] = VoxelTypeEnum.GRASS;
                                    //        if (voxZ - ChunkZOffset * (chunkSize / voxelScale) == chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels.GetLength(2) - 1 && voxX - ChunkXOffset * (chunkSize / voxelScale) == chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels.GetLength(0) - 1)
                                    //        {
                                    //            chunks[updateX + ChunkXOffset+1, updateY, updateZ + ChunkZOffset + 1].voxels[0, voxY, 0] = VoxelTypeEnum.GRASS;
                                    //            usedChunks.Add(new Vector3(updateX + ChunkXOffset+1, updateY, updateZ + ChunkZOffset + 1));
                                    //        }else if (voxX - ChunkXOffset * (chunkSize / voxelScale)== chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels.GetLength(0)-1)
                                    //        {
                                    //            chunks[updateX + ChunkXOffset+1, updateY, updateZ + ChunkZOffset].voxels[0, voxY, voxZ - (int)(ChunkZOffset * (chunkSize / voxelScale))] = VoxelTypeEnum.GRASS;
                                    //            usedChunks.Add(new Vector3(updateX + ChunkXOffset + 1, updateY, updateZ + ChunkZOffset));
                                    //        }else if (voxZ - ChunkZOffset * (chunkSize / voxelScale) == chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels.GetLength(2)-1)
                                    //        {
                                    //            chunks[updateX + ChunkXOffset , updateY, updateZ + ChunkZOffset+1].voxels[voxX - (int)(ChunkXOffset * (chunkSize / voxelScale)), voxY, 0] = VoxelTypeEnum.GRASS;
                                    //            usedChunks.Add(new Vector3(updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset+1));
                                    //        }else if (voxZ - ChunkZOffset * (chunkSize / voxelScale) == 0 && voxX - ChunkXOffset * (chunkSize / voxelScale) == 0)
                                    //        {
                                    //            chunks[updateX + ChunkXOffset + 1, updateY, updateZ + ChunkZOffset + 1].voxels[(int)(chunkSize/voxelScale), voxY, (int)(chunkSize / voxelScale)] = VoxelTypeEnum.GRASS;
                                    //            usedChunks.Add(new Vector3(updateX + ChunkXOffset + 1, updateY, updateZ + ChunkZOffset + 1));
                                    //        }
                                    //        else if (voxX - ChunkXOffset * (chunkSize / voxelScale) == 0)
                                    //        {
                                    //            chunks[updateX + ChunkXOffset + 1, updateY, updateZ + ChunkZOffset].voxels[(int)(chunkSize / voxelScale), voxY, voxZ - (int)(ChunkZOffset * (chunkSize / voxelScale))] = VoxelTypeEnum.GRASS;
                                    //            usedChunks.Add(new Vector3(updateX + ChunkXOffset + 1, updateY, updateZ + ChunkZOffset));
                                    //        }
                                    //        else if (voxZ - ChunkZOffset * (chunkSize / voxelScale) == 0)
                                    //        {
                                    //            chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset + 1].voxels[voxX - (int)(ChunkXOffset * (chunkSize / voxelScale)), voxY, (int)(chunkSize / voxelScale)] = VoxelTypeEnum.GRASS;
                                    //            usedChunks.Add(new Vector3(updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset + 1));
                                    //        }
                                    //    }
                                    //}
                                    //catch (Exception ex)
                                    //{

                                    //    throw;
                                    //}
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
                        for (int voxY = 0; voxY < (int)((worldY / voxelScale) - 1); voxY++)
                        {
                            if (chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels[voxX - (int)(ChunkXOffset * (chunkSize / voxelScale)), voxY, voxZ - (int)(ChunkZOffset * (chunkSize / voxelScale))] == VoxelTypeEnum.AIR && voxY <= targetHeight)
                            {
                                chunks[updateX + ChunkXOffset, updateY, updateZ + ChunkZOffset].voxels[voxX - (int)(ChunkXOffset * (chunkSize / voxelScale)), voxY, voxZ - (int)(ChunkZOffset * (chunkSize / voxelScale))] = VoxelTypeEnum.GRASS;
                            }
                        }
                    }
                    //}
                }
                catch (Exception ex)
                {
                    throw;
                }
            }
        }
        stopwatch.Stop();

        logs.Enqueue($"||------Terrain deformed; Deformation time [ms]; {stopwatch.ElapsedMilliseconds}; Chunks affected; {usedChunks.Count} ;");
        foreach (var item in usedChunks)
        {
            Task.Factory.StartNew(() =>
            {
                chunks[(int)item.x, (int)item.y, (int)item.z].deformInprogress = true;
                //UnityEngine.//Debug.Log($"deformed");
                chunks[(int)item.x, (int)item.y, (int)item.z].draw();
                chunks[(int)item.x, (int)item.y, (int)item.z].meshUpdateNeeded = true;
                //UnityEngine.//Debug.Log($"drawn");
            });
        }
        //logs.Enqueue($"Voxel deformation; {stopwatch.ElapsedMilliseconds};  Terrain size (X/Y/Z); {world.worldX}/{world.worldY}/{world.worldZ}; voxel scale: {world.voxelScale}");
    }

}
