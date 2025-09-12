using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Debug = DebugEx;
using UnityEngine.InputSystem;

public class UniversalPair : MonoBehaviour
{
    [SerializeField] PlayerInput pi;

    void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
        PairAll();
    }
    void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    void OnDeviceChange(InputDevice d, InputDeviceChange c)
    {
        if (c == InputDeviceChange.Added || c == InputDeviceChange.Removed)
            PairAll();
    }

    void PairAll()
    {
        var all = new List<InputDevice>();
        if (Keyboard.current != null) all.Add(Keyboard.current);
        all.AddRange(Gamepad.all);
        all.AddRange(Joystick.all);

        // Universal 스킴으로 현재 연결된 장치 전체를 페어링
        pi.SwitchCurrentControlScheme("UniversalSch", all.ToArray());

        // 보너스: 맵 보장(없으면 Enable 안 됨)
        if (pi.currentActionMap == null || pi.currentActionMap.enabled == false)
            pi.SwitchCurrentActionMap("Player");

        // 진단
        Debug.Log($"[PI] scheme={pi.currentControlScheme} | devices={string.Join(", ", pi.devices.Select(d=>d.ToString()))}");
    }
}
