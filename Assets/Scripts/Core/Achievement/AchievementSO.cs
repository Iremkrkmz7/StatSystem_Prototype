using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Achievement", menuName = "Achievement/Achievement")]
public class AchievementSO : ScriptableObject
{
    public string AchievementName;
    public string Description;
    public Sprite Icon;
    public string TrackedKey;
    public int    RequiredCount;

     [HideInInspector] public bool IsUnlocked;

    void OnEnable() => IsUnlocked = false;
}
