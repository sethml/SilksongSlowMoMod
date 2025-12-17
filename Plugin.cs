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

