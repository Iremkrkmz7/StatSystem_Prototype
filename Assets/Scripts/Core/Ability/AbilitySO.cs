using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum AbilityType
{
    Dash,
    Shield,
    AoE,
    Heal
}
[CreateAssetMenu(fileName = "New Ability", menuName = "Ability/Ability")]
public class AbilitySO : ScriptableObject
{
    public string      AbilityName;
    public AbilityType Type;
    public KeyCode     ActivationKey = KeyCode.Q;
    public float        Cooldown      = 5f;
    public float        Value         = 10f;
    public float        Duration      = 0f;  
    
    public GameObject VFXPrefab;
}
