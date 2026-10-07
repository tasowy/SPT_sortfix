using BepInEx;
using SPT_sortfix.Patches;

namespace SPT_sortfix
{
    [BepInPlugin("com.tasowy.SPT_sortfix", "SPT_sortfix", "1.0.1")]
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
            Logger.LogInfo("SPT_sortfix loaded!");
        }
    }
}
