using BepInEx;
using HarmonyLib;

namespace OC2ThrowAny
{
    [BepInPlugin("dev.gua.overcooked.throwany", "Overcooked2 ThrowAny Plugin", "1.0")]
    [BepInProcess("Overcooked2.exe")]
    public class ThrowAnyPlugin : BaseUnityPlugin
    {
        static ThrowAnyPlugin pluginInstance;
        static Harmony patcher;

        public void Awake()
        {
            pluginInstance = this;
            patcher = new Harmony("dev.gua.overcooked.throwany");
            patcher.PatchAll(typeof(Patch));
            Patch.PatchInternal(patcher);
            foreach (var patched in patcher.GetPatchedMethods())
                Log("Patched: " + patched.FullDescription());
        }

        public static void Log(string msg) { pluginInstance.Logger.LogInfo(msg); }
    }
}