using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using UnityEngine;

// Class that connects each tree to the corresponding collider
public class TreeColliderBridge : MonoBehaviour 
{
    public GameObject targetTree;
    public StockData myData;
}

// This part of the script is the generator of the forest. It connects stock data to its visual representation as tree features.
public class visManager : MonoBehaviour
{
    [Header("References")]
    public CSVReaderManager csvReaderScript; 
    
    [Header("Corridor Settings")]
    private float finalX;
    private float finalZ;
    public float roadWidth = 20f; 
    public int multiplier1 = 1;
    public int multiplier2 = 1; 

    [Header("Prefabs")]
    public GameObject treePref1;
    public GameObject treePref2;
    public GameObject treePref3;

    [Header("Bark Materials")]
    public Material barkMaterial1;
    public Material barkMaterial2;
    public Material barkMaterial3;
    public Material barkMaterial4;

    private Dictionary<string, GameObject> spawnedTrees = new Dictionary<string, GameObject>();
    private Dictionary<string, TreeInteraction> treeInteractions = new Dictionary<string, TreeInteraction>();

    void Start()
    {
    
    }

    // Main App loop called by TimeSliderManager to update or spawn asset visuals every time the user changes the timeframe.
    public void UpdateForestVisuals(List<StockData> monthlyData)
    {
        foreach (StockData data in monthlyData)
        {
            // Case A: Asset already exists in space, swap visuals to reflect the new given data
            if (spawnedTrees.ContainsKey(data.ticker))
            {
                GameObject targetTree = spawnedTrees[data.ticker];
                ApplyVisuals(targetTree, data);

                if (treeInteractions.ContainsKey(data.ticker))
                {
                    treeInteractions[data.ticker].myData = data;
                }
            }
            // Case B: New asset introduced in the portfolio, new instantiation (for debug)
            else
            {
                InitializeAndSpawnTree(data);
            }
        }
    }

    // Instantiates and configures a tree based on StockData.
    void InitializeAndSpawnTree(StockData data)
    {
        finalZ = data.posz * multiplier2;
        
        // 1. Apply Corridor Logic: Push trees away from the center to create a road
        if (data.posx >= 0) { finalX = data.posx * multiplier1 + roadWidth; } 
        else { finalX = data.posx * multiplier1 - roadWidth; }
        
        // 2. Tree size
        float k = treeSize(data); // Tree size is defined by the stock's MCap

        // 3. Tree Position
        Vector3 desiredPosition = new Vector3(finalX, 0, finalZ);
        Vector3 finalPosition = GetValidPosition(desiredPosition, k); //Resolve spatial boundaries to prevent overlapping asset nodes

        // 4. Tree Instantiation
        GameObject prefab = leafNumber(data); // Tree prefab is based on portfolio weight
        GameObject newTree = Instantiate(prefab, finalPosition, Quaternion.identity);

        newTree.name = "Tree_" + data.ticker;
        
        newTree.transform.localScale = new Vector3(k, k, k); // tree size adaptation

        // 5. Register asset reference within Dictionary memory
        spawnedTrees.Add(data.ticker, newTree);

        // 6. Initialize interaction interfaces and contextual visuals
        CreateStaticCollider(newTree, data, finalPosition, k);
        ApplyVisuals(newTree, data);
    }

    // This method (mapping method) translates financial indexes into tree aesthetics and behavior
    void ApplyVisuals(GameObject tree, StockData data)
    {
        leafColor(tree, data);  // applies total returns
        barkDamage(tree, data);  // applies MDD
        fruits(tree, data);  // applies dividends

        // applies volatility
        treeSway sway = tree.GetComponent<treeSway>();
        if (sway == null) sway = tree.AddComponent<treeSway>();
        sway.speed = data.volatility * 2f; 
        sway.amount = data.volatility * 0.2f;
    }

    // Spatial collision-avoidance sorting function
    Vector3 GetValidPosition(Vector3 startingPos, float treeScale)
    {
        Vector3 currentCheckPos = startingPos;
        bool hasOverlap = true;
        int safetyCounter = 0; 
        Vector3 boxHalfExtents = new Vector3(4f * treeScale, 10f, 4f * treeScale);
        
        // while the position is occupied by another tree object the new tree is beeing moved to z ax until it finds a free position
        while (hasOverlap && safetyCounter < 50)
        {
            Collider[] colliders = Physics.OverlapBox(currentCheckPos, boxHalfExtents, Quaternion.identity);
            hasOverlap = false;

            foreach (Collider col in colliders)
            {
                if (col.gameObject.name.StartsWith("Collider_"))
                {
                    hasOverlap = true;
                    currentCheckPos.z += 6f;
                    break; 
                }
            }
            safetyCounter++;
        }
        return currentCheckPos;
    }

    // Method that creates a collider as a different object
    void CreateStaticCollider(GameObject tree, StockData data, Vector3 pos, float scale)
    {
        // Create a new empty GameObject to act as the interaction hub
        GameObject colObj = new GameObject("Collider_" + data.ticker);
        colObj.transform.position = pos;
    
        BoxCollider box = colObj.AddComponent<BoxCollider>(); // Attaches a new collider
        box.isTrigger = false; 

        // Interaction component is assigned to the corresponding collider object.
        TreeInteraction interaction = colObj.AddComponent<TreeInteraction>();
        interaction.myData = data;
        treeInteractions.Add(data.ticker, interaction);

        // Calculating collider's size based on tree object's size on scene
        Renderer rend = tree.GetComponentInChildren<Renderer>();
        if (rend != null)
        {        
            float thickness = 1f;     
            float heightMult = 1.5f;      
            float centerAdjustment = 0.9f; 

            box.size = new Vector3(
                rend.localBounds.size.x * scale * thickness, 
                rend.localBounds.size.y * scale * heightMult, 
                rend.localBounds.size.z * scale * thickness
            );
        
            Vector3 localCenter = rend.localBounds.center;
            box.center = new Vector3(
                localCenter.x * scale, 
                localCenter.y * scale * centerAdjustment, 
                localCenter.z * scale
            );
        }

        // Add a bridge component to maintain a link between the collider and the visual tree
        TreeColliderBridge bridge = colObj.AddComponent<TreeColliderBridge>();
        bridge.targetTree = tree;
        bridge.myData = data;
    }

    // Tree size depends on the MCap Category of the corresponding Stock
    float treeSize(StockData data)
    {
        if(data.mCap == 1) return 1.8f; // huge MCap
        if (data.mCap == 2) return 1.5f; // large MCap
        if (data.mCap == 3) return 1.2f; // mid MCap
        if (data.mCap == 4) return 0.9f; // small MCap
        if (data.mCap == 5) return 0.7f; // micro MCap
        return 0.5f; // nano MCap
    }

    // It chooses tree prefab based on a stocks portfolio weight
    GameObject leafNumber(StockData data)
    {
       if (data.weight > 0.1f) return treePref1; // overweight - big leaves' #
       else if (data.weight > 0.01f) return treePref2; // neutral - medium leaves' #
       else return treePref3; // underweight - small leaves' #
    }

    // Leaf color depends on Total Returns index (utilizes color.lerp)
    void leafColor(GameObject newTree, StockData data)
    {
        Renderer treeRender = newTree.GetComponentInChildren<Renderer>(); 
        if (treeRender == null) return;

        Material[] treeMaterials = treeRender.materials; 
        if (treeMaterials.Length > 1)
        {
            Color customOrange = new Color(1f, 0.5f, 0f);
            if (data.totalReturn >= 0.15f) // High Outperformance
            {
                // Smooth transition from Yellow to Lush Green
                float intensity = Mathf.InverseLerp(0.15f, 0.30f, data.totalReturn);
                treeMaterials[1].color = Color.Lerp(Color.yellow, Color.green, intensity); 
            }
            else if (data.totalReturn >= 0.05f) // Steady Growth 
            {
                // Smooth transition from Orange to Vibrant Yellow
                float intensity = Mathf.InverseLerp(0.05f, 0.15f, data.totalReturn);
                treeMaterials[1].color = Color.Lerp(customOrange, Color.yellow, intensity); 
            }
            else if (data.totalReturn > 0f) // Marginal Gains 
            {
                // Smooth transition from Danger Red to Warning Orange
                float intensity = Mathf.InverseLerp(0f, 0.05f, data.totalReturn);
                treeMaterials[1].color = Color.Lerp(Color.red, customOrange, intensity);
            }
            else // Fiscal Loss/Negative Returns
            {
                // Smooth progression from Red down into Deep Burgundy
            {
                float lossIntensity = Mathf.InverseLerp(0f, -0.30f, data.totalReturn);
                Color darkRed = new Color(0.35f, 0f, 0f); 
                treeMaterials[1].color = Color.Lerp(Color.red, darkRed, lossIntensity);
            }
        }
        treeRender.materials = treeMaterials;
        }
    }

    // Bark Damage/ Material/ Texture depends on MDD index
    void barkDamage(GameObject newTree, StockData data) 
    {
        GameObject smoke1 = newTree.transform.Find("mat3")?.gameObject;
        GameObject smoke2 = newTree.transform.Find("mat4")?.gameObject;
        Renderer treeRender = newTree.GetComponentInChildren<Renderer>();
        if (treeRender == null) return;

        Material[] treeMaterials = treeRender.materials;
        if (treeMaterials.Length > 0)
        {
            if (data.drawdown >= 0.35f) 
            // High MDD/Risk: Charred Bark + Heavy Smoke FX
            {
                treeMaterials[0] = barkMaterial4;
                smoke1.SetActive(false);
                smoke2.SetActive(true); 
            }
            else if (data.drawdown >= 0.20f)
            // Medium MDD/Risk: Damaged Bark + Warning Smoke FX
            {
                treeMaterials[0] = barkMaterial3;
                smoke1.SetActive(true);
                smoke2.SetActive(false);
            }
            else if (data.drawdown >= 0.10f) 
            // Standard MDD/Risk: Moderate Bark Texture
            {
                treeMaterials[0] = barkMaterial2;
                smoke2.SetActive(false);
                smoke1.SetActive(false);  
            }
            else // Low MDD/Risk: "Healthy" Bark Texture
            {
                treeMaterials[0] = barkMaterial1;
                smoke2.SetActive(false);
                smoke1.SetActive(false); 
            }
        }
        treeRender.materials = treeMaterials;
    }

    // Enables sub-models (fruits) based on Dividend Yield performance.
    void fruits(GameObject newTree, StockData data)
    {
        GameObject lvl1 = newTree.transform.Find("model1")?.gameObject;
        GameObject lvl2 = newTree.transform.Find("model2")?.gameObject;
        GameObject lvl3 = newTree.transform.Find("model3")?.gameObject;
        GameObject lvl4 = newTree.transform.Find("model4")?.gameObject;

        // Reset visual fruit state structures prior to updates
        if(lvl1) lvl1.SetActive(false);
        if(lvl2) lvl2.SetActive(false);
        if(lvl3) lvl3.SetActive(false);
        if(lvl4) lvl4.SetActive(false);

        // Higher dividend yields trigger denser, more colorful fruit sub-models.
        if (data.divYield >= 0.06f) { if(lvl4) lvl4.SetActive(true); } // extreme yield
        else if (data.divYield >= 0.03f && data.divYield < 0.06f) { if(lvl3) lvl3.SetActive(true); } // high yield
        else if (data.divYield >= 0.01f && data.divYield < 0.03f) { if(lvl2) lvl2.SetActive(true); } // standard yield
        else if (data.divYield > 0f && data.divYield < 0.01f) { if(lvl1) lvl1.SetActive(true); } // minimal yield
        // else: No fruits = zero yield
    }
} 
