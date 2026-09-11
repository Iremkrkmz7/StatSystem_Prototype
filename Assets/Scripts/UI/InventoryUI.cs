using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
public class InventoryUI : MonoBehaviour
{
   
    [System.Serializable]
    public class SlotUI
    {
        public Image            Icon;
        public TextMeshProUGUI  NameText;
        public AbilitySlotUI    AbilitySlot;
    }

    [SerializeField] SlotUI[] slots;
    [SerializeField] Sprite   emptySlotSprite;
    [SerializeField] TextMeshProUGUI diamondText;

    InventorySystem _inventory;
    AbilityController _abilityController;

    void Start()
    {
        _inventory = FindFirstObjectByType<InventorySystem>();
        _abilityController = FindFirstObjectByType<AbilityController>();

        if (_inventory != null)
        {
            _inventory.OnInventoryUpdated += Refresh;
            _inventory.OnDiamondsChanged += UpdateDiamondText;
    }
        Refresh();
        UpdateDiamondText();
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.Q)) _abilityController.TryActivateAbilityByType(AbilityType.Dash);
        if(Input.GetKeyDown(KeyCode.E)) UseItemByType(ItemType.WeaponPart);
        if(Input.GetKeyDown(KeyCode.R)) UseItemByType(ItemType.AoEItem);
    }
    
void UseItemByType(ItemType type)
{
    if (_inventory == null || _abilityController == null) return;

    foreach (var s in slots)
    if(s?.AbilitySlot != null && s.AbilitySlot.IsUsed) return;

    for (int i = 0; i < slots.Length; i++)
    {
        var item = _inventory.GetSlot(i);
        if (item != null && item.Type == type)
        {
            if (i < slots.Length && slots[i]?.AbilitySlot != null)
            {
              AbilityType abilityType = type == ItemType.WeaponPart ? AbilityType.Shield : AbilityType.AoE;
              float duration = _abilityController.GetAbilityDuration(abilityType);
              slots[i].AbilitySlot.UseSlot(duration);
            }
                _inventory.ConsumeSlot(i);

            if (type == ItemType.WeaponPart)
                _abilityController.TryActivateAbilityByType(AbilityType.Shield);
            else if (type == ItemType.AoEItem)
                _abilityController.TryActivateAbilityByType(AbilityType.AoE);
            return;
        }
    }
}

    void Refresh()
    {
        if (_inventory == null) return;
        for (int i = 0; i < slots.Length; i++)
        {
            var item = _inventory.GetSlot(i);
            var slot = slots[i];
            if (slot == null) continue;

            bool filled = item != null;
            if(slot.Icon)
            {
                if (slot.AbilitySlot == null || !slot.AbilitySlot.IsUsed)
                {
            slot.Icon.sprite = filled ? item.Icon : emptySlotSprite;
            slot.Icon.color  = filled ? Color.white : new Color(1,1,1,0.3f);
            }
            if (slot.AbilitySlot != null && filled)
           {
            string key = item.Type == ItemType.WeaponPart ? "E" : "R";
            if (!slot.AbilitySlot.IsUsed)
            slot.AbilitySlot.SetItem(item.Icon, key);
            }
         }
            if(slot.NameText)
            {
           if (!filled)
                slot.NameText.text = "";
            else if (item.Type == ItemType.WeaponPart)
                slot.NameText.text = "Press E";
            else if (item.Type == ItemType.AoEItem)
                slot.NameText.text = "Press R";
            else
                slot.NameText.text = item.ItemName;
        }
    }
   }
   

    void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnInventoryUpdated -= Refresh;
            _inventory.OnDiamondsChanged -= UpdateDiamondText;
    }
    void UpdateDiamondText()
{
    if (_inventory != null && diamondText != null)
    {
        diamondText.text = _inventory.Diamonds.ToString(); 
    }
}
}

