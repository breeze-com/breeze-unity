using UnityEngine;

/// <summary>
/// Bridge MonoBehaviour attached to the "BreezePay" GameObject.
/// Kept as a marker component for the Breeze SDK lifecycle.
/// Android callbacks are handled by BreezeAndroidCallbackReceiver (see BreezeNativeAndroid.cs).
/// </summary>
public class BreezeBridgeMessenger : MonoBehaviour
{
    // Intentionally empty — Android dialog dismiss callbacks are routed through
    // BreezeAndroidCallbackReceiver to avoid duplicate callback paths.
}
