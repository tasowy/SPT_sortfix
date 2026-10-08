using BepInEx;
using AutoSortFix.Patches;

namespace AutoSortFix
{
    [BepInPlugin("com.tasowy.AutoSortFix", "AutoSortFix", "0.1.1")]
    public class Plugin : BaseUnityPlugin
    {
        private void Awake()
        {
            Settings.Init(Config);
            SortGuard.LogSource = Logger;
            new SortArmPatch().Enable();
            new SortDonePatch().Enable();
            new SuppressAddPatch().Enable();
            new SuppressRemovePatch().Enable();
            Logger.LogInfo("AutoSortFix loaded!");
        }
    }
}
