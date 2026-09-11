using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

// LevelUpUI'nin gosterdigi 3 karttan biri - basligi/aciklamayi doldurur,
// tiklaninca secilen secenegi bildirir.
public class UpgradeCardUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] Image iconImage; // opsiyonel - option.icon bos ise gizlenir
    [SerializeField] Button button;

    public void Setup(UpgradeOption option, Action onChosen)
    {
        if (titleText != null) titleText.text = option.title;
        if (descriptionText != null) descriptionText.text = option.description;

        if (iconImage != null)
        {
            iconImage.sprite = option.icon;
            iconImage.enabled = option.icon != null;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onChosen());
        }
    }
}
