// Level atlarken oyuncuya sunulan 3 secenekten birinin tanimi. ScriptableObject
// yerine sade bir C# sinifi olarak tutuluyor - LevelUpUI icindeki sabit
// havuzdan (Pool) rastgele 3 tanesi secilip kart olarak gosteriliyor.
[System.Serializable]
public class UpgradeOption
{
    public string title;
    public string description;
    public StatType stat;
    public StatModifier.ModifierType modType;
    public float value;
    public UnityEngine.Sprite icon; // Duruma gore ikon - LevelUpUI StatType'a gore otomatik atar

    public UpgradeOption(string title, string description, StatType stat, float value,
        StatModifier.ModifierType modType = StatModifier.ModifierType.PercentAdd)
    {
        this.title = title;
        this.description = description;
        this.stat = stat;
        this.value = value;
        this.modType = modType;
    }
}
