using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public enum ItemType
{
    HealthPack,
    WeaponPart,
    ArmorPart,
    BuffItem,
    Collectible,
    Diamond,
    AoEItem  
}
[CreateAssetMenu(fileName = "New Item" , menuName = "Inventory/Item")]
public class ItemSO : ScriptableObject
{
   public string ItemName;
   [TextArea]
   public string Description;
   public Sprite Icon;
   public ItemType Type;

   public float HealthRestoreAmount;
   public BuffSO AppliedBuff;
   public StatType BoostedStat;
   public float StatBoostAmount;
   [Header("3D Model")]
   public GameObject PickupPrefab;

   [Range(0f, 1f)] public float DropChance = 0.3f;
}
