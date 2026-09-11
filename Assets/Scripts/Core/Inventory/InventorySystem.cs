using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class InventorySystem : MonoBehaviour
{
    [SerializeField] int slotCount = 6;
    [SerializeField] AudioClip coinSound;
    public event Action OnInventoryUpdated;
    int _diamonds = 0;
    public int Diamonds => _diamonds;
    public event Action OnDiamondsChanged;
    ItemSO[] _slots;
    SCharacterStats _stats;
    public int SlotCount => _slots.Length;
    void Awake()
    {
        _slots = new ItemSO[slotCount];
        _stats = GetComponent<SCharacterStats>();
        // ONEMLI: _diamonds BU RUN'da toplanan sayi - GameOverUI'daki
        // "Diamonds: X" ve BestDiamonds rekoru hala BUNA gore, 0'dan
        // basliyor (ESKI davranis DEGISMEDI). Kalici/BIRIKEN toplam
        // (butun run'lar boyunca) AYRI olarak SaveSystem.TotalDiamonds'ta
        // tutuluyor - asagida her toplamada ikisi de guncelleniyor.
    }
    public bool AddItem(ItemSO item)
    {
            Debug.Log("AddItem: " + item.ItemName + " Type: " + item.Type);

        if (item.Type == ItemType.HealthPack || item.Type == ItemType.Diamond)
        {

        if (item.Type == ItemType.HealthPack) _stats?.Heal(item.HealthRestoreAmount);
        
            if (item.Type == ItemType.Diamond)
            {
                _diamonds += 1;
                SaveSystem.SetTotalDiamonds(SaveSystem.TotalDiamonds + 1);
                OnDiamondsChanged?.Invoke();
                QuestManager.Instance?.OnDiamondCollected();
                Sfx.PlayAt(coinSound, transform.position);
            }
            OnInventoryUpdated?.Invoke();
            return true;
    }
        for(int i=0; i < 2; i++)
        {
            if(_slots[i] !=null) continue;
            _slots[i] = item;
            Debug.Log("Slot " + i + " doldu: " + item.ItemName);
            OnInventoryUpdated?.Invoke();
            return true;
        }
        Debug.Log("Envanter dolu!");
        return false;
    }
    public void AddDiamond()
   {
    _diamonds++;
    SaveSystem.SetTotalDiamonds(SaveSystem.TotalDiamonds + 1);
    OnDiamondsChanged?.Invoke();
    Sfx.PlayAt(coinSound, transform.position);
   }
    
  
    public ItemSO ConsumeSlot(int index)
    {
        ItemSO item =GetSlot(index);
        if(item != null)
        {
            _slots[index] = null;
            OnInventoryUpdated?.Invoke();
            return item;
        }
        return null;
    }
      public ItemSO GetSlot(int index)
   {
    return index >= 0 && index < _slots.Length ? _slots[index] : null;
    }
}



