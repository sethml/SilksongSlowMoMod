using System;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace OFBSlowMo
{
    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        private ConfigEntry<KeyCode> slowMoKey = null!;
        private ConfigEntry<float> slowMoSpeed = null!;
        
        private ConfigEntry<string> speedPresets = null!;

        private ConfigEntry<KeyCode> increaseSlowMoKey = null!;
        private ConfigEntry<KeyCode> decreaseSlowMoKey = null!;
        
        // Tracks the timeScale the game *wants* to run at (e.g. 1.0 normally, 0.0 when paused).
        private float gameBaseTimeScale = 1.0f;
        // Tracks the last timeScale we applied, so we can detect external changes.
        private float lastAppliedTimeScale = 1.0f;

        private bool isSlowMoActive = false;
        
        private float previewTimer = 0f;
        private const float PreviewDuration = 1f;

        private Font? trajanFont = null;
        private bool trajanSearchAttempted = false;

        private float logTimer = 0f;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Configuration
            slowMoKey = Config.Bind(
                "General",
                "SlowMoKey",
                KeyCode.RightShift,
                "Key to press to activate slow motion"
            );

            slowMoSpeed = Config.Bind(
                "General",
                "SlowMoSpeed",
                0.5f,
                "Current slow motion speed (used to persist state)"
            );
            
            speedPresets = Config.Bind(
                "General",
                "SpeedPresets",
                "80,65,50",
                "Comma-separated list of slow motion speeds as percentages (e.g. '80,65,50' means 0.8, 0.65, 0.5)"
            );

            increaseSlowMoKey = Config.Bind(
                "General",
                "IncreaseSlowMoKey",
                KeyCode.Equals,
                "Key to increase slow motion speed (cycles through presets)"
            );

            decreaseSlowMoKey = Config.Bind(
                "General",
                "DecreaseSlowMoKey",
                KeyCode.Minus,
                "Key to decrease slow motion speed (cycles through presets)"
            );
        }

        private void Update()
        {
            HandleInput();
            UpdateTimeScale();
            UpdateMetrics();
        }

        private void HandleInput()
        {
            // Adjust slow-mo speed using presets
            if (Input.GetKeyDown(increaseSlowMoKey.Value))
            {
                AdjustSpeed(true);
            }
            else if (Input.GetKeyDown(decreaseSlowMoKey.Value))
            {
                AdjustSpeed(false);
            }

            if (Input.GetKeyDown(slowMoKey.Value))
            {
                isSlowMoActive = !isSlowMoActive;
                Logger.LogInfo($"Slow-Mo Toggled: {isSlowMoActive}");
            }
        }

        private void AdjustSpeed(bool increase)
        {
            float current = slowMoSpeed.Value;
            var presets = GetSortedPresets();
            float? next = null;

            if (increase)
            {
                // Find smallest preset strictly greater than current
                foreach (var p in presets)
                {
                    if (p > current + 0.001f)
                    {
                        next = p;
                        break;
                    }
                }
            }
            else
            {
                // Find largest preset strictly less than current (iterate backwards)
                for (int i = presets.Count - 1; i >= 0; i--)
                {
                    if (presets[i] < current - 0.001f)
                    {
                        next = presets[i];
                        break;
                    }
                }
            }

            // If found, update. If not found (we are at limit), do nothing.
            if (next.HasValue)
            {
                slowMoSpeed.Value = next.Value;
                
                if (!isSlowMoActive)
                {
                    previewTimer = PreviewDuration;
                }
                else
                {
                    Logger.LogInfo($"Mod updated target factor: {slowMoSpeed.Value}x");
                }
            }
        }
        
        private System.Collections.Generic.List<float> GetSortedPresets()
        {
            var list = new System.Collections.Generic.List<float>();
            if (string.IsNullOrEmpty(speedPresets.Value)) return list;

            string[] parts = speedPresets.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p.Trim(), out int val))
                {
                    // Clamp 1-100 just in case
                    float f = Mathf.Clamp(val, 1, 200) / 100f;
                    list.Add(f);
                }
            }
            list.Sort();
            return list;
        }

        private void UpdateTimeScale()
        {
            float currentTimeScale = Time.timeScale;

            // DETECT: If the current timeScale is different from what we last set, 
            // the game (or another mod) must have changed it. 
            // We capture this as the new "base" speed.
            if (Mathf.Abs(currentTimeScale - lastAppliedTimeScale) > 0.0001f)
            {
                // NOTE: Should we log this? It might be spammy if the game interpolates time scale.
                // useful for debugging:
                Logger.LogWarning($"External timeScale change detected: {currentTimeScale} (Previous applied: {lastAppliedTimeScale})");
                gameBaseTimeScale = currentTimeScale;
            }

            // CALCULATE: Desired = Base * Multiplier
            float multiplier = isSlowMoActive ? slowMoSpeed.Value : 1.0f;
            float targetTimeScale = gameBaseTimeScale * multiplier;

            // Prevent negative or invalid time scales? Unity handles 0 fine.
            if (targetTimeScale < 0f) targetTimeScale = 0f;

            // APPLY: If we aren't at the target, set it.
            if (Mathf.Abs(currentTimeScale - targetTimeScale) > 0.0001f)
            {
                Time.timeScale = targetTimeScale;
            }
            lastAppliedTimeScale = targetTimeScale;
        }
        
        private void UpdateMetrics()
        {
            // Preview timer countdown (use unscaled time so it isn't affected by slow-mo)
            if (previewTimer > 0f)
            {
                previewTimer -= Time.unscaledDeltaTime;
                if (previewTimer < 0f)
                {
                    previewTimer = 0f;
                }
            }

            // Logging (1Hz)
            logTimer += Time.unscaledDeltaTime;
            if (logTimer >= 1.0f)
            {
                logTimer = 0f;
                Logger.LogInfo($"OFBSlowMo: [Active:{isSlowMoActive}] [Base:{gameBaseTimeScale:F3}] [Mult:{slowMoSpeed.Value:F3}] -> [Actual:{Time.timeScale:F3}]");
            }
        }

        private void OnGUI()
        {
            // Show HUD when slow-mo is active, or when previewing a speed change
            if (isSlowMoActive || previewTimer > 0f)
            {
                // Save and override GUI state so other mods' GUI settings don't leak into ours
                int previousDepth = GUI.depth;
                Matrix4x4 previousMatrix = GUI.matrix;

                // Draw on top of most other GUI and with an identity matrix
                GUI.depth = -100;
                GUI.matrix = Matrix4x4.identity;

                // Try to find Trajan font if we haven't found it yet
                if (trajanFont == null && !trajanSearchAttempted)
                {
                    trajanSearchAttempted = true;
                    // Look for loaded fonts named "Trajan..."
                    // We use FindObjectsOfTypeAll to catch assets even if they aren't currently active in the scene,
                    // as long as they are loaded in memory.
                    var fonts = Resources.FindObjectsOfTypeAll<Font>();
                    Font? bestCandidate = null;

                    foreach (var f in fonts)
                    {
                        if (f.name.IndexOf("Trajan", StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            // If we haven't found a candidate yet, take this one
                            if (bestCandidate == null)
                            {
                                bestCandidate = f;
                            }
                            // If this new one is NOT bold and the current best IS bold, switch to this one
                            else if (bestCandidate.name.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) >= 0 
                                     && f.name.IndexOf("Bold", StringComparison.OrdinalIgnoreCase) < 0)
                            {
                                bestCandidate = f;
                            }
                        }
                    }

                    if (bestCandidate != null)
                    {
                        trajanFont = bestCandidate;
                        Logger.LogInfo($"OFBSlowMo: Found Trajan font: {trajanFont.name}");
                    }
                    else
                    {
                        Logger.LogWarning("OFBSlowMo: Could not find any font named 'Trajan' in Resources. Using default font.");
                    }
                }
                
                // Calculate relative sizes based on screen height (3.5% font size)
                float screenHeight = Screen.height;
                int fontSize = Mathf.RoundToInt(screenHeight * 0.035f);
                float padding = screenHeight * 0.03f;
                // Ensure reasonable text box size
                float textWidth = fontSize * 4f; 
                float textHeight = fontSize * 1.5f;

                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.fontSize = fontSize;
                textStyle.fontStyle = FontStyle.Bold;

                // Base color: white when active, green fade when previewing
                Color baseColor;
                if (isSlowMoActive)
                {
                    baseColor = Color.white;
                }
                else
                {
                    // Preview color: green, fading out over PreviewDuration
                    float t = Mathf.Clamp01(previewTimer / PreviewDuration);
                    baseColor = new Color(0f, 1f, 0f, t);
                }

                textStyle.normal.textColor = baseColor;
                textStyle.alignment = TextAnchor.MiddleLeft;
                if (trajanFont != null)
                {
                    textStyle.font = trajanFont;
                }
                
                // Calculate position (lower-left corner with padding)
                float x = padding;
                float y = screenHeight - textHeight - padding;
                
                // Draw text with shadow for depth
                int speedPercent = Mathf.RoundToInt(slowMoSpeed.Value * 100f);
                string text = $"{speedPercent}%";
                
                // Shadow (respect alpha so fade looks nice)
                GUIStyle shadowStyle = new GUIStyle(textStyle);
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f * baseColor.a);
                // Shadow offset relative to font size
                float shadowOffset = fontSize * 0.06f; 
                GUI.Label(new Rect(x + shadowOffset, y + shadowOffset, textWidth, textHeight), text, shadowStyle);
                
                // Main text
                GUI.Label(new Rect(x, y, textWidth, textHeight), text, textStyle);

                // Restore previous GUI state
                GUI.matrix = previousMatrix;
                GUI.depth = previousDepth;
            }
        }

        private void OnDestroy()
        {
            // Restore normal time scale when mod is unloaded
            if (Time.timeScale != 1.0f)
            {
                Time.timeScale = 1.0f;
            }
        }
    }
}

