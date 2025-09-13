using UnityEngine;
using System.Collections;
using Settings; 
using System;

public class MemoryManager : MonoBehaviour
{
    // Removed the public maxHeapSizeMB field. We get it from SettingsManager now.

    [Tooltip("How often to check memory usage in seconds.")]
    public float checkInterval = 5f;

    private long _maxHeapSizeBytes;
    private GraphicsSettings _graphicsSettings; // Reference to the settings

    void Start()
    {
        // 1. Get a reference to the graphics settings
        _graphicsSettings = SettingsManager.GraphicsSettings;

        // 2. Convert the initial MB value from the settings to bytes
        _maxHeapSizeBytes = _graphicsSettings.MemoryCapMB.Value * 1024L * 1024L;

        // 3. Start the periodic check
        StartCoroutine(PeriodicMemoryCheck());

        Debug.Log($"[MemoryManager] Started. Capping heap at {_graphicsSettings.MemoryCapMB.Value}MB, checking every {checkInterval}s.");
    }

    // This method is now called internally when the setting changes, not from a UI script.
    private void UpdateMemoryCap()
    {
        _maxHeapSizeBytes = _graphicsSettings.MemoryCapMB.Value * 1024L * 1024L;
        Debug.Log($"[MemoryManager] Memory cap updated to: {_graphicsSettings.MemoryCapMB.Value}MB");
    }

    IEnumerator PeriodicMemoryCheck()
    {
        while (true)
        {
            yield return new WaitForSecondsRealtime(checkInterval);

            long currentHeapSize = GC.GetTotalMemory(false);
            int currentHeapSizeMB = (int)(currentHeapSize / (1024f * 1024f));

            // Log compared to the current value of the setting
            Debug.LogWarning($"[MemoryManager] Current Heap: {currentHeapSizeMB}MB / {_graphicsSettings.MemoryCapMB.Value}MB");

            if (currentHeapSize > _maxHeapSizeBytes)
            {
                Debug.LogWarning($"[MemoryManager] Heap limit exceeded! Forcing Garbage Collection.");
                GC.Collect();
                GC.WaitForPendingFinalizers();

                long newHeapSize = GC.GetTotalMemory(false);
                int newHeapSizeMB = (int)(newHeapSize / (1024f * 1024f));
                Debug.LogWarning($"[MemoryManager] GC Complete. New Heap: {newHeapSizeMB}MB");
            }

            // Check if the setting has been changed by the user elsewhere (e.g., the UI)
            // This is more efficient than checking every frame and only updates if needed.
            if (_maxHeapSizeBytes != _graphicsSettings.MemoryCapMB.Value * 1024L * 1024L)
            {
                UpdateMemoryCap();
            }
        }
    }
}