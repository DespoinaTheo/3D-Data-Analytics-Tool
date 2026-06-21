using System.Collections;
using System.Collections.Generic;
using Microsoft.VisualBasic;
using UnityEngine;

public class TreeColliderBridge : MonoBehaviour 
{
    public GameObject targetTree;
    public StockData myData;
}

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
        // Μένει κενό καθώς το αρχικό Spawn το αναλαμβάνει ο Slider στην εκκίνηση
    }

    // Μέθοδος που καλείται από τον TimeSliderManager για ανανέωση/spawn των δέντρων
    public void UpdateForestVisuals(List<StockData> monthlyData)
    {
        foreach (StockData data in monthlyData)
        {
            // Αν το δέντρο υπάρχει, άλλαξε μόνο τα visuals του
            if (spawnedTrees.ContainsKey(data.ticker))
            {
                GameObject targetTree = spawnedTrees[data.ticker];
                ApplyVisuals(targetTree, data);

                if (treeInteractions.ContainsKey(data.ticker))
                {
                    treeInteractions[data.ticker].myData = data;
                }
            }
            // Αν δεν υπάρχει, κάντο Spawn
            else
            {
                InitializeAndSpawnTree(data);
            }
        }
    }

    void InitializeAndSpawnTree(StockData data)
    {
        finalZ = data.posz * multiplier2;

        if (data.posx >= 0) { finalX = data.posx * multiplier1 + roadWidth; } 
        else { finalX = data.posx * multiplier1 - roadWidth; }

        float k = treeSize(data);
        Vector3 desiredPosition = new Vector3(finalX, 0, finalZ);
        Vector3 finalPosition = GetValidPosition(desiredPosition, k);

        GameObject prefab = leafNumber(data); 
        GameObject newTree = Instantiate(prefab, finalPosition, Quaternion.identity);

        newTree.name = "Tree_" + data.ticker;
        newTree.transform.localScale = new Vector3(k, k, k);

        spawnedTrees.Add(data.ticker, newTree);

        CreateStaticCollider(newTree, data, finalPosition, k);
        ApplyVisuals(newTree, data);
    }

    void ApplyVisuals(GameObject tree, StockData data)
    {
        leafColor(tree, data);   
        barkDamage(tree, data);  
        fruits(tree, data);      
        
        treeSway sway = tree.GetComponent<treeSway>();
        if (sway == null) sway = tree.AddComponent<treeSway>();
        sway.speed = data.volatility * 2f; 
        sway.amount = data.volatility * 0.2f;
    }

    Vector3 GetValidPosition(Vector3 startingPos, float treeScale)
    {
        Vector3 currentCheckPos = startingPos;
        bool hasOverlap = true;
        int safetyCounter = 0; 
        Vector3 boxHalfExtents = new Vector3(4f * treeScale, 10f, 4f * treeScale);

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

    void CreateStaticCollider(GameObject tree, StockData data, Vector3 pos, float scale)
    {
        GameObject colObj = new GameObject("Collider_" + data.ticker);
        colObj.transform.position = pos;
    
        BoxCollider box = colObj.AddComponent<BoxCollider>();
        box.isTrigger = false; 

        TreeInteraction interaction = colObj.AddComponent<TreeInteraction>();
        interaction.myData = data;
        treeInteractions.Add(data.ticker, interaction);

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

        TreeColliderBridge bridge = colObj.AddComponent<TreeColliderBridge>();
        bridge.targetTree = tree;
        bridge.myData = data;
    }

    float treeSize(StockData data)
    {
        if(data.mCap == 1) return 1.8f;
        if (data.mCap == 2) return 1.5f;
        if (data.mCap == 3) return 1.2f;
        if (data.mCap == 4) return 0.9f;
        if (data.mCap == 5) return 0.7f;
        return 0.5f;
    }

    GameObject leafNumber(StockData data)
    {
       if (data.weight > 0.1f) return treePref1; 
       else if (data.weight > 0.01f) return treePref2; 
       else return treePref3; 
    }
    
    void leafColor(GameObject newTree, StockData data)
    {
        Renderer treeRender = newTree.GetComponentInChildren<Renderer>(); 
        if (treeRender == null) return;

        Material[] treeMaterials = treeRender.materials; 
        if (treeMaterials.Length > 1)
        {
            Color customOrange = new Color(1f, 0.5f, 0f);

            // --- ΡΕΑΛΙΣΤΙΚΑ ΟΡΙΑ ΓΙΑ ΜΗΝΙΑΙΕΣ ΑΠΟΔΟΣΕΙΣ ---

            if (data.totalReturn >= 0.15f) // Μεγάλη Άνοδος (Από +15% έως +30%+)
            {
                // Ομαλή μετάβαση από Κίτρινο σε έντονο Πράσινο
                float intensity = Mathf.InverseLerp(0.15f, 0.30f, data.totalReturn);
                treeMaterials[1].color = Color.Lerp(Color.yellow, Color.green, intensity); 
            }
            else if (data.totalReturn >= 0.05f) // Μικρή Άνοδος (Από +5% έως +15%)
            {
                // Ομαλή μετάβαση από Πορτοκαλί σε Κίτρινο
                float intensity = Mathf.InverseLerp(0.05f, 0.15f, data.totalReturn);
                treeMaterials[1].color = Color.Lerp(customOrange, Color.yellow, intensity); 
            }
            else if (data.totalReturn > 0f) // Οριακή Άνοδος (Από 0% έως +5%)
            {
                // Ομαλή μετάβαση από Κόκκινο σε Πορτοκαλί
                float intensity = Mathf.InverseLerp(0f, 0.05f, data.totalReturn);
                treeMaterials[1].color = Color.Lerp(Color.red, customOrange, intensity);
            }
            else // Ζημιά / Πτώση (Κάτω από 0%)
            {
                // Μετάβαση από Κόκκινο σε Βαθύ Μπορντό (αντί για μαύρο)
                float lossIntensity = Mathf.InverseLerp(0f, -0.30f, data.totalReturn);
                Color darkRed = new Color(0.35f, 0f, 0f); 
                treeMaterials[1].color = Color.Lerp(Color.red, darkRed, lossIntensity);
            }
        }
        treeRender.materials = treeMaterials;
    }
    void barkDamage(GameObject newTree, StockData data) 
    {
        GameObject smoke1 = newTree.transform.Find("mat3")?.gameObject;
        GameObject smoke2 = newTree.transform.Find("mat4")?.gameObject;
        Renderer treeRender = newTree.GetComponentInChildren<Renderer>();
        if (treeRender == null) return;

        Material[] treeMaterials = treeRender.materials;
        if (treeMaterials.Length > 0)
        {
            // --- ΝΕΑ ΡΕΑΛΙΣΤΙΚΑ ΟΡΙΑ ΓΙΑ MAX DRAWDOWN (MDD) ---

            if (data.drawdown >= 0.35f) 
            {
                // Μεγάλο Κραχ / Ύφεση (MDD > 35%)
                treeMaterials[0] = barkMaterial4;
                smoke1.SetActive(false);
                smoke2.SetActive(true); 
            }
            else if (data.drawdown >= 0.20f) 
            {
                // Σημαντική υποχώρηση (MDD 20% - 35%)
                treeMaterials[0] = barkMaterial3;
                smoke1.SetActive(true);
                smoke2.SetActive(false);
            }
            else if (data.drawdown >= 0.10f) 
            {
                // Ήπια διόρθωση / Φυσιολογικό ρίσκο (MDD 10% - 20%)
                treeMaterials[0] = barkMaterial2;
                smoke2.SetActive(false);
                smoke1.SetActive(false);  
            }
            else 
            {
                // Υψηλή σταθερότητα / Ασφαλές καταφύγιο (MDD < 10%)
                treeMaterials[0] = barkMaterial1;
                smoke2.SetActive(false);
                smoke1.SetActive(false); 
            }
        }
        treeRender.materials = treeMaterials;
    }
    
    void fruits(GameObject newTree, StockData data)
    {
        GameObject lvl1 = newTree.transform.Find("model1")?.gameObject;
        GameObject lvl2 = newTree.transform.Find("model2")?.gameObject;
        GameObject lvl3 = newTree.transform.Find("model3")?.gameObject;
        GameObject lvl4 = newTree.transform.Find("model4")?.gameObject;

        if(lvl1) lvl1.SetActive(false);
        if(lvl2) lvl2.SetActive(false);
        if(lvl3) lvl3.SetActive(false);
        if(lvl4) lvl4.SetActive(false);
        
        if (data.divYield >= 0.06f) { if(lvl4) lvl4.SetActive(true); }
        else if (data.divYield >= 0.03f && data.divYield < 0.06f) { if(lvl3) lvl3.SetActive(true); }
        else if (data.divYield >= 0.01f && data.divYield < 0.03f) { if(lvl2) lvl2.SetActive(true); }
        else if (data.divYield > 0f && data.divYield < 0.01f) { if(lvl1) lvl1.SetActive(true); }
    }
} 