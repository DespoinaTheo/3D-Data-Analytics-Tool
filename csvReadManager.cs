using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;

public class CSVReaderManager : MonoBehaviour
{
    [Header("File Settings")]
    [Tooltip("Σύρετε και αφήστε (Drag & Drop) το αρχείο CSV εδώ από το Project window.")]
    public TextAsset csvFile; 

    // Λίστα που χρησιμοποιεί απευθείας το δικό σου StockData
    private List<StockData> allStocksData = new List<StockData>();

    void Awake()
    {
        LoadCSV();
    }

    void LoadCSV()
    {
        if (csvFile == null)
        {
            Debug.LogError("CSVReaderManager: Δεν έχετε σύρει το αρχείο CSV στο πεδίο Csv File του Inspector!");
            return;
        }

        // Διαβάζουμε το κείμενο απευθείας από τη μνήμη του TextAsset (Δεν χρειάζεται filePath!)
        using (StringReader reader = new StringReader(csvFile.text))
        {
            bool isHeader = true;
            while (reader.Peek() != -1)
            {
                string line = reader.ReadLine();
                if (isHeader) { isHeader = false; continue; }

                string[] values = line.Split(',');
                if (values.Length < 11) continue;

                try
                {
                    StockData data = new StockData();
                    
                    data.date = values[0].Trim();
                    data.ticker = values[1].Trim();
                    data.sector = values[2].Trim();
                    data.mCap = int.Parse(values[3].Trim());
                    data.weight = float.Parse(values[4].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    data.totalReturn = float.Parse(values[5].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    data.volatility = float.Parse(values[6].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    data.drawdown = float.Parse(values[7].Trim(), System.Globalization.CultureInfo.InvariantCulture); 
                    data.divYield = float.Parse(values[8].Trim(), System.Globalization.CultureInfo.InvariantCulture);  
                    data.posx = float.Parse(values[9].Trim(), System.Globalization.CultureInfo.InvariantCulture);
                    data.posz = float.Parse(values[10].Trim(), System.Globalization.CultureInfo.InvariantCulture);

                    allStocksData.Add(data);
                }
                catch (Exception e)
                {
                    Debug.LogWarning("Error parsing CSV line: " + e.Message);
                }
            }
        }
        Debug.Log("CSV loaded successfully via Inspector. Total rows: " + allStocksData.Count);
    }

    // Η συνάρτηση που φιλτράρει και επιστρέφει τη λίστα StockData χωρίς σφάλματα μετατροπής
    public List<StockData> FilterDataByMonth(int month, int year)
    {
        List<StockData> filtered = new List<StockData>();

        foreach (var row in allStocksData)
        {
            string[] dateParts = row.date.Split('-');
            if (dateParts.Length >= 2)
            {
                int rowYear = int.Parse(dateParts[0]);
                int rowMonth = int.Parse(dateParts[1]);

                if (rowMonth == month && rowYear == year)
                {
                    filtered.Add(row);
                }
            }
        }
        return filtered;
    }
}