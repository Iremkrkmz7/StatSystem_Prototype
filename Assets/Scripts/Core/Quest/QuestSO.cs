using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum QuestType
{
    KillEnemies,
    CollectDiamonds,
    SurviveSeconds,
    KillWithoutDamage,
    MultiKill
}
[CreateAssetMenu(fileName = "New Quest", menuName = "Quest/Quest")]
public class QuestSO : ScriptableObject
{
     [Header("Kimlik")]
    public string QuestName;
    public string Description;
    public Sprite Icon;

    [Header("Görev")]
    public QuestType Type;
    public int       RequiredAmount;

    [Header("Ödül")]
    public int XPReward;
    public int DiamondReward;

    [HideInInspector] public bool IsCompleted;
    [HideInInspector] public int  CurrentAmount;

    void OnEnable()
    {
        IsCompleted   = false;
        CurrentAmount = 0;
    }
}