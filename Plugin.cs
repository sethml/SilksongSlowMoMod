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
        // toggleMode removed
        private ConfigEntry<KeyCode> increaseSlowMoKey = null!;
        private ConfigEntry<KeyCode> decreaseSlowMoKey = null!;
        
        // Tracks the timeScale the game *wants* to run at (e.g. 1.0 normally, 0.0 when paused).
        private float gameBaseTimeScale = 1.0f;
        // Tracks the last timeScale we applied, so we can detect external changes.
        private float lastAppliedTimeScale = 1.0f;

        private bool isSlowMoActive = false;
        
        private float previewTimer = 0f;
        private const float PreviewDuration = 0.5f;

        private Font? trajanFont = null;
        private bool trajanSearchAttempted = false;

        private float logTimer = 0f;

        // 1 / sqrt(sqrt(2)) ≈ 0.84089642
        private const float SpeedScale = 0.84089642f;

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
                SpeedScale,
                "Time scale when slow motion is active (0.1 = 10% speed, 0.5 = 50% speed, etc.)"
            );

            // toggleMode config removed

            increaseSlowMoKey = Config.Bind(
                "General",
                "IncreaseSlowMoKey",
                KeyCode.Equals,
                "Key to increase slow motion speed by 10%"
            );

            decreaseSlowMoKey = Config.Bind(
                "General",
                "DecreaseSlowMoKey",
                KeyCode.Minus,
                "Key to decrease slow motion speed by 10%"
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
            // Adjust slow-mo speed with +/- keys (scale by 1/sqrt(2))
            if (Input.GetKeyDown(increaseSlowMoKey.Value))
            {
                // Increase towards normal speed by dividing by SpeedScale
                slowMoSpeed.Value = Mathf.Clamp01(slowMoSpeed.Value / SpeedScale);

                // If slow-mo is currently off, briefly preview the new speed
                if (!isSlowMoActive)
                {
                    previewTimer = PreviewDuration;
                }
                else
                {
                    Logger.LogInfo($"Mod updated target factor: {slowMoSpeed.Value}x");
                }
            }
            else if (Input.GetKeyDown(decreaseSlowMoKey.Value))
            {
                // Decrease speed (more slow-mo) by multiplying by SpeedScale
                slowMoSpeed.Value = Mathf.Clamp01(slowMoSpeed.Value * SpeedScale);

                // If slow-mo is currently off, briefly preview the new speed
                if (!isSlowMoActive)
                {
                    previewTimer = PreviewDuration;
                }
                else
                {
                    Logger.LogInfo($"Mod updated target factor: {slowMoSpeed.Value}x");
                }
            }

            if (Input.GetKeyDown(slowMoKey.Value))
            {
                isSlowMoActive = !isSlowMoActive;
                Logger.LogInfo($"Slow-Mo Toggled: {isSlowMoActive}");
            }
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
                // Logger.LogWarning($"External timeScale change detected: {currentTimeScale} (Previous applied: {lastAppliedTimeScale})");
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
                lastAppliedTimeScale = targetTimeScale;
            }
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
                
                // Style for the text with shadow effect
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.fontSize = 48;
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
                float padding = 30f;
                float textWidth = 200f;
                float textHeight = 60f;
                float x = padding;
                float y = Screen.height - textHeight - padding;
                
                // Draw text with shadow for depth
                int speedPercent = Mathf.RoundToInt(slowMoSpeed.Value * 100f);
                string text = $"{speedPercent}%";
                
                // Shadow (respect alpha so fade looks nice)
                GUIStyle shadowStyle = new GUIStyle(textStyle);
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f * baseColor.a);
                GUI.Label(new Rect(x + 3, y + 3, textWidth, textHeight), text, shadowStyle);
                
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

