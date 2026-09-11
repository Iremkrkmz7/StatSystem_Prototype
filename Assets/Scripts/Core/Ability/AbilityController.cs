using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AbilityController : MonoBehaviour
{ 
    [SerializeField] List<AbilitySO> abilities = new();

    [Header("Ses")]
    [SerializeField] AudioClip shieldSound;
    [SerializeField] AudioClip aoeSound;
    [SerializeField] AudioClip healSound;

    Dictionary<AbilitySO, float> _cooldownUntil = new();

    Animator _animator;
    SCharacterStats _stats;
    Rigidbody       _rb;
    AudioSource     _audioSource;

    void Awake()
    {
        _animator = GetComponentInChildren<Animator>();
        _stats = GetComponent<SCharacterStats>();
        _rb = GetComponent<Rigidbody>();
        _audioSource = GetComponent<AudioSource>();
        foreach (var a in abilities) _cooldownUntil[a] =0f;

    }
    void Update()
    {
       if(_stats.IsDead) return;
       if(Input.GetKeyDown(KeyCode.Q)) TryActivateAbilityByType(AbilityType.Dash);
   
    }
    public bool TryActivateAbilityByType(AbilityType type)
    {
        foreach (var ability in abilities)
        
            if (ability.Type == type)
            
                return TryActive(ability); // Eşleşen yeteneği çalıştır
            
        
        return false;
    }
    public bool TryActive(AbilitySO ability)
    {
        if(!_cooldownUntil.ContainsKey(ability)) return false;
        if (Time.time < _cooldownUntil[ability]) return false;
         _cooldownUntil[ability] = Time.time + GetScaledCooldown(ability);
        StartCoroutine(Execute(ability));
        return true;
    }

    // Level yukseldikce yetenek daha sik kullanilabilsin diye cooldown azalir.
    // Asla sifira yaklasmaz (payda hep 1'in ustunde), asiri OP olma riski yok.
    float GetScaledCooldown(AbilitySO ability)
    {
        int level = XPSystem.Instance != null ? XPSystem.Instance.CurrentLevel : 1;
        return ability.Cooldown / (1f + BalanceConfig.Instance.abilityCDLevelFactor * level);
    }
   IEnumerator Execute(AbilitySO ability)
    {
        GameObject vfx = null;
        if (ability.VFXPrefab != null)
        vfx = Instantiate(ability.VFXPrefab, transform.position, Quaternion.identity, transform);
   

        switch (ability.Type)
        {
            case AbilityType.Dash:
                 Vector3 dir = transform.forward;
           StartCoroutine(DashMove(dir, ability.Value));
           break;

           case AbilityType.Shield:
           _animator?.SetTrigger("Shield");
            Sfx.PlayOneShot(_audioSource, shieldSound);
            _stats.AddModifier(StatType.Defense, StatModifier.Flat(ability.Value, ability));
            yield return new WaitForSeconds(ability.Duration);
           _stats.RemoveModifiersFromSource(ability);
           Debug.Log("UseSlot shield çağrıldı");
           if (vfx != null) Destroy(vfx);
           break;

            case AbilityType.AoE:
            _animator?.SetTrigger("AoE");
            Sfx.PlayOneShot(_audioSource, aoeSound);
             float aoeDmg = _stats.GetStat(StatType.Damage) * 2f;
             Collider[] hits = Physics.OverlapSphere(transform.position, ability.Value);
             foreach (var col in hits)
            if (col.CompareTag("Enemy") && col.TryGetComponent<SCharacterStats>(out var es))
            es.TakeDamage(aoeDmg);
            yield return new WaitForSeconds(ability.Duration);
            Debug.Log("UseSlot aeo çağrıldı");
            if (vfx != null) Destroy(vfx);
               break;

            case AbilityType.Heal:
                // Ses artik SCharacterStats.Heal() icinde merkezi olarak calinyor
                // (item/heal-pack gibi diger iyilesmelerde de calissin diye).
                _stats.Heal(ability.Value);
                break;
        }
        
    }
      IEnumerator DashMove(Vector3 dir, float value)
    {
        float elapsed = 0f;
        float dashDuration = 0.2f;
        var cc = GetComponent<CharacterController>();
        
        while (elapsed < dashDuration)
        {
            cc?.Move(dir * value * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
    public float GetAbilityDuration(AbilityType type)
{
    foreach (var ability in abilities)
        if (ability.Type == type) return ability.Duration;
    return 5f;
}
    
}  
    
