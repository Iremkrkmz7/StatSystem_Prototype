using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class GameEventListener : MonoBehaviour
{
    [SerializeField] GameEvent gameEvent;
    [SerializeField] UnityEvent response;

    void OnEnable() => gameEvent.AddListener(this);
    void OnDisable() => gameEvent.RemoveListener(this);

    public void OnEventRaised() => response?.Invoke();
}
    
