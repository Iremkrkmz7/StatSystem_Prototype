using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FloatingText : MonoBehaviour
{
    [Header("Ayarlar")]
    public float moveSpeed = 2f;
    public float destroyTime = 1.5f;
    public float fadeStartDelay = 0.6f; // YAZI DOĞDUKTAN SONRA KAÇ SANİYE CANLI KALSIN?

    private Vector3 initialOffset; 
    private TextMeshProUGUI textMesh;
    private Color textColor;
    private Transform targetTransform;
    
    private float currentFloatY = 0f;
    private float timer = 0f; // Geçen süreyi ölçeceğiz
    private Camera _cam;

    void Awake()
    {
        textMesh = GetComponent<TextMeshProUGUI>();
        _cam = Camera.main;
    }

    void Start()
    {
       if (textMesh != null)
            textColor = textMesh.color;
        
        Destroy(gameObject, destroyTime);
    }

    void Update()
    {
        timer += Time.deltaTime; // Süreyi ilerlet
        currentFloatY += moveSpeed * Time.deltaTime;

        if (targetTransform != null)
        {
            transform.position = targetTransform.position + initialOffset + new Vector3(0, currentFloatY, 0);
        }
        else
        {
            transform.position += Vector3.up * moveSpeed * Time.deltaTime;
        }

        // World Space Canvas kameraya dönük dursun diye (billboard), yoksa
        // top-down kamera açısından panel kenardan görünüp neredeyse kaybolur.
        if (_cam != null)
            transform.rotation = _cam.transform.rotation;

        // SADECE 'fadeStartDelay' SÜRESİ GEÇTİKTEN SONRA SOLMAYA BAŞLA
        if (textMesh != null && timer > fadeStartDelay)
        {
            // Kalan kısacık sürede (örneğin son 0.9 saniyede) hızla solup yok olsun
            float fadeDuration = destroyTime - fadeStartDelay;
            textColor.a -= (Time.deltaTime / fadeDuration);
            textMesh.color = textColor;
        }
    }

    public void SetText(string amount, Color newColor, Transform target = null)
    {
        if (textMesh == null) textMesh = GetComponent<TextMeshProUGUI>();
        
        // Rengin Alpha'sını (Saydamlık) ne olursa olsun ZORLA 1 (Tam opak) yapıyoruz.
        newColor.a = 1f; 
        
        textMesh.text = amount;
        textMesh.color = newColor;
        textColor = newColor;
        
        targetTransform = target;

        if (targetTransform != null)
        {
            initialOffset = transform.position - targetTransform.position;
        }
    }
}