using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class QuestManager : MonoBehaviour
{
     public static QuestManager Instance { get; private set; }

    [SerializeField] List<QuestSO> quests = new List<QuestSO>();

    public event Action<QuestSO> OnQuestCompleted;
    public event Action<QuestSO> OnQuestUpdated;

    float _surviveTimer = 0f;
    int   _damageFreeKills = 0;
    bool  _tookDamageThisKill = false;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        GameEvents.OnEnemyKilled.AddListener(OnEnemyKilled);

        var playerStats = FindFirstObjectByType<SCharacterStats>();
        if (playerStats != null)
            playerStats.OnHealthChanged += OnPlayerHealthChanged;
    }

    void Update()
    {
        _surviveTimer += Time.deltaTime;
        UpdateSurviveQuests();
    }

    // ─── Kill ─────────────────────────────────────────────────────────────────
    void OnEnemyKilled()
    {
        IncrementQuest(QuestType.KillEnemies, 1);

        if (!_tookDamageThisKill)
        {
            _damageFreeKills++;
            IncrementQuest(QuestType.KillWithoutDamage, 1);
        }
        _tookDamageThisKill = false;
    }

    void OnPlayerHealthChanged(float current, float max)
    {
        if (current < max) _tookDamageThisKill = true;
    }

    // ─── Diamond ──────────────────────────────────────────────────────────────
    public void OnDiamondCollected()
    {
        IncrementQuest(QuestType.CollectDiamonds, 1);
    }

    // ─── Multi-kill ───────────────────────────────────────────────────────────
    // MultiKillUI en az 2'li (Double Kill) bir seri her yakaladiginda cagirir.
    public void OnMultiKill(int streak)
    {
        IncrementQuest(QuestType.MultiKill, 1);
    }

    // ─── Survive ──────────────────────────────────────────────────────────────
    void UpdateSurviveQuests()
    {
        foreach (var q in quests)
        {
            if (q.IsCompleted || q.Type != QuestType.SurviveSeconds) continue;
            q.CurrentAmount = Mathf.FloorToInt(_surviveTimer);
            OnQuestUpdated?.Invoke(q);
            if (q.CurrentAmount >= q.RequiredAmount)
                CompleteQuest(q);
        }
    }

    // ─── Genel ────────────────────────────────────────────────────────────────
    void IncrementQuest(QuestType type, int amount)
    {
        foreach (var q in quests)
        {
            if (q.IsCompleted || q.Type != type) continue;
            q.CurrentAmount += amount;
            OnQuestUpdated?.Invoke(q);
            if (q.CurrentAmount >= q.RequiredAmount)
                CompleteQuest(q);
        }
    }

    void CompleteQuest(QuestSO quest)
    {
        quest.IsCompleted = true;
        XPSystem.Instance?.AddXP(quest.XPReward);

        var inventory = FindFirstObjectByType<InventorySystem>();
        if (inventory != null && quest.DiamondReward > 0)
            for (int i = 0; i < quest.DiamondReward; i++)
                inventory.AddDiamond();

        OnQuestCompleted?.Invoke(quest);
        Debug.Log("Görev tamamlandı: " + quest.QuestName);
    }

    public List<QuestSO> GetQuests() => quests;
}