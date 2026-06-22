using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Logging;
using MegaCrit.Sts2.Core.Modding;

namespace GivingBack.GivingBackCode;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    public const string ModId = "GivingBack"; //Used for resource filepath
    public const string ResPath = $"res://{ModId}";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } = new(ModId, LogType.Generic);

    public static void Initialize()
    {
        Harmony harmony = new(ModId);

        try
        {
            harmony.PatchAll();
        }
        catch (Exception ex)
        {
            Logger.Error($"[GivingBack] PatchAll failed: {ex}");
            throw;
        }

        Logger.Info("GivingBack initialized.");
    }
}
