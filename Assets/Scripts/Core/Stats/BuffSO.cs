using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(fileName = "New Buff", menuName = "Stats/Buff")]
public class BuffSO :  ScriptableObject
{

    [Header("Kimlik")]
    public string BuffName;
    public string Description;
    public Sprite Icon;
    public float  Duration;

    [Header("Modifier Listesi")]
    public List<ModifierEntry> Modifiers = new();

    [Serializable]
    public struct ModifierEntry
    {
        public StatType                  StatType;
        public float                     Value;
        public StatModifier.ModifierType ModType;
    }
}
