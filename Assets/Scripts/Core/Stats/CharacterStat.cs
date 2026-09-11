using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class CharacterStat
{
    public float BaseValue;

    readonly List<StatModifier> _modifiers = new();
    float _lastBase;
    float _cachedValue;
    bool _isDirty = true;

    public CharacterStat(float baseValue)
    {
        BaseValue = baseValue;
        _lastBase = baseValue;
    }
    public float Value
    {
       get
       {
        if(_isDirty || !Mathf.Approximately(_lastBase, BaseValue))
        {
            _lastBase = BaseValue;
            _cachedValue = Calculate();
            _isDirty = false;
        }
        return _cachedValue;
       }
    }
    
    public void AddModifier(StatModifier mod)
    {
        _modifiers.Add(mod);
        _isDirty = true;
    }
    public bool RemoveModifier(StatModifier mod)
    {
        if(!_modifiers.Remove(mod)) return false;
        _isDirty = true;
        return true;
    }
    public void RemoveAllFromSource(object source)
    {
        int removed = _modifiers.RemoveAll(m => m.Source == source);
        if(removed > 0) _isDirty = true;
    }
    float Calculate()
    {
       float final = BaseValue; 
       float sumPercAdd = 0;
       _modifiers.Sort((a,b) => a.Order.CompareTo(b.Order));

       for(int i=0; i < _modifiers.Count; i++)
       {
        var mod = _modifiers[i];
        switch (mod.Type)
        {
            case StatModifier.ModifierType.Flat:
            final += mod.Value;
            break;
            case StatModifier.ModifierType.PercentAdd:
            sumPercAdd += mod.Value;
            bool isLast = i + 1 >= _modifiers.Count 
            || _modifiers[i + 1].Type != StatModifier.ModifierType.PercentAdd;
            if(isLast)
            {
                final *= 1 + sumPercAdd;
                sumPercAdd = 0;
            }
            break;
            case StatModifier.ModifierType.PercentMult:
            final *= 1 + mod.Value;
            break;
       }
    }
    return(float)Math.Round(final ,4);
    }
}