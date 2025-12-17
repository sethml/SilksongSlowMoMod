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
        
        private bool isSlowMoActive = false;
        private float normalTimeScale = 1.0f;

        private void Awake()
        {
            // Plugin startup logic
            Logger.LogInfo($"Plugin {PluginInfo.PLUGIN_GUID} is loaded!");

            // Configuration
            slowMoKey = Config.Bind(
                "General",
                "SlowMoKey",
                KeyCode.LeftShift,
                "Key to press to activate slow motion"
            );

            slowMoSpeed = Config.Bind(
                "General",
                "SlowMoSpeed",
                0.3f,
                "Time scale when slow motion is active (0.1 = 10% speed, 0.5 = 50% speed, etc.)"
            );

            toggleMode = Config.Bind(
                "General",
                "ToggleMode",
                false,
                "If true, slow motion toggles on/off. If false, hold key to activate."
            );
        }

        private void Update()
        {
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
            
            // Continuously enforce timeScale every frame to prevent game from resetting it
            if (isSlowMoActive)
            {
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

        private void OnGUI()
        {
            if (isSlowMoActive)
            {
                // Try to load a fancy font, fallback to default if not available
                Font? fancyFont = null;
                string[] fontNames = { "Arial Black", "Impact", "Comic Sans MS", "Trebuchet MS" };
                foreach (string fontName in fontNames)
                {
                    fancyFont = Font.CreateDynamicFontFromOSFont(fontName, 48);
                    if (fancyFont != null) break;
                }
                
                // Style for the text with shadow effect
                GUIStyle textStyle = new GUIStyle(GUI.skin.label);
                textStyle.fontSize = 48;
                textStyle.fontStyle = FontStyle.Bold;
                textStyle.normal.textColor = Color.white;
                textStyle.alignment = TextAnchor.MiddleLeft;
                if (fancyFont != null)
                {
                    textStyle.font = fancyFont;
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
                
                // Shadow
                GUIStyle shadowStyle = new GUIStyle(textStyle);
                shadowStyle.normal.textColor = new Color(0f, 0f, 0f, 0.6f);
                GUI.Label(new Rect(x + 3, y + 3, textWidth, textHeight), text, shadowStyle);
                
                // Main text
                GUI.Label(new Rect(x, y, textWidth, textHeight), text, textStyle);
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

