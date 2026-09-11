using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;


public abstract class TypedGameEvent<T> : ScriptableObject
{
    readonly List<Action<T>> _listeners = new();
    public void Raise(T value)
    {
        for(int i = _listeners.Count - 1;i>= 0; i--)
        _listeners[i]?.Invoke(value);
    }
    public void Addlistener(Action<T> listener)
    {
        if(!_listeners.Contains(listener))
        _listeners.Add(listener);
    }
    public void RemoveListener(Action<T> listener) =>
    _listeners.Remove(listener);
}
[CreateAssetMenu(fileName = "FloatEvent", menuName = "Events/Float Event")]
public class FloatGameEvent : TypedGameEvent<float> {}

[CreateAssetMenu(fileName = "IntEvent", menuName = "Events/Int Event")]
public class IntGameEvent : TypedGameEvent<int> {}

[CreateAssetMenu(fileName = "StringEvent", menuName = "Events/String Event")]
public class StringGameEvent : TypedGameEvent<string> {}
