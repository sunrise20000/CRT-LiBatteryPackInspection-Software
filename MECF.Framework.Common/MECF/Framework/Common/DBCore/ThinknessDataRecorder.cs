using System;
using Aitex.Core.RT.DBCore;

namespace MECF.Framework.Common.DBCore
{
	public class ThinknessDataRecorder
	{
		public static void InsertTrayThinkness(string waferGuid, string trayNumber, int trayCoating, int trayMax, string trayInnerNumber, int trayInnerCoating, int trayInnerMax, string ringInnerNumber, int ringInnerCoating, int ringInnerMax, string ringOuterNumber, int ringOuterCoating, int ringOuterMax)
		{
			string sql = $"INSERT INTO \"tray_thickness_data\"(\"wafer_guid\", \"tray_number\", \"tray_coating\", \"tray_max\", \"tray_inner_number\", \"tray_inner_coating\", \"tray_inner_max\", \"ring_inner_number\", \"ring_inner_coating\", \"ring_inner_max\", \"ring_outer_number\", \"ring_outer_coating\", \"ring_outer_max\") VALUES ('{waferGuid}', '{trayNumber}', '{trayCoating}', '{trayMax}', '{trayInnerNumber}', '{trayInnerCoating}','{trayInnerMax}', '{ringInnerNumber}', '{ringInnerCoating}', '{ringInnerMax}', '{ringOuterNumber}', '{ringOuterCoating}', '{ringOuterMax}');";
			DB.Insert(sql);
		}

		public static void InsertPMThinkness(string pmName, int coating1, int max1, int coating2, int max2)
		{
			string text = Guid.NewGuid().ToString();
			string sql = $"INSERT INTO \"pm_thickness_data\"(\"guid\", \"pm_name\", \"coating1\", \"max1\", \"coating2\", \"max2\") VALUES ('{text}', '{pmName}', '{coating1}', '{max1}', '{coating2}', '{max2}');";
			DB.Insert(sql);
		}
	}
}
