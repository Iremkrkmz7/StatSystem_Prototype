using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    
    [Header("Can")]
    [SerializeField] Slider          healthSlider;
    [SerializeField] TextMeshProUGUI healthText;

    [Header("Statlar")]
    [SerializeField] TextMeshProUGUI damageText;

    [Header("Diamond / Kill")]
    [SerializeField] TextMeshProUGUI diamondText;
    [SerializeField] TextMeshProUGUI killCountText;
    [SerializeField] Slider          diamondSlider;

    SCharacterStats _playerStats;
    int _killCount = 0;
    public int KillCount => _killCount;

    void Start()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if(player != null)
        {
            _playerStats = player.GetComponent<SCharacterStats>();
            _playerStats.OnHealthChanged += UpdateHealth;
            UpdateHealth(_playerStats.CurrentHealth, _playerStats.MaxHealth);
        }
        GameEvents.OnEnemyKilled.AddListener(UpdateKills);
        var inventory = FindFirstObjectByType<InventorySystem>();
        if (inventory != null)
        inventory.OnDiamondsChanged += UpdateDiamonds;
    }
    
    void Update()
    {
        if(_playerStats == null) return;
        if (damageText) damageText.text = "Damage: " + _playerStats.GetStat(StatType.Damage).ToString("F0");
    }
    void UpdateHealth(float current, float max)
    {
       if(healthSlider) healthSlider.value = max > 0 ? current /max : 0;
       if(healthText) healthText.text = Mathf.CeilToInt(current) + "/ " +Mathf.CeilToInt(max);
    }
    void UpdateKills()
    {
        _killCount++;
        if(killCountText) killCountText.text = "Kill: " + _killCount;

    }  
   
    void UpdateDiamonds()
   {
      var inventory = FindFirstObjectByType<InventorySystem>();
       if (diamondText) diamondText.text = inventory?.Diamonds.ToString() ?? "0";
     // if (diamondText) diamondText.text = "💎 " + inventory?.Diamonds;
    }
    void OnDestroy()
    {
        GameEvents.OnEnemyKilled.RemoveListener(UpdateKills);
    }
}













