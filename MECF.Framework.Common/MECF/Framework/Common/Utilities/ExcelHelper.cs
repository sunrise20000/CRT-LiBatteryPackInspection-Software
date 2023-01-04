using System;
using System.Data;
using Aitex.Core.RT.Log;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace MECF.Framework.Common.Utilities
{
	public class ExcelHelper
	{
		public static bool ExportToExcel(string filepath, DataSet ds, out string reason)
		{
			reason = string.Empty;
			try
			{
				SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(filepath, SpreadsheetDocumentType.Workbook);
				WorkbookPart workbookPart = spreadsheetDocument.AddWorkbookPart();
				workbookPart.Workbook = new Workbook();
				WorksheetPart worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
				worksheetPart.Worksheet = new Worksheet(new SheetData());
				Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());
				for (int i = 0; i < ds.Tables.Count; i++)
				{
					Sheet sheet = new Sheet
					{
						Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
						SheetId = 1u,
						Name = ds.Tables[i].TableName
					};
					sheets.Append(sheet);
					SheetData firstChild = worksheetPart.Worksheet.GetFirstChild<SheetData>();
					Row row = new Row();
					for (int j = 0; j < ds.Tables[i].Columns.Count; j++)
					{
						Cell cell = new Cell();
						cell.CellValue = new CellValue(ds.Tables[i].Columns[j].Caption);
						cell.DataType = new EnumValue<CellValues>(CellValues.String);
						row.Append(cell);
					}
					firstChild.Append(row);
					for (int k = 0; k < ds.Tables[i].Rows.Count; k++)
					{
						Row row2 = new Row();
						for (int l = 0; l < ds.Tables[i].Columns.Count; l++)
						{
							Cell cell2 = new Cell();
							object obj = ds.Tables[i].Rows[k][l];
							if (obj is DateTime dateTime)
							{
								cell2.CellValue = new CellValue(dateTime);
								cell2.DataType = new EnumValue<CellValues>(CellValues.String);
							}
							else if (obj is double)
							{
								cell2.CellValue = new CellValue(obj.ToString());
								cell2.DataType = new EnumValue<CellValues>(CellValues.Number);
							}
							else
							{
								cell2.CellValue = new CellValue(obj.ToString());
								cell2.DataType = new EnumValue<CellValues>(CellValues.String);
							}
							row2.Append(cell2);
						}
						firstChild.Append(row2);
					}
				}
				workbookPart.Workbook.Save();
				spreadsheetDocument.Close();
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
				reason = ex.Message;
				return false;
			}
			return true;
		}
	}
}
