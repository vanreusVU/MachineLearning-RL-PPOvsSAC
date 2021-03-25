using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.MLAgents;
using UnityEngine;

public struct HeatMapStruct
{
    public float heat;
    public int timesSpawned;

    public HeatMapStruct(float myHeat, int mySpawned)
    {
        this.heat = myHeat;
        this.timesSpawned = mySpawned;
    }
}
public class MasterScene : MonoBehaviour
{
    [HideInInspector]
    public HeatMapStruct[] heatMap;
    public float initHeat = 1.0f;
    private bool allreadyInit = false;
    public TextMeshProUGUI heatText;
    
    public void initHeatMap(int numSpawn)
    {
        if(allreadyInit)
            return;
        
        allreadyInit = true;
        heatMap = new HeatMapStruct[numSpawn];
        for (int i = 0; i < numSpawn; i++)
        {
            heatMap[i].heat = initHeat;
            heatMap[i].timesSpawned = 0;
        }

        
    }
    
    public void updateHeatUI()
    {
        string text = "";
        for(int i = 0; i < heatMap.Length; i++)
        {
            text += i + ": " + heatMap[i].heat.ToString("F3") + " | " + heatMap[i].timesSpawned + "\n";
            var statsRecorder = Academy.Instance.StatsRecorder;
            statsRecorder.Add("HeatMap_Heat:SpawnLoc_" + i,heatMap[i].heat);
            statsRecorder.Add("HeatMap_TimesSpawned:SpawnLoc_" + i,heatMap[i].timesSpawned);
        }
        
        heatText.text = text;
    }
    
    public void resetHeat()
    {
        for (int i = 0; i < heatMap.Length; i++)
        {
            heatMap[i].heat = 1f;
        }
    }
    
    public float getTotalHeat()
    {
        float tempHeat = 0.0f;
        foreach (HeatMapStruct heat in heatMap) // Get total heat
        {
            tempHeat += heat.heat;
        }

        return tempHeat;
    }
    
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
