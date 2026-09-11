#if UNITY_EDITOR
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEngine;

// DATA-DRIVEN DENGE PIPELINE - Excel (GameBalance.xlsx) -> CSV export ->
// bu menu ile oyuna aktar.
//
// KULLANIM:
//  1) GameBalance.xlsx'te degerleri ayarla.
//  2) Config sayfasi:  Dosya > Farkli Kaydet > CSV  ->  proje kokune
//     "balance_global.csv" adiyla kaydet (A sutunu key, B sutunu value).
//  3) Enemies sayfasi (istege bagli): ust taraftaki stat tablosunu ("Stat |
//     BasicEnemy | BomberSkeleton") ayni sekilde "balance_enemies.csv" olarak
//     kaydet.
//  4) Unity:  Tools > Balance > Import ALL from CSV.
//
// Dosyalar proje KOKUNDE aranir (Assets'in bir ust klasoru - GameBalance.xlsx
// ile ayni yer).
public static class BalanceImporter
{
    const string GlobalCsv  = "balance_global.csv";
    const string EnemiesCsv = "balance_enemies.csv";

    static string ProjectRoot => Directory.GetParent(Application.dataPath).FullName;

    [MenuItem("Tools/Balance/Import ALL from CSV", priority = 0)]
    public static void ImportAll()
    {
        int total = 0;
        total += ImportGlobalInternal(false);
        total += ImportEnemiesInternal(false);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Balance Import",
            total > 0 ? $"{total} deger guncellendi." : "Guncellenen deger yok (CSV bulundu mu? Format dogru mu?).", "OK");
    }

    [MenuItem("Tools/Balance/Import Global only", priority = 20)]
    public static void ImportGlobal()
    {
        int n = ImportGlobalInternal(true);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Balance Import", $"Global: {n} deger guncellendi.", "OK");
    }

    [MenuItem("Tools/Balance/Import Enemies only", priority = 21)]
    public static void ImportEnemies()
    {
        int n = ImportEnemiesInternal(true);
        AssetDatabase.SaveAssets(); AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Balance Import", $"Enemies: {n} deger guncellendi.", "OK");
    }

    [MenuItem("Tools/Balance/Export CURRENT values to CSV", priority = 40)]
    public static void ExportCurrent()
    {
        var cfg = LoadConfig();
        if (cfg == null) { EditorUtility.DisplayDialog("Balance", "Assets/Resources/BalanceConfig.asset yok.", "OK"); return; }

        var g = new System.Text.StringBuilder();
        g.AppendLine("key,value");
        foreach (var f in typeof(BalanceConfig).GetFields(BindingFlags.Public | BindingFlags.Instance))
            g.AppendLine($"{f.Name},{Convert.ToString(f.GetValue(cfg), CultureInfo.InvariantCulture)}");
        File.WriteAllText(Path.Combine(ProjectRoot, GlobalCsv), g.ToString());

        var e = new System.Text.StringBuilder();
        var basics  = FindEnemySO("BasicEnemy");
        var bombers = FindEnemySO("BomberSkeleton");
        e.AppendLine("Stat,BasicEnemy,BomberSkeleton");
        foreach (var f in typeof(EnemyDataSO).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (f.FieldType != typeof(float) && f.FieldType != typeof(int) && f.FieldType != typeof(bool)) continue;
            e.AppendLine($"{f.Name},{V(basics, f)},{V(bombers, f)}");
        }
        File.WriteAllText(Path.Combine(ProjectRoot, EnemiesCsv), e.ToString());

        EditorUtility.DisplayDialog("Balance Export",
            $"Yazildi:\n{GlobalCsv}\n{EnemiesCsv}\n({ProjectRoot})", "OK");
    }

    static string V(EnemyDataSO so, FieldInfo f) =>
        so == null ? "" : Convert.ToString(f.GetValue(so), CultureInfo.InvariantCulture);

    // ---------------------------------------------------------------

    static int ImportGlobalInternal(bool warn)
    {
        string path = Path.Combine(ProjectRoot, GlobalCsv);
        if (!File.Exists(path)) { if (warn) Debug.LogWarning($"[Balance] {GlobalCsv} yok: {path}"); return 0; }

        var cfg = LoadConfig();
        if (cfg == null) { Debug.LogWarning("[Balance] BalanceConfig.asset yok."); return 0; }

        int applied = 0;
        foreach (var line in File.ReadAllLines(path))
        {
            var cols = SplitCsv(line);
            if (cols.Length < 2) continue;
            string key = cols[0].Trim();
            if (key.Length == 0 || key.StartsWith("#") || key.Equals("key", StringComparison.OrdinalIgnoreCase) || key.Equals("name", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!TryNum(cols[1], out float val)) continue;
            if (cfg.ApplyKey(key, val)) applied++;
        }
        if (applied > 0) EditorUtility.SetDirty(cfg);
        Debug.Log($"[Balance] Global: {applied} deger guncellendi.");
        return applied;
    }

    static int ImportEnemiesInternal(bool warn)
    {
        string path = Path.Combine(ProjectRoot, EnemiesCsv);
        if (!File.Exists(path)) { if (warn) Debug.LogWarning($"[Balance] {EnemiesCsv} yok: {path}"); return 0; }

        var basics  = FindEnemySO("BasicEnemy");
        var bombers = FindEnemySO("BomberSkeleton");
        if (basics == null && bombers == null) { Debug.LogWarning("[Balance] EnemyDataSO asset'leri bulunamadi."); return 0; }

        int applied = 0;
        foreach (var line in File.ReadAllLines(path))
        {
            var cols = SplitCsv(line);
            if (cols.Length < 2) continue;
            string field = cols[0].Trim();
            if (field.Length == 0 || field.StartsWith("#") || field.Equals("Stat", StringComparison.OrdinalIgnoreCase))
                continue;

            var fi = typeof(EnemyDataSO).GetField(field, BindingFlags.Public | BindingFlags.Instance);
            if (fi == null) continue; // bilinmeyen / hesaplama / referans alani - atla

            if (cols.Length >= 2 && SetField(basics,  fi, cols[1])) applied++;
            if (cols.Length >= 3 && SetField(bombers, fi, cols[2])) applied++;
        }
        if (basics  != null) EditorUtility.SetDirty(basics);
        if (bombers != null) EditorUtility.SetDirty(bombers);
        Debug.Log($"[Balance] Enemies: {applied} deger guncellendi.");
        return applied;
    }

    static bool SetField(EnemyDataSO so, FieldInfo fi, string raw)
    {
        if (so == null) return false;
        raw = raw.Trim();
        try
        {
            if (fi.FieldType == typeof(float)) { if (!TryNum(raw, out float f)) return false; fi.SetValue(so, f); return true; }
            if (fi.FieldType == typeof(int))   { if (!TryNum(raw, out float f)) return false; fi.SetValue(so, Mathf.RoundToInt(f)); return true; }
            if (fi.FieldType == typeof(bool))
            {
                bool b = raw == "1" || raw.Equals("true", StringComparison.OrdinalIgnoreCase)
                                    || raw.Equals("yes", StringComparison.OrdinalIgnoreCase)
                                    || raw.Equals("evet", StringComparison.OrdinalIgnoreCase);
                fi.SetValue(so, b); return true;
            }
            if (fi.FieldType == typeof(string)) { fi.SetValue(so, raw); return true; }
        }
        catch { }
        return false;
    }

    // ---------- helpers ----------

    static BalanceConfig LoadConfig()
    {
        var guids = AssetDatabase.FindAssets("t:BalanceConfig");
        return guids.Length == 0 ? null
            : AssetDatabase.LoadAssetAtPath<BalanceConfig>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    static EnemyDataSO FindEnemySO(string assetName)
    {
        foreach (var guid in AssetDatabase.FindAssets($"{assetName} t:EnemyDataSO"))
        {
            var p = AssetDatabase.GUIDToAssetPath(guid);
            if (Path.GetFileNameWithoutExtension(p).Equals(assetName, StringComparison.OrdinalIgnoreCase))
                return AssetDatabase.LoadAssetAtPath<EnemyDataSO>(p);
        }
        return null;
    }

    static bool TryNum(string s, out float v) =>
        float.TryParse(s.Trim().Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out v);

    // Basit CSV bolucu - tirnakli alanlari ("a,b") tek parca tutar.
    static string[] SplitCsv(string line)
    {
        var res = new System.Collections.Generic.List<string>();
        bool q = false; var cur = new System.Text.StringBuilder();
        foreach (char c in line ?? "")
        {
            if (c == '"') q = !q;
            else if (c == ',' && !q) { res.Add(cur.ToString()); cur.Clear(); }
            else cur.Append(c);
        }
        res.Add(cur.ToString());
        return res.ToArray();
    }
}
#endif
