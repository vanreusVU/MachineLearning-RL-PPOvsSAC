using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using TMPro.Examples;
using UnityEngine;
using UnityEngine.SceneManagement;

[System.Serializable]
public struct SpawnLocation
{ 
    public GameObject spawnLocation;
    public BlockRewardScript[] platformsToReward;
    public float platformReward;
    [HideInInspector]
    public float heat;
}

public class EnvironmentManager : MonoBehaviour
{
    [HideInInspector]
    public GameObject[] agents;
    [HideInInspector]
    public GameObject target;

    [Header("Heat Settings")]
    public float heatRecution = 0.005f;

    [Header("Scene Settings")]
    public int numberOfAgentsToSpawn = 5; // How many agents to spawn per generation
    public float distanceFromCenter = 5.0f; // The spawn location for the agents 
    public GameObject playerPrefab;
    public GameObject targetPrefab;
    public SpawnLocation[] spawnLocations; // Locations that the target will spawn at
    public float timePerGeneration = 5.0f; // How long to wait to reset the scene
    public SpriteRenderer Indicator;
    public TextMeshPro rewardText;
    public TextMeshPro stepCountText;
    public TextMeshPro generationText;
    


    private MasterScene _heatScript;
    private int _randomPlatform;

    public void ReduceTargetHeat()
    {
        if (_heatScript.heatMap[_randomPlatform].heat <= 0.5f)
        {
            _heatScript.heatMap[_randomPlatform].heat = 0.5f;
        }
        else
        {
            _heatScript.heatMap[_randomPlatform].heat -= heatRecution;
        }
        
        _heatScript.updateHeatUI();
    }

    public void RandomSpawnLocation()
    {
        float totalHeat = _heatScript.getTotalHeat();
        Debug.Log("Total Heat = " + totalHeat);
        float randHeatSelection = Random.Range(0f, totalHeat);
        Debug.Log("RandomHeatSelection = " + randHeatSelection);
        float prevHeat = 0f;
        for (int i = 0; i <  _heatScript.heatMap.Length; i++) // Choose 
        {
            if (randHeatSelection >= prevHeat && randHeatSelection < prevHeat +  _heatScript.heatMap[i].heat)
            {
                _randomPlatform = i;
                break;
            }
            prevHeat +=  _heatScript.heatMap[i].heat;
        }

        _heatScript.heatMap[_randomPlatform].timesSpawned++;
        Debug.Log("RandomPlatform: " + _randomPlatform.ToString());
    }
    public void SetupScene()
    {
        DefaultColor();
        
        target.transform.parent = this.gameObject.transform; // Set target as the child object of the scene
        
        RandomSpawnLocation();
        
        
        foreach (SpawnLocation spawnLocation in spawnLocations) // Reset Everything
        {
            foreach (BlockRewardScript blockRewardScript in spawnLocation.platformsToReward)
            {
                blockRewardScript.ResetPlatform();
            }
        }
        foreach (BlockRewardScript blockRewardScript in spawnLocations[_randomPlatform].platformsToReward) // Activate the current ones
        {
            blockRewardScript.isActive = true;
            blockRewardScript.reward = spawnLocations[_randomPlatform].platformReward;
        }
        
        target.transform.localPosition = spawnLocations[_randomPlatform].spawnLocation.transform.localPosition; // Sets a new location for the target
        

        //StartCoroutine( sceneTimer());
    }

    public void SpawnAgent()
    {
        agents = new GameObject[numberOfAgentsToSpawn]; // Init the array
        target = Instantiate(targetPrefab, new Vector3(-5,-5,-5), Quaternion.identity); // Create the target object
        for (int i = 0; i < 1; i++) // Create the agents
        {
            agents[i] = Instantiate(playerPrefab) as GameObject;
            var bobController = agents[i].GetComponent<BobController>(); // Create a reference to the BobController Script
            bobController.parentScene = this; // Set the parent scene of the bob to this script
            bobController.rewardValue = rewardText;
            bobController.stepValue = stepCountText;
            bobController.episodesValue = generationText;
            bobController.targetEnemy = target; // Set the agents target to the spawned target
            agents[i].transform.parent = this.gameObject.transform; // Set agent as the child object of the scene
            agents[i].transform.localPosition = new Vector3(distanceFromCenter * -1, 0, 0);
        }
        foreach (SpawnLocation loc in spawnLocations) // Reset Everything
        {
            foreach (BlockRewardScript blockRewardScript in loc.platformsToReward)
            {
                blockRewardScript.parentScene = this;
                blockRewardScript.agent = agents[0].GetComponent<BobController>();
            }
        }
    }
    

    void Start() // This is like the main() function. This will be executed first
    {
        
        _heatScript = Object.FindObjectOfType<MasterScene>();

        _heatScript.initHeatMap(spawnLocations.Length);
        _heatScript.resetHeat();
        _heatScript.updateHeatUI();
        
        SpawnAgent();
        //SetupScene();
    }
    
    IEnumerator sceneTimer()
    {
        yield return new WaitForSeconds(timePerGeneration);
        for (int i = 0; i < numberOfAgentsToSpawn; i++) // Kill the remaining agents by sending the time out signal
        {
            if (agents[i] != null)
            {
                var bobController = agents[i].GetComponent<BobController>();
                bobController.timedOut();
            }
        }
        
        yield return new WaitForSeconds(0.5f);
        SetupScene(); // Reset the scene and pass on to the new generation
    }

    public void WinColor()
    {
        Indicator.color = Color.green;
    }
    
    public void LooseColor()
    {
        Indicator.color = Color.red;
    }
    
    public void DefaultColor()
    {
        Indicator.color = Color.gray;
    }
}
