using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class StatModifier 
{
  public enum ModifierType { Flat, PercentAdd, PercentMult}

  public float Value;
  public ModifierType Type;
  public int Order;
  public object Source;

  public StatModifier(float value, ModifierType type, int order = 0, object source = null)
  {
    Value = value;
    Type = type;
    Order = order;
    Source = source;
  }
 public static StatModifier Flat       (float v, object src = null) => new(v, ModifierType.Flat, 0, src);
 public static StatModifier PercentAdd (float v, object src = null) => new(v, ModifierType.PercentAdd, 1, src);
 public static StatModifier PercentMult(float v, object src = null) => new(v, ModifierType.PercentMult, 2, src);
}