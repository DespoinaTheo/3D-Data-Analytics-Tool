using System.Collections;
using System.Collections.Generic;
using UnityEngine;


// The script handles mouse-based interaction with tree objects.
// it  is attached to the static colliders generated for each tree to trigger UI updates

public class TreeInteraction : MonoBehaviour
{
    public StockData myData;

    // Unity calles this function when a tree object's  adjusted collider is clicked
    void OnMouseDown()
    {
        if (myData != null)
        {
            
            if (stockUIManager.Instance != null)
            {
                stockUIManager.Instance.ToggleTooltip(myData); //Pass the stock data to the UI Manager
                Debug.Log("Clicked on: " + myData.ticker); // Tracebaks
            }
            else
            {
                Debug.LogWarning("stockUIManager Instance not found!"); // Tracebaks
            }
        }
    }
}