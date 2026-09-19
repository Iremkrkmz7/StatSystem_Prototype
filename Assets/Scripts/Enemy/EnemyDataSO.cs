using System.Collections;
using System.Collections.Generic;
using UnityEngine;
[CreateAssetMenu(fileName = "New Enemy", menuName = "Enemy/ Enemy Data")]
public class EnemyDataSO : ScriptableObject
{
    public string PoolKey = "Enemy_Basic";

    [Tooltip("Bu dusman tipi ancak bu dalgadan itibaren (dahil) spawn havuzuna girer - orn. 4 verilirse ilk 3 dalgada hic cikmaz.")]
    public int MinWave = 1;

    [Tooltip("Bu dusman tipi ancak OYUNCU bu XP seviyesine (Level Up ekranindaki seviye) ulastiktan SONRA spawn havuzuna girer - MinWave'den FARKLI/BAGIMSIZ bir kosul, IKISI de saglanmali (AND). Orn. 4 verilirse oyuncu Level 4'e gelmeden hic cikmaz.")]
    public int MinPlayerLevel = 1;

    [Tooltip("Ayni dalgada birden fazla dusman tipi uygunken hangisinin secilecegini agirliklandirir - orn. Basic=1, Bomber=0.3 verilirse Bomber, Basic'ten ~3 kat DAHA NADIR cikar. Butun uygun tiplerin agirligi toplanip orantili sansla secilir (esit degil).")]
    public float SpawnWeight = 1f;

    [Tooltip("Ayni anda sahnede bu tipten en fazla kac tane canli olabilir (0 = sinirsiz). Bomber gibi ozel/guclu dusmanlarin bir anda 'suru halinde' gelmesini engellemek icin kullanilir - limit doluyken bu tip secilebilir havuzdan cikarilir.")]
    public int MaxAlive = 0;

    public float BaseHealth = 50f;
    public float BaseSpeed =3.5f;
    public float BaseDamage =8f;
    public float AttackRange =1.8f;
    public float AttackCooldown =1.2f;
    [Tooltip("Saldiri animasyonu basladiktan kac saniye sonra hasar + vurus sesi uygulanir - yani silahin hedefe DEGDIGI an. Animasyona gore ayarla: ses savurustan once geliyorsa artir, sonra geliyorsa azalt.")]
    public float AttackImpactDelay = 0.35f;

    public float HealthScalePerWave = 1.12f;
    public float DamageScalePerWave = 1.06f;

    public int ScoreValue = 10;

    [Header("Rewards (Ödüller)")]
    public int BaseXP = 15;
    public float XPScalePerWave = 1.05f; // İlerleyen dalgalarda XP de artsın
    public GameObject FloatingTextPrefab; // Yazı prefabını buraya koyacağız

    [Header("Olum Patlamasi (Bomber tipi dusmanlar icin)")]
    public bool ExplodesOnDeath = false;
    public float ExplosionRadius = 3f;
    public float ExplosionDamage = 25f;
    [Tooltip("Yerde patlayip cevreyi saran gorsel efekt (kucuk kafa patlamasindan bagimsiz, sadece ExplodesOnDeath true iken kullanilir)")]
    public GameObject DeathExplosionVFX;
    [Tooltip("Bu dusman tipinin olum sesi")]
    public AudioClip DeathSound;
    [Tooltip("Yerdeki alevin donen sesi (sadece ExplodesOnDeath icin)")]
    public AudioClip FireLoopSound;
    [Tooltip("Yerdeki alev oyuncuya hasar verdigi anda calinacak ses (sadece ExplodesOnDeath icin)")]
    public AudioClip FireHitSound;

    [Tooltip("Bu dusman yakin donusle (bicak vs.) vurdugunda calinacak ses")]
    public AudioClip MeleeHitSound;

    [Tooltip("Olum aninda patlama oynarken dusmanin yere yigilma animasyonuna gecmeden once ne kadar donuk beklesin (saniye)")]
    public float DeathFreezeDuration = 0.7f;

    [Tooltip("Kafa patlamasinin, kafa noktasinin ne kadar ustunde gorunecegi (dusman modeline gore ayarlanir)")]
    public float HeadBurstHeightOffset = 1.1f;

    public float ScaleHealth(int wave) => BaseHealth * Mathf.Pow(HealthScalePerWave, wave - 1);
    public float ScaleDamage(int wave) => BaseDamage * Mathf.Pow(DamageScalePerWave, wave -1);

    public int ScaleXP(int wave) => Mathf.RoundToInt(BaseXP * Mathf.Pow(XPScalePerWave, wave - 1));

}
