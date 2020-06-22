using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DefScript : MonoBehaviour
{

    public enum DefTypes
    {
        Geometric = 0
    }
    public enum Shape
    {
        Circle=0
    }
    public Button generateBtn;
    public GameObject world;
    //World parameters
    [Header("Def parameters")]


    private bool modeApllied;
    //RidgeNoise parameters
    [Header("Shape parameters")]
    public Shape selectedShape;
    public InputField cSize;
    public InputField cMaxValue;

    public DefTypes selectedType;
    
    // Start is called before the first frame update
    void Start()
    {
        modeApllied = false;
        Button btn = generateBtn.GetComponent<Button>();
        btn.onClick.AddListener(ApplyClicked);
    }

    public void ApplyClicked()
    {
        modeApllied = true;
    }
    public void Update()
    {
        var worldTMP = world.GetComponent("World") as World;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                Vector3 position = hit.point;
                position += (hit.normal * -0.5f);


                if (selectedType == DefTypes.Geometric && selectedShape == Shape.Circle)
                {
                    worldTMP.DeformChunk(selectedShape, int.Parse(cSize.text), double.Parse(cMaxValue.text), position);
                }
            }

        }
    }
}
