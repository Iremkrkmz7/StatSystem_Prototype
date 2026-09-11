using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    [SerializeField] ItemSO item;
    [Tooltip("Item'in altinda gorunecek yer efekti (orn. FX_LootDrop_Blue). Bu objenin child'i olarak spawn edilir, item toplanip Destroy edilince o da otomatik gider.")]
    [SerializeField] GameObject groundEffectPrefab;
    [Tooltip("Efektin item'e gore Y offset'i - zemine daha yakin dursun diye negatif deger.")]
    [SerializeField] float groundEffectYOffset = -0.3f;

    void Start()
    {
        if (groundEffectPrefab != null)
            Instantiate(groundEffectPrefab, transform.position + Vector3.up * groundEffectYOffset, Quaternion.identity, transform);
    }

  void OnTriggerEnter(Collider other)
   {
    
        Debug.Log("Trigger tetiklendi: " + other.gameObject.name);

        if(!other.CompareTag("Player")) return;
        Debug.Log("Player algılandı!");

        if(item == null)
        {
            Debug.LogWarning($"[ItemPickup] '{name}' objesinde Item atanmamis (SetItem() cagrilmadan sahneye elle konmus olabilir) - toplanamaz.");
            return;
        }

        var inventory = other.GetComponent<InventorySystem>();
        if(inventory == null)
        {     
               Debug.Log("Inventory bulunamadı!"); return;
        } 

        if(inventory.AddItem(item))
        {
            Debug.Log("Item toplandı:" + item.ItemName);
            Destroy(gameObject);
        }
}
  public void SetItem(ItemSO newItem)
{
    Debug.Log("SetItem: " + newItem?.ItemName);
    item = newItem;
}
}
