using System;
using UnityEngine;

// Runs an action whenever its object is enabled, e.g. to refresh a screen each time it appears
public class OnEnableHook : MonoBehaviour
{
    public Action action;

    private void OnEnable() => action?.Invoke();
}
