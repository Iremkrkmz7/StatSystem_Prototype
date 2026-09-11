using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameEvents 
{
    public static SimpleEvent OnEnemyKilled {get ; } = new SimpleEvent();
    public static SimpleEvent OnWaveComplete {get; } = new SimpleEvent();
    public static SimpleEvent OnPlayerDied {get; } = new SimpleEvent();
    public static DataEvent<int> OnScoreChanged {get; } = new DataEvent<int>();
    public static DataEvent<int> OnWaveChanged {get; } = new DataEvent<int>();
}
public class SimpleEvent
{
    System.Action _listeners;

    public void Raise() => _listeners?.Invoke();
    public void AddListener(System.Action l) => _listeners += l;
    public void RemoveListener(System.Action l) => _listeners -= l;
}
public class DataEvent<T>
{
    System.Action<T> _listeners;

    public void Raise(T value) => _listeners?.Invoke(value);
    public void AddListener(System.Action<T> l) => _listeners += l;
    public void RemoveListener(System.Action<T> l) => _listeners -= l;
}


