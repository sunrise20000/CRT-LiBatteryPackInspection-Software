using System;

namespace MECF.Framework.Common.Equipment
{
	public static class ModuleHelper
	{
		public static bool IsTurnOverStation(ModuleName unit)
		{
			return unit == ModuleName.TurnOverStation;
		}

		public static bool IsLoadPort(ModuleName unit)
		{
			return unit == ModuleName.LP1 || unit == ModuleName.LP2 || unit == ModuleName.LP3 || unit == ModuleName.LP4 || unit == ModuleName.LP5 || unit == ModuleName.LP6 || unit == ModuleName.LP7 || unit == ModuleName.LP8 || unit == ModuleName.LP9 || unit == ModuleName.LP10;
		}

		public static bool IsCoolingBuffer(ModuleName unit)
		{
			return unit == ModuleName.CoolingBuffer1 || unit == ModuleName.CoolingBuffer2;
		}

		public static bool IsCassette(ModuleName unit)
		{
			return unit == ModuleName.CassAL || unit == ModuleName.CassAR || unit == ModuleName.CassBL || unit == ModuleName.CassBR || unit == ModuleName.Cassette;
		}

		public static bool IsBuffer(ModuleName unit)
		{
			return unit == ModuleName.Buffer || unit == ModuleName.Buffer1 || unit == ModuleName.Buffer2 || unit == ModuleName.Buffer3 || unit == ModuleName.Buffer4 || unit == ModuleName.Buffer5 || unit == ModuleName.Buffer6 || unit == ModuleName.Buffer7 || unit == ModuleName.Buffer8 || unit == ModuleName.BufferIn || unit == ModuleName.BufferOut;
		}

		public static bool IsPm(string unit)
		{
			return IsPm(Converter(unit));
		}

		public static bool IsPm(ModuleName unit)
		{
			return unit == ModuleName.PM1 || unit == ModuleName.PM2 || unit == ModuleName.PM3 || unit == ModuleName.PM4 || unit == ModuleName.PM5 || unit == ModuleName.PM6 || unit == ModuleName.PM7 || unit == ModuleName.PM8 || unit == ModuleName.Spin1L || unit == ModuleName.Spin1H || unit == ModuleName.Spin2L || unit == ModuleName.Spin2H || unit == ModuleName.Spin3L || unit == ModuleName.Spin3H || unit == ModuleName.Spin4L || unit == ModuleName.Spin4H || unit == ModuleName.PM || unit == ModuleName.PMA || unit == ModuleName.PMB || unit == ModuleName.PMC || unit == ModuleName.PMD || unit == ModuleName.SPM1 || unit == ModuleName.SPM2 || unit == ModuleName.BRM1 || unit == ModuleName.BRM2;
		}

		public static bool IsLoadLock(string unit)
		{
			return IsLoadLock(Converter(unit));
		}

		public static bool IsLoadLock(ModuleName unit)
		{
			return unit == ModuleName.LLA || unit == ModuleName.LLB || unit == ModuleName.LL1 || unit == ModuleName.LL1IN || unit == ModuleName.LL1OUT || unit == ModuleName.LL2 || unit == ModuleName.LL2IN || unit == ModuleName.LL2OUT || unit == ModuleName.LL3 || unit == ModuleName.LL4 || unit == ModuleName.LL5 || unit == ModuleName.LL6 || unit == ModuleName.LL7 || unit == ModuleName.LL8 || unit == ModuleName.LLC || unit == ModuleName.LLD || unit == ModuleName.LoadLock || unit == ModuleName.VCEA || unit == ModuleName.VCEB;
		}

		public static bool IsCooling(string unit)
		{
			return IsCooling(Converter(unit));
		}

		public static bool IsCooling(ModuleName unit)
		{
			return unit == ModuleName.Cooling || unit == ModuleName.CoolingBuffer1 || unit == ModuleName.CoolingBuffer2;
		}

		public static bool IsAligner(string unit)
		{
			return IsAligner(Converter(unit));
		}

		public static bool IsAligner(ModuleName unit)
		{
			return unit == ModuleName.Aligner || unit == ModuleName.Aligner1 || unit == ModuleName.Aligner2;
		}

		public static bool IsRobot(string unit)
		{
			return IsRobot(Converter(unit));
		}

		public static bool IsRobot(ModuleName unit)
		{
			return unit.ToString().Contains("Robot");
		}

		public static bool IsEfemRobot(string unit)
		{
			return IsEfemRobot(Converter(unit));
		}

		public static bool IsEfemRobot(ModuleName unit)
		{
			return unit == ModuleName.EfemRobot;
		}

		public static bool IsTMRobot(string unit)
		{
			return IsTMRobot(Converter(unit));
		}

		public static bool IsTMRobot(ModuleName unit)
		{
			return unit == ModuleName.TMRobot;
		}

		public static bool IsTurnStation(ModuleName unit)
		{
			return unit == ModuleName.TurnStation;
		}

		public static string GetAbbr(ModuleName module)
		{
			return module switch
			{
				ModuleName.Aligner => "PA", 
				ModuleName.Robot => "RB", 
				_ => module.ToString(), 
			};
		}

		public static string GetE84LpName(string device)
		{
			string result = string.Empty;
			switch (device)
			{
			case "P1":
			case "LP1":
				result = "Loadport1E84";
				break;
			case "P2":
			case "LP2":
				result = "Loadport2E84";
				break;
			case "P3":
			case "LP3":
				result = "Loadport3E84";
				break;
			case "P4":
			case "LP4":
				result = "Loadport4E84";
				break;
			case "P5":
			case "LP5":
				result = "Loadport5E84";
				break;
			case "P6":
			case "LP6":
				result = "Loadport6E84";
				break;
			case "P7":
			case "LP7":
				result = "Loadport7E84";
				break;
			case "P8":
			case "LP8":
				result = "Loadport8E84";
				break;
			case "P9":
			case "LP9":
				result = "Loadport9E84";
				break;
			case "P10":
			case "LP10":
				result = "Loadport10E84";
				break;
			}
			return result;
		}

		public static ModuleName Converter(string module)
		{
			return (ModuleName)Enum.Parse(typeof(ModuleName), module);
		}

		public static ModuleName GetLoadPort(int index)
		{
			ModuleName[] array = new ModuleName[10]
			{
				ModuleName.LP1,
				ModuleName.LP2,
				ModuleName.LP3,
				ModuleName.LP4,
				ModuleName.LP5,
				ModuleName.LP6,
				ModuleName.LP7,
				ModuleName.LP8,
				ModuleName.LP9,
				ModuleName.LP10
			};
			return array[index];
		}
	}
}
