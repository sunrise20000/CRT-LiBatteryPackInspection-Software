
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.EFEM
{
    public interface IEFEM
    {
        double ChamberPressure { get; }

        bool CheckSlitValveOpen(ModuleName module, ModuleName robot);

        bool CheckSlitValveClose(ModuleName module, ModuleName robot);

        bool SetSlitValve(ModuleName module, ModuleName robot, bool isOpen, out string reason);

    }
}

