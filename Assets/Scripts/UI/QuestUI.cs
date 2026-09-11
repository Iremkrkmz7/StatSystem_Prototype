using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuestUI : MonoBehaviour
{
     [System.Serializable]
    public class QuestSlotUI
    {
        public GameObject     Panel;
        public TextMeshProUGUI NameText;
        public TextMeshProUGUI ProgressText;
        public Image  CheckboxImage;
        public Sprite CheckedSprite; 
        public Sprite UncheckedSprite;  

    }

    [Header("Görev Slotları")]
    [SerializeField] QuestSlotUI[] questSlots;

    [Header("Tamamlanma Bildirimi")]
    [SerializeField] GameObject    completionPanel;
    [SerializeField] TextMeshProUGUI completionText;
    [SerializeField] float         displayDuration = 3f;

    private List<QuestSO> hiddenQuests = new List<QuestSO>();
    private List<QuestSO> hidingQuests = new List<QuestSO>();

     void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestUpdated   += RefreshUI;
            QuestManager.Instance.OnQuestCompleted += ShowCompletion;
        }
        if (completionPanel) completionPanel.SetActive(false);
        RefreshAll();
    }
    void RefreshAll()
    {
        if (QuestManager.Instance == null) return;
        var quests = QuestManager.Instance.GetQuests();

        int slotIndex = 0;

        for (int i = 0; i < quests.Count; i++)
        {
            if (hiddenQuests.Contains(quests[i])) continue;

            if (slotIndex < questSlots.Length)
            {
                questSlots[slotIndex].Panel.SetActive(true);
                UpdateSlot(questSlots[slotIndex], quests[i]);
                slotIndex++;
            }    
        }
        for (int i = slotIndex; i < questSlots.Length; i++)
        {
            if (questSlots[i].Panel != null)
                questSlots[i].Panel.SetActive(false);
        }
    }

    void RefreshUI(QuestSO quest) => RefreshAll();

    void UpdateSlot(QuestSlotUI slot, QuestSO quest)
    {
        if (slot.NameText)
            slot.NameText.text = quest.QuestName;

        if (slot.ProgressText)
            slot.ProgressText.text = $"{quest.CurrentAmount} / {quest.RequiredAmount}";

        if (slot.CheckboxImage)
        slot.CheckboxImage.sprite = quest.IsCompleted ? slot.CheckedSprite : slot.UncheckedSprite;    
       
        if (quest.IsCompleted)
        {
            if (slot.NameText) slot.NameText.color = Color.green;
            if (slot.ProgressText) slot.ProgressText.color = Color.green;

            if (!hidingQuests.Contains(quest) && !hiddenQuests.Contains(quest))
            {
                hidingQuests.Add(quest); 
                StartCoroutine(HideSlotRoutine(quest, displayDuration));
            }
        }
        else
        {
            if (slot.NameText) slot.NameText.color = Color.white;
            if (slot.ProgressText) slot.ProgressText.color = Color.white;
        }
    }
    IEnumerator HideSlotRoutine(QuestSO quest, float delay)
    {
        yield return new WaitForSeconds(delay);
        hidingQuests.Remove(quest);
        hiddenQuests.Add(quest);
      
        RefreshAll();
    }

    void ShowCompletion(QuestSO quest)
    {
        StartCoroutine(ShowMessageRoutine($"✓ {quest.QuestName} Completed!\n+{quest.XPReward} XP"));
    }

    IEnumerator ShowMessageRoutine(string message)
    {
        if (completionPanel) completionPanel.SetActive(true);
        if (completionText)  completionText.text = message;

        yield return new WaitForSeconds(displayDuration);

        if (completionPanel) completionPanel.SetActive(false);
    }

    void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestUpdated   -= RefreshUI;
            QuestManager.Instance.OnQuestCompleted -= ShowCompletion;
        }
    }
}

