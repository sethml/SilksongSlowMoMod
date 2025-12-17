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
        private ConfigEntry<bool> toggleMode = null!;
        private ConfigEntry<KeyCode> increaseSlowMoKey = null!;
        private ConfigEntry<KeyCode> decreaseSlowMoKey = null!;
        private ConfigEntry<string> fontNamesConfig = null!;
        
        private bool isSlowMoActive = false;
        private float normalTimeScale = 1.0f;

        private bool flashRed = false;
        private float flashTimer = 0f;
        private const float FlashDuration = 0.5f;

        private float previewTimer = 0f;
        private const float PreviewDuration = 0.5f;

        private Font? hudFont = null;
        private bool hudFontInitialized = false;
        private string? hudFontName = null;

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

            toggleMode = Config.Bind(
                "General",
                "ToggleMode",
                true,
                "If true, slow motion toggles on/off. If false, hold key to activate."
            );

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

            fontNamesConfig = Config.Bind(
                "Visual",
                "FontNames",
                "BlackChancery, Old English Text MT, GothicE, UnifrakturMaguntia, Hoefler Text, Palatino Linotype, Book Antiqua, Georgia, Times New Roman",
                "Comma-separated list of font names to try for the HUD text (first available font is used)."
            );
        }

        private void Update()
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
            }

            if (toggleMode.Value)
            {
                // Toggle mode: press key to toggle slow motion on/off
                if (Input.GetKeyDown(slowMoKey.Value))
                {
                    isSlowMoActive = !isSlowMoActive;
                    if (isSlowMoActive)
                    {
                        ApplySlowMotion();
                    }
                    else
                    {
                        RestoreNormalSpeed();
                    }
                }
            }
            else
            {
                // Hold mode: hold key to activate slow motion
                bool keyPressed = Input.GetKey(slowMoKey.Value);
                
                if (keyPressed && !isSlowMoActive)
                {
                    isSlowMoActive = true;
                    ApplySlowMotion();
                }
                else if (!keyPressed && isSlowMoActive)
                {
                    isSlowMoActive = false;
                    RestoreNormalSpeed();
                }
            }

            // Flash timer countdown (use unscaled time so it isn't affected by slow-mo)
            if (flashRed)
            {
                flashTimer -= Time.unscaledDeltaTime;
                if (flashTimer <= 0f)
                {
                    flashRed = false;
                    flashTimer = 0f;
                }
            }

            // Preview timer countdown (use unscaled time so it isn't affected by slow-mo)
            if (previewTimer > 0f)
            {
                previewTimer -= Time.unscaledDeltaTime;
                if (previewTimer < 0f)
                {
                    previewTimer = 0f;
                }
            }
            
            // Continuously enforce timeScale every frame to prevent game from resetting it
            if (isSlowMoActive)
            {
                // Detect if something else changed timeScale
                if (Mathf.Abs(Time.timeScale - slowMoSpeed.Value) > 0.0001f)
                {
                    Logger.LogWarning($"OFBSlowMo: Detected external timeScale change to {Time.timeScale} from {slowMoSpeed.Value}.");
                    flashRed = true;
                    flashTimer = FlashDuration;
                }

                Time.timeScale = slowMoSpeed.Value;
            }
        }

        private void ApplySlowMotion()
        {
            normalTimeScale = Time.timeScale;
            Time.timeScale = slowMoSpeed.Value;
            Logger.LogInfo($"Slow motion activated: {slowMoSpeed.Value}x speed");
        }

        private void RestoreNormalSpeed()
        {
            Time.timeScale = normalTimeScale;
            Logger.LogInfo("Normal speed restored");
        }

        private void InitializeHudFontIfNeeded()
        {
            if (hudFontInitialized)
                return;

            hudFontInitialized = true;
            hudFont = null;

            if (string.IsNullOrWhiteSpace(fontNamesConfig.Value))
            {
                Logger.LogInfo("OFBSlowMo: FontNames config is empty; using default GUI font.");
                return;
            }

            // Get OS-installed font names once and intersect with the configured list.
            string[] osFonts;
            try
            {
                osFonts = Font.GetOSInstalledFontNames();
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"OFBSlowMo: Failed to query OS fonts: {ex.Message}. Falling back to default GUI font.");
                return;
            }

            string[] requestedNames = fontNamesConfig.Value.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var validNames = new System.Collections.Generic.List<string>();

            foreach (string rawName in requestedNames)
            {
                string requested = rawName.Trim();
                if (string.IsNullOrEmpty(requested))
                    continue;

                foreach (string osName in osFonts)
                {
                    if (string.Equals(osName, requested, StringComparison.OrdinalIgnoreCase))
                    {
                        validNames.Add(osName);
                        break;
                    }
                }
            }

            if (validNames.Count == 0)
            {
                Logger.LogWarning($"OFBSlowMo: None of the FontNames '{fontNamesConfig.Value}' matched OS-installed fonts. Using default GUI font.");
                return;
            }

            try
            {
                // Let Unity pick from the validated list only.
                hudFont = Font.CreateDynamicFontFromOSFont(validNames.ToArray(), 48);
                if (hudFont != null)
                {
                    hudFontName = hudFont.name;
                    Logger.LogInfo($"OFBSlowMo: Using HUD font '{hudFontName}' from validated list: {string.Join(", ", validNames)}.");
                }
                else
                {
                    Logger.LogWarning("OFBSlowMo: Font.CreateDynamicFontFromOSFont returned null for validated font list. Using default GUI font.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning($"OFBSlowMo: Exception while creating HUD font from validated list: {ex.Message}. Using default GUI font.");
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

                // Ensure HUD font resolved once
                InitializeHudFontIfNeeded();
                
                // Style for the text with shadow effect
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.fontSize = 48;
                textStyle.fontStyle = FontStyle.Bold;

                // Base color: red/white when active, green fade when previewing
                Color baseColor;
                if (isSlowMoActive)
                {
                    baseColor = flashRed ? Color.red : Color.white;
                }
                else
                {
                    // Preview color: green, fading out over PreviewDuration
                    float t = Mathf.Clamp01(previewTimer / PreviewDuration);
                    baseColor = new Color(0f, 1f, 0f, t);
                }

                textStyle.normal.textColor = baseColor;
                textStyle.alignment = TextAnchor.MiddleLeft;
                if (hudFont != null)
                {
                    textStyle.font = hudFont;
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

