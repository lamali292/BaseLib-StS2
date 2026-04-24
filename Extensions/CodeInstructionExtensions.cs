using BaseLib.Utils.Patching;
using HarmonyLib;

namespace BaseLib.Extensions;

public static class CodeInstructionExtensions
{
    /// <summary>
    /// Attempt to get the integer value of a load-constant instruction.
    /// If the instruction is not a load-constant instruction, returns false.
    /// </summary>
    public static bool TryGetIntValue(this CodeInstruction instruction, out int result)
    {
        result = 0;
        switch (instruction.opcode.Value)
        {
            case (int)OpCodeValues.Ldc_I4_0:
            case (int)OpCodeValues.Ldc_I4_1:
            case (int)OpCodeValues.Ldc_I4_2:
            case (int)OpCodeValues.Ldc_I4_3:
            case (int)OpCodeValues.Ldc_I4_4:
            case (int)OpCodeValues.Ldc_I4_5:
            case (int)OpCodeValues.Ldc_I4_6:
            case (int)OpCodeValues.Ldc_I4_7:
            case (int)OpCodeValues.Ldc_I4_8:
                result = instruction.opcode.Value - (int)OpCodeValues.Ldc_I4_0;
                return true;
            case (int)OpCodeValues.Ldc_I4_M1:
                result = -1;
                return true;
            case (int)OpCodeValues.Ldc_I4_S:
            case (int)OpCodeValues.Ldc_I4:
                result = (int) Convert.ToInt64(instruction.operand);
                return true;
        }

        return false;
    }
}