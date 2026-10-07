using BepInEx.Configuration;

namespace SPT_sortfix
{
    internal static class Settings
    {
        public static ConfigEntry<bool> Enabled;

        public static void Init(ConfigFile config)
        {
            Enabled = config.Bind(
                "General",
                "Enabled",
                true,
                "When disabled the mod does nothing and vanilla sorting bug stays.");
        }
    }
}
