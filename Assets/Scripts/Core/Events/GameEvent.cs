using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(fileName = "New GameEvent" , menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    readonly List<GameEventListener> _listeners = new();

    public void Raise()
    {
        for(int i = _listeners.Count - 1; i >= 0; i--)
        _listeners[i].OnEventRaised();

    }
    public void AddListener(GameEventListener listener)
    {
        if(!_listeners.Contains(listener))
        _listeners.Add(listener);
    }
    public void RemoveListener(GameEventListener listener) =>
    _listeners.Remove(listener);
}
