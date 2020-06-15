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
    public float Y;
    public float chunkSize;
    public float voxelScale;

    public InputField XInput;
    public InputField YInput;
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
        YInput.text = Y.ToString();
        chunkSizeInput.text = chunkSize.ToString();
        voxelScaleInput.text = voxelScale.ToString();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void GenerateClicked()
    {
        var worldTMP =world.GetComponent("World") as World;
        ///ssss
        Debug.Log("You have clicked the button!");
    }
}
