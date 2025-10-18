using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StarData {
    public string source_id;
    public float x, y, z;
    public float phot_g_mean_mag;
    public float bp_rp;
    public float radius_rsun;
}
[Serializable]
public class StarDataList { public List<StarData> stars; }

public class StarParticleLoader : MonoBehaviour
{
    public TextAsset jsonFile;           // pek til gaia_converted.json
    public float positionScale = 0.0001f;     // pc -> world units (la stå 1 først)
    public float sizeScale = 2f;      // juster visuell størrelse

    ParticleSystem ps;
    ParticleSystem.Particle[] buffer;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
        if (ps == null) ps = gameObject.AddComponent<ParticleSystem>();
    }

    void Start()
    {
        if (jsonFile == null) { Debug.LogWarning("StarParticleLoader: jsonFile not assigned"); return; }

        // Vårt JSON-format er { "stars": [...] }
        var list = JsonUtility.FromJson<StarDataList>(jsonFile.text);
        if (list == null || list.stars == null || list.stars.Count == 0)
        {
            Debug.LogWarning("StarParticleLoader: no stars in file"); return;
        }

        int n = list.stars.Count;
        buffer = new ParticleSystem.Particle[n];

        for (int i = 0; i < 1000; i++) // bare 1000 for test
        {
            // plasser dem i en liten kube rundt origo
            Vector3 p = new Vector3(
                UnityEngine.Random.Range(-50f, 50f),
                UnityEngine.Random.Range(-50f, 50f),
                UnityEngine.Random.Range(-50f, 50f)
            );

            var par = new ParticleSystem.Particle
            {
                position = p,
                startSize = 2f,
                startColor = Color.white,
                remainingLifetime = Mathf.Infinity
            };
            buffer[i] = par;
        }


        // push til systemet
        ps.SetParticles(buffer, buffer.Length);
        ps.Play();

        Debug.Log($"StarParticleLoader: spawned {n} particles.");
    }

    Color StarColorFromBPRP(float bp_rp)
    {
        if (bp_rp < 0.3f) return new Color(0.6f, 0.8f, 1.0f);   // blå
        if (bp_rp < 0.8f) return new Color(1.0f, 1.0f, 1.0f);   // hvit
        if (bp_rp < 1.2f) return new Color(1.0f, 1.0f, 0.85f);  // gulhvit
        if (bp_rp < 1.6f) return new Color(1.0f, 0.85f, 0.6f);  // oransje
        return new Color(1.0f, 0.6f, 0.5f);                     // rød
    }
}