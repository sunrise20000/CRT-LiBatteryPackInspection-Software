using System;
using System.Runtime.Serialization;

namespace MECF.Framework.Common.Equipment
{
	[Serializable]
	[DataContract]
	public enum ModuleName
	{
		[EnumMember]
		System = 0,
		[EnumMember]
		LP1 = 1,
		[EnumMember]
		LP2 = 2,
		[EnumMember]
		LP3 = 3,
		[EnumMember]
		LP4 = 4,
		[EnumMember]
		LP5 = 5,
		[EnumMember]
		LP6 = 6,
		[EnumMember]
		LP7 = 7,
		[EnumMember]
		LP8 = 8,
		[EnumMember]
		LP9 = 9,
		[EnumMember]
		LP10 = 10,
		[EnumMember]
		VaccumRobot = 11,
		[EnumMember]
		LoadRobot = 12,
		[EnumMember]
		EfemRobot = 13,
		[EnumMember]
		TMRobot = 14,
		[EnumMember]
		Upender = 15,
		[EnumMember]
		EFEM = 16,
		[EnumMember]
		Aligner = 17,
		[EnumMember]
		Aligner1 = 18,
		[EnumMember]
		Aligner2 = 19,
		[EnumMember]
		AlignerA = 20,
		[EnumMember]
		AlignerB = 21,
		[EnumMember]
		LL1 = 22,
		[EnumMember]
		LL2 = 23,
		[EnumMember]
		LL3 = 24,
		[EnumMember]
		LL4 = 25,
		[EnumMember]
		LL5 = 26,
		[EnumMember]
		LL6 = 27,
		[EnumMember]
		LL7 = 28,
		[EnumMember]
		LL8 = 29,
		[EnumMember]
		LLA = 30,
		[EnumMember]
		LLB = 31,
		[EnumMember]
		LLC = 32,
		[EnumMember]
		LLD = 33,
		[EnumMember]
		LLE = 34,
		[EnumMember]
		LLF = 35,
		[EnumMember]
		LLG = 36,
		[EnumMember]
		LLH = 37,
		[EnumMember]
		VCE1 = 38,
		[EnumMember]
		VCE2 = 39,
		[EnumMember]
		VCEA = 40,
		[EnumMember]
		VCEB = 41,
		[EnumMember]
		TM = 42,
		[EnumMember]
		Cooling1 = 43,
		[EnumMember]
		Cooling2 = 44,
		[EnumMember]
		Buffer = 45,
		[EnumMember]
		Buffer1 = 46,
		[EnumMember]
		Buffer2 = 47,
		[EnumMember]
		Buffer3 = 48,
		[EnumMember]
		Buffer4 = 49,
		[EnumMember]
		Buffer5 = 50,
		[EnumMember]
		Buffer6 = 51,
		[EnumMember]
		PM = 52,
		[EnumMember]
		PM1 = 53,
		[EnumMember]
		PM2 = 54,
		[EnumMember]
		PM3 = 55,
		[EnumMember]
		PM4 = 56,
		[EnumMember]
		PM5 = 57,
		[EnumMember]
		PM6 = 58,
		[EnumMember]
		PM7 = 59,
		[EnumMember]
		PM8 = 60,
		[EnumMember]
		PMA = 61,
		[EnumMember]
		PMB = 62,
		[EnumMember]
		PMC = 63,
		[EnumMember]
		PMD = 64,
		[EnumMember]
		Spin1L = 65,
		[EnumMember]
		Spin1H = 66,
		[EnumMember]
		Spin2L = 67,
		[EnumMember]
		Spin2H = 68,
		[EnumMember]
		Spin3L = 69,
		[EnumMember]
		Spin3H = 70,
		[EnumMember]
		Spin4L = 71,
		[EnumMember]
		Spin4H = 72,
		[EnumMember]
		Spin5L = 73,
		[EnumMember]
		Spin5H = 74,
		[EnumMember]
		Spin6L = 75,
		[EnumMember]
		Spin6H = 76,
		[EnumMember]
		LDULD = 77,
		[EnumMember]
		BufferOut = 78,
		[EnumMember]
		BufferIn = 79,
		[EnumMember]
		Dryer = 80,
		[EnumMember]
		QDR = 81,
		[EnumMember]
		Robot = 82,
		[EnumMember]
		Handler = 83,
		[EnumMember]
		WIDReader1 = 84,
		[EnumMember]
		WIDReader2 = 85,
		[EnumMember]
		LL1IN = 86,
		[EnumMember]
		LL1OUT = 87,
		[EnumMember]
		LL2IN = 88,
		[EnumMember]
		LL2OUT = 89,
		[EnumMember]
		TurnOverStation = 90,
		[EnumMember]
		Host = 91,
		[EnumMember]
		PTR = 92,
		[EnumMember]
		Cooling = 93,
		[EnumMember]
		Cassette = 94,
		[EnumMember]
		CassInnerL = 95,
		[EnumMember]
		CassInnerR = 96,
		[EnumMember]
		CassOuterL = 97,
		[EnumMember]
		CassOuterR = 98,
		[EnumMember]
		CassAL = 99,
		[EnumMember]
		CassAR = 100,
		[EnumMember]
		CassBL = 101,
		[EnumMember]
		CassBR = 102,
		[EnumMember]
		Shuttle = 103,
		[EnumMember]
		CoolingBuffer1 = 104,
		[EnumMember]
		CoolingBuffer2 = 105,
		[EnumMember]
		LoadLock = 106,
		[EnumMember]
		SMIFLeft = 107,
		[EnumMember]
		SMIFRight = 108,
		[EnumMember]
		SMIFA = 109,
		[EnumMember]
		SMIFB = 110,
		[EnumMember]
		SPM1 = 111,
		[EnumMember]
		SPM2 = 112,
		[EnumMember]
		BRM1 = 113,
		[EnumMember]
		BRM2 = 114,
		[EnumMember]
		CCU = 115,
		[EnumMember]
		Buffer7 = 116,
		[EnumMember]
		Buffer8 = 117,
		[EnumMember]
		Robot1 = 118,
		[EnumMember]
		Robot2 = 119,
		[EnumMember]
		CassetteRobot = 120,
		[EnumMember]
		SignalTower = 121,
		[EnumMember]
		TurnStation = 122,
		[EnumMember]
		InternalCassette1 = 123,
		[EnumMember]
		InternalCassette2 = 124,
		[EnumMember]
		InternalCassette3 = 125,
		[EnumMember]
		InternalCassette4 = 126,
		[EnumMember]
		InternalCassette5 = 127,
		[EnumMember]
		InternalCassette6 = 128,
		[EnumMember]
		InternalCassette7 = 129,
		[EnumMember]
		InternalCassette8 = 130,
		[EnumMember]
		InternalCassette9 = 131,
		[EnumMember]
		InternalCassette10 = 132,
		[EnumMember]
		Prs1 = 133,
		[EnumMember]
		Prs2 = 134,
		[EnumMember]
		Prs3 = 135,
		[EnumMember]
		Prs4 = 136,
		[EnumMember]
		PreHeat = 137,
		[EnumMember]
		UnLoad = 138,
		[EnumMember]
		WaferRobot = 139,
		[EnumMember]
		TrayRobot = 140,
		[EnumMember]
		Load = 141
	}
}
