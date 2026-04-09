using System.Text;
using BaseLib.BaseLibScenes;
using BaseLib.Commands;
using BaseLib.Config;
using Godot;
using Godot.Collections;
using HarmonyLib;
using MegaCrit.Sts2.Core.Nodes.Screens.MainMenu;

namespace BaseLib.Patches.Utils;

public partial class LogListener : Godot.Logger
{
    public override void _LogMessage(string message, bool error)
    {
        NLogWindow.AddLog(message);
    }

    public override void _LogError(string function, string file, int line, string code, string rationale, bool editorNotify, int errorType,
        Array<ScriptBacktrace> scriptBacktraces)
    {
        var errorName = ((ErrorType)errorType).ToString();
        StringBuilder msg = new StringBuilder().Append($"Error occurred [{errorName}]: {rationale}\n{code}\n{file}:{line} @ {function}()\n");
        foreach (var backtrace in scriptBacktraces)
        {
            if (backtrace.IsEmpty()) continue;
            msg.Append($"{backtrace.Format()}");
        }

        NLogWindow.AddLog(msg.ToString());
    }
}

[HarmonyPatch(typeof(NMainMenu), nameof(NMainMenu._Ready))]
class NMainMenuReadyOpenLogWindowPatch
{
    private static bool _hasOpenedOnStartup;

    [HarmonyPostfix]
    private static void Postfix()
    {
        if (_hasOpenedOnStartup || !BaseLibConfig.OpenLogWindowOnStartup) return;

        _hasOpenedOnStartup = true;
        OpenLogWindow.OpenWindow(stealFocus: false);
    }
}
