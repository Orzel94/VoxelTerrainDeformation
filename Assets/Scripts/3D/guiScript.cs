using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class guiScript : MonoBehaviour
{
    public enum Presets
    {
        RIDGE=0
    }
    public Button generateBtn;
    public GameObject world;
    //World parameters
    [Header("World parameters")]
    public float X;
    public float Z;
    public float chunkSize;
    public float voxelScale;

    public InputField XInput;
    public InputField ZInput;
    public InputField chunkSizeInput;
    public InputField voxelScaleInput;
    //RidgeNoise parameters
    [Header("RidgeNoise parameters")]
    public float rBias;
    public float rGain;
    public float rnGain;
    public float rnOffset;
    public float rnExp;

    public InputField rBiasInput;
    public InputField rGainInput;
    public InputField rnGainInput;
    public InputField rnOffsetInput;
    public InputField rnExpInput;
    //

    public Presets selectedPreset;
    // Start is called before the first frame update
    void Start()
    {
        Button btn = generateBtn.GetComponent<Button>();
        btn.onClick.AddListener(GenerateClicked);

        if (selectedPreset== Presets.RIDGE)
        {
            rBiasInput.text = rBias.ToString();
            rGainInput.text = rGain.ToString();
            rnGainInput.text = rnGain.ToString();
            rnOffsetInput.text = rnOffset.ToString();
            rnExpInput.text = rnExp.ToString();

        }
        XInput.text = X.ToString();
        ZInput.text = Z.ToString();
        chunkSizeInput.text = chunkSize.ToString();
        voxelScaleInput.text = voxelScale.ToString();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateClicked()
    {
        var worldTMP = world.GetComponent("World") as World;
        worldTMP.worldX = int.Parse(XInput.text);
        worldTMP.worldY = 64;
        worldTMP.worldZ = int.Parse(ZInput.text);
        worldTMP.voxelScale = float.Parse(voxelScaleInput.text);

        worldTMP.rBias = float.Parse(rBiasInput.text); ;
        worldTMP.rGain = float.Parse(rGainInput.text); ;
        worldTMP.rnExp = float.Parse(rnExpInput.text); ;
        worldTMP.rnGain = float.Parse(rnGainInput.text); ;
        worldTMP.rnOffset = float.Parse(rnOffsetInput.text);
        worldTMP.chunkSize = int.Parse(chunkSizeInput.text);
        worldTMP.GenerateWorld();
        Debug.Log("Generation in progress");
    }
}
