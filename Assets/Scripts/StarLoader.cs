using System.Collections.Generic;
using System.IO;
using UnityEngine;

[System.Serializable]
public class StarData
{
    public string source_id;
    public float x, y, z;
    public float phot_g_mean_mag;
    public float bp_rp;
    public float radius_rsun;   // <-- NYTT
}

[System.Serializable]
public class StarDataList
{
    public List<StarData> stars;
}

public class StarLoader : MonoBehaviour
{
    public TextAsset jsonFile;
    public GameObject starPrefab;
    public float positionScale = 1f; // juster for skala

    void Start()
    {
        LoadStars();
    }

    void LoadStars()
    {
        if (jsonFile == null || starPrefab == null)
        {
            Debug.LogWarning("StarLoader: jsonFile or starPrefab not assigned.");
            return;
        }

        // JSON-array → liste
        StarDataList starDataList = JsonUtility.FromJson<StarDataList>(jsonFile.text);
        Debug.Log("StarLoader: Loaded " + starDataList.stars.Count + " stars");
        foreach (StarData star in starDataList.stars)
        {
            Vector3 pos = new Vector3(star.x, star.y, star.z) * positionScale;
            GameObject starGO = Instantiate(starPrefab, pos, Quaternion.identity, this.transform);

            CelestialBody cb = starGO.GetComponent<CelestialBody>();
            if (cb != null)
            {
                cb.bodyName = star.source_id;
                cb.luminosity = star.phot_g_mean_mag;

                // Sett radius (solradier)
                float starRadius = star.radius_rsun;
                if (starRadius <= 0 || float.IsNaN(starRadius)) starRadius = 1f; // fallback
                cb.radius = starRadius;
            }

            // Skaler objektet i scenen (alltid mulig, uavhengig av cb)
            float visualScale = (cb != null ? cb.radius : 1f) * 0.1f;
            starGO.transform.localScale = Vector3.one * visualScale;

            // Farge og lysstyrke
            Color color = StarColorFromBPRP(star.bp_rp);
            float brightness = Mathf.Clamp01((1f - star.phot_g_mean_mag + 5f) / 10f);

            Renderer rend = starGO.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                MaterialPropertyBlock block = new MaterialPropertyBlock();
                block.SetColor("_BaseColor", color);
                block.SetColor("_EmissionColor", color * brightness * 10f);
                rend.SetPropertyBlock(block);
            }
        }


    }

    Color StarColorFromBPRP(float bp_rp)
    {
        if (bp_rp < 0.3f) return new Color(0.6f, 0.8f, 1.0f); // blå
        if (bp_rp < 0.8f) return new Color(1.0f, 1.0f, 1.0f); // hvit
        if (bp_rp < 1.2f) return new Color(1.0f, 1.0f, 0.8f); // gulhvit
        if (bp_rp < 1.6f) return new Color(1.0f, 0.9f, 0.6f); // oransje
        return new Color(1.0f, 0.7f, 0.5f); // rød
    }
}
