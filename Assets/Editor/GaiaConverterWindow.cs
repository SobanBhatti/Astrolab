using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class GaiaCsvConverterWindow : EditorWindow
{
    [Serializable] class StarOut
    {
        public string source_id;
        public float x, y, z;           // parsec
        public float phot_g_mean_mag;   // mag
        public float bp_rp;             // mag
        public float radius_rsun;       // solradier
    }
    [Serializable] class StarOutList { public List<StarOut> stars = new(); }

    private TextAsset csvFile;
    private string outputFileName = "gaia_converted.json";
    private float positionScale = 1f; // pc→units (la stå 1.0 nå)

    [MenuItem("Tools/Astrolab/Convert Gaia CSV")]
    public static void Open() => GetWindow<GaiaCsvConverterWindow>("Convert Gaia CSV");

    void OnGUI()
    {
        GUILayout.Label("Input (Gaia CSV)", EditorStyles.boldLabel);
        csvFile = (TextAsset)EditorGUILayout.ObjectField("CSV file", csvFile, typeof(TextAsset), false);
        outputFileName = EditorGUILayout.TextField("Output file name", outputFileName);
        positionScale = EditorGUILayout.FloatField("Position scale (pc→units)", positionScale);

        using (new EditorGUI.DisabledScope(csvFile == null || string.IsNullOrEmpty(outputFileName)))
        {
            if (GUILayout.Button("Convert"))
            {
                try
                {
                    ConvertCsv();
                    EditorUtility.DisplayDialog("Gaia CSV Converter", "Conversion finished.", "OK");
                    AssetDatabase.Refresh();
                }
                catch (Exception ex)
                {
                    Debug.LogError("Gaia CSV conversion failed: " + ex);
                    EditorUtility.DisplayDialog("Gaia CSV Converter", "Conversion failed. See Console.", "OK");
                }
            }
        }
    }

    void ConvertCsv()
    {
        var lines = SplitLines(csvFile.text);
        if (lines.Count < 2) throw new Exception("CSV seems empty.");

        // Parse header and find column indices (case-insensitive)
        var header = ParseCsvLine(lines[0]);
        int idx_source = FindCol(header, new[] { "source_id" });
        int idx_ra     = FindCol(header, new[] { "ra" });
        int idx_dec    = FindCol(header, new[] { "dec" });
        int idx_plx    = FindCol(header, new[] { "parallax" });
        int idx_gmag   = FindCol(header, new[] { "phot_g_mean_mag" });
        int idx_bprp   = FindCol(header, new[] { "bp_rp" });
        int idx_radius = FindCol(header, new[] {
            "radius_gspphot", "radius_flame", "radius", "r_star", "stellar_radius"
        }, required:false); // noen exports bruker andre navn

        var outList = new StarOutList();

        for (int i = 1; i < lines.Count; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;
            var cols = ParseCsvLine(lines[i]);
            if (cols.Count != header.Count) continue;

            // Safe getters
            bool ok = true;
            string src = GetString(cols, idx_source, ref ok);
            double raDeg = GetDouble(cols, idx_ra, ref ok);
            double decDeg = GetDouble(cols, idx_dec, ref ok);
            double plxMas = GetDouble(cols, idx_plx, ref ok);
            double gmag = GetDouble(cols, idx_gmag, ref ok);
            double bprp = GetDouble(cols, idx_bprp, ref ok);
            double rRsun = (idx_radius >= 0) ? GetDouble(cols, idx_radius, ref ok) : double.NaN;

            if (!ok || plxMas <= 0) continue;

            // Spherical → Cartesian (pc)
            double d_pc = 1000.0 / plxMas;
            double ra = raDeg * Math.PI / 180.0;
            double dec = decDeg * Math.PI / 180.0;
            double cosDec = Math.Cos(dec);

            float x = (float)(d_pc * cosDec * Math.Cos(ra) * positionScale);
            float y = (float)(d_pc * cosDec * Math.Sin(ra) * positionScale);
            float z = (float)(d_pc * Math.Sin(dec) * positionScale);

            // Hvis radius mangler, sett en enkel fallback (kan justeres senere)
            float radius = float.IsNaN((float)rRsun) ? EstimateRadiusFromMag((float)gmag) : (float)rRsun;

            outList.stars.Add(new StarOut {
                source_id = src,
                x = x, y = y, z = z,
                phot_g_mean_mag = (float)gmag,
                bp_rp = (float)bprp,
                radius_rsun = radius
            });
        }

        // Skriv JSON i vårt enkle format
        string json = JsonUtility.ToJson(outList, prettyPrint: false);
        string outPath = Path.Combine(Application.dataPath, "Data", outputFileName);
        Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
        File.WriteAllText(outPath, json);
        Debug.Log($"Wrote {outList.stars.Count} stars to {outPath}");
    }

    // ---------- Helpers ----------
    static List<string> SplitLines(string text)
    {
        var list = new List<string>();
        using (var sr = new StringReader(text))
        {
            string line;
            while ((line = sr.ReadLine()) != null) list.Add(line);
        }
        return list;
    }

    // CSV line parser that handles quotes and commas
    static List<string> ParseCsvLine(string line)
    {
        var res = new List<string>();
        if (line == null) return res;

        var sb = new StringBuilder();
        bool inQuotes = false;
        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"'); i++; // escaped quote
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                res.Add(sb.ToString());
                sb.Length = 0;
            }
            else
            {
                sb.Append(c);
            }
        }
        res.Add(sb.ToString());
        return res;
    }

    static int FindCol(List<string> header, string[] names, bool required = true)
    {
        for (int i = 0; i < header.Count; i++)
        {
            string h = header[i].Trim().Trim('"').ToLowerInvariant();
            foreach (var n in names)
                if (h == n.ToLowerInvariant())
                    return i;
        }
        if (required) throw new Exception("Required column not found: " + string.Join("/", names));
        return -1;
    }

    static string GetString(List<string> cols, int idx, ref bool ok)
    {
        if (idx < 0 || idx >= cols.Count) { ok = false; return ""; }
        return cols[idx].Trim().Trim('"');
    }

    static double GetDouble(List<string> cols, int idx, ref bool ok)
    {
        if (idx < 0 || idx >= cols.Count) { ok = false; return double.NaN; }
        var s = cols[idx].Trim().Trim('"');
        if (string.IsNullOrEmpty(s) || s.Equals("nan", StringComparison.OrdinalIgnoreCase)) return double.NaN;
        if (double.TryParse(s, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var v)) return v;
        // fallback for locale commas
        if (double.TryParse(s.Replace(',', '.'),
            System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out v)) return v;
        ok = false; return double.NaN;
    }

    static float EstimateRadiusFromMag(float gmag)
    {
        // enkel, visuell fallback: lysere stjerner blir større
        // skaler 5–15 mag → ~[1.5, 0.2] R_sun
        float t = Mathf.InverseLerp(15f, 5f, gmag);
        return Mathf.Lerp(0.2f, 1.5f, t);
    }
}