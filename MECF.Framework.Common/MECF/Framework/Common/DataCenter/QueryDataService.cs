using System;
using System.Collections.Generic;
using System.Data;
using System.ServiceModel;
using Aitex.Core.RT.ConfigCenter;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.UI.ControlDataContext;
using Aitex.Core.Util;
using Aitex.Sorter.Common;
using Aitex.Sorter.RT.Module.DBRecorder;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.IOCore;
using MECF.Framework.Common.SCCore;

namespace MECF.Framework.Common.DataCenter
{
	[ServiceBehavior(InstanceContextMode = InstanceContextMode.Single, ConcurrencyMode = ConcurrencyMode.Multiple, UseSynchronizationContext = false)]
	public class QueryDataService : IQueryDataService
	{
		public Dictionary<string, object> PollData(IEnumerable<string> keys)
		{
			try
			{
				return DATA.PollData(keys);
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return null;
		}

		public string GetConfigFileContent()
		{
			return SC.GetConfigFileContent();
		}

		public string GetConfigFileContentByModule(string module)
		{
			return SC.GetConfigFileContent(module);
		}

		public List<SCConfigItem> GetConfigItemList()
		{
			return SC.GetItemList();
		}

		public object GetConfigByModule(string module, string key)
		{
			return CONFIG.Poll(module, key);
		}

		public Dictionary<string, object> PollConfigByModule(string module, IEnumerable<string> keys)
		{
			return CONFIG.PollConfig(module, keys);
		}

		public Dictionary<string, object> PollConfig(IEnumerable<string> keys)
		{
			return CONFIG.PollConfig(keys);
		}

		public object GetData(string key)
		{
			return DATA.Poll(key);
		}

		public object GetConfig(string key)
		{
			return CONFIG.Poll(key);
		}

		public List<NotifiableIoItem> GetDiList(string key)
		{
			List<DIAccessor> diList = IO.GetDiList(key);
			List<NotifiableIoItem> result = new List<NotifiableIoItem>();
			diList.ForEach(delegate(DIAccessor x)
			{
				result.Add(new NotifiableIoItem
				{
					Address = x.Addr,
					Name = x.Name,
					Description = x.Description,
					Index = x.Index,
					BoolValue = x.Value,
					Provider = x.Provider,
					BlockOffset = x.BlockOffset,
					BlockIndex = x.Index
				});
			});
			return result;
		}

		public List<NotifiableIoItem> GetDoList(string key)
		{
			List<DOAccessor> doList = IO.GetDoList(key);
			List<NotifiableIoItem> result = new List<NotifiableIoItem>();
			doList.ForEach(delegate(DOAccessor x)
			{
				result.Add(new NotifiableIoItem
				{
					Address = x.Addr,
					Name = x.Name,
					Description = x.Description,
					Index = x.Index,
					BoolValue = x.Value,
					Provider = x.Provider,
					BlockOffset = x.BlockOffset,
					BlockIndex = x.Index
				});
			});
			return result;
		}

		public List<NotifiableIoItem> GetAiList(string key)
		{
			List<AIAccessor> aiList = IO.GetAiList(key);
			List<NotifiableIoItem> result = new List<NotifiableIoItem>();
			aiList.ForEach(delegate(AIAccessor x)
			{
				result.Add(new NotifiableIoItem
				{
					Address = x.Addr,
					Name = x.Name,
					Description = x.Description,
					Index = x.Index,
					ShortValue = x.Value,
					Provider = x.Provider,
					BlockOffset = x.BlockOffset,
					BlockIndex = x.Index
				});
			});
			return result;
		}

		public List<NotifiableIoItem> GetAoList(string key)
		{
			List<AOAccessor> aoList = IO.GetAoList(key);
			List<NotifiableIoItem> result = new List<NotifiableIoItem>();
			aoList.ForEach(delegate(AOAccessor x)
			{
				result.Add(new NotifiableIoItem
				{
					Address = x.Addr,
					Name = x.Name,
					Description = x.Description,
					Index = x.Index,
					ShortValue = x.Value,
					Provider = x.Provider,
					BlockOffset = x.BlockOffset,
					BlockIndex = x.Index
				});
			});
			return result;
		}

		public List<EventItem> QueryDBEvent(string sql)
		{
			return EV.QueryDBEvent(sql);
		}

		public List<HistoryCarrierData> QueryDBCarrier(string sql)
		{
			return CarrierDataRecorder.QueryDBCarrier(sql);
		}

		public List<HistoryStatisticsOCRData> QueryDBOCRStatistics(string sql)
		{
			return OCRDataRecorder.QueryDBOCRStatistics(sql);
		}

		public List<HistoryFfuDiffPressureData> QueryDBFfuDiffPressureStatistics(string sql)
		{
			return FfuDiffPressureDataRecorder.FfuDiffPressureHistory(sql);
		}

		public List<HistoryOCRData> QueryDBOCRHistory(string sql)
		{
			return OCRDataRecorder.QueryDBOCRHistory(sql);
		}

		public List<StatsStatisticsData> QueryStatsDBStatistics(string sql)
		{
			List<StatsStatisticsData> list = new List<StatsStatisticsData>();
			return StatsDataRecorder.QueryStatsStatistics(sql);
		}

		public List<HistoryProcessData> QueryDBProcess(string sql)
		{
			return ProcessDataRecorder.QueryDBProcess(sql);
		}

		public List<HistoryWaferData> QueryDBWafer(string sql)
		{
			return WaferDataRecorder.QueryDBWafer(sql);
		}

		public List<HistoryMoveData> QueryDBMovement(string sql)
		{
			return WaferMoveHistoryRecorder.QueryDBMovement(sql);
		}

		public List<HistoryJobMoveData> QueryDBJobMovementByJobGuid(string jobGuid)
		{
			return JobMoveHistoryRecorder.QueryJobMovement(jobGuid);
		}

		public List<HistoryJobMoveData> QueryDBJobMovementByJobGuidAndStationName(string jobGuid, string stationName)
		{
			return JobMoveHistoryRecorder.QueryJobMovement(jobGuid, stationName);
		}

		public List<HistoryDataItem> GetHistoryData(IEnumerable<string> keys, string recipeRunGuid, string module)
		{
			return ProcessDataRecorder.GetHistoryData(keys, recipeRunGuid, module);
		}

		public List<HistoryDataItem> GetOneDayHistoryData(IEnumerable<string> keys, DateTime begin, string module)
		{
			return ProcessDataRecorder.GetOneDayHistoryData(keys, begin, module);
		}

		public List<HistoryDataItem> GetHistoryDataFromStartToEnd(IEnumerable<string> keys, DateTime begin, DateTime end, string module)
		{
			return ProcessDataRecorder.GetHistoryDataFromStartToEnd(keys, begin, end, module);
		}

		public DataTable QueryData(string sql)
		{
			return DataQuery.Query(sql);
		}

		public bool ExcuteTransAction(List<string> sql)
		{
			return DataQuery.ExcuteTransAction(sql);
		}

		public List<WaferHistoryWafer> GetWaferHistoryWafers(string id)
		{
			return WaferDataRecorder.GetWaferHistoryWafers(id);
		}

		public WaferHistoryRecipe GetWaferHistoryRecipe(string id)
		{
			return RecipeDataRecorder.GetWaferHistoryRecipe(id);
		}

		public List<WaferHistoryRecipe> GetWaferHistoryRecipes(string id)
		{
			return RecipeDataRecorder.GetWaferHistoryRecipes(id);
		}

		public List<WaferHistorySecquence> GetWaferHistorySecquences(string id)
		{
			return RecipeDataRecorder.GetWaferHistorySecquences(id);
		}

		public List<WaferHistoryMovement> GetWaferHistoryMovements(string id)
		{
			return WaferMoveHistoryRecorder.GetWaferHistoryMovements(id);
		}

		public List<WaferHistoryLot> QueryWaferHistoryLotsBySql(string sql)
		{
			return CarrierDataRecorder.QueryWaferHistoryLotsBySql(sql);
		}

		public List<WaferHistoryLot> GetWaferHistoryLots(DateTime startTime, DateTime endTime, string keyWord)
		{
			return CarrierDataRecorder.GetWaferHistoryLots(startTime, endTime, keyWord);
		}

		public string GetTypedConfigContent(string type, string contentPath)
		{
			return Singleton<TypedConfigManager>.Instance.GetTypedConfigContent(type, contentPath);
		}

		public void SetTypedConfigContent(string type, string contentPath, string content)
		{
			Singleton<TypedConfigManager>.Instance.SetTypedConfigContent(type, contentPath, content);
		}
	}
}
