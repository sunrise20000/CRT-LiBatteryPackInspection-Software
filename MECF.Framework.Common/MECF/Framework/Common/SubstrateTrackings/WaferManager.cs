using System;
using System.Collections.Generic;
using System.Linq;
using Aitex.Core.Common;
using Aitex.Core.RT.DataCenter;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.SCCore;
using Aitex.Core.Util;
using FabConnect.SecsGemInterface.Common;
using MECF.Framework.Common.DBCore;
using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.Utilities;

namespace MECF.Framework.Common.SubstrateTrackings
{
	public class WaferManager : Singleton<WaferManager>
	{
		private Dictionary<string, WaferInfo> _dict = new Dictionary<string, WaferInfo>();

		private object _lockerWaferList = new object();

		private Dictionary<ModuleName, Dictionary<int, WaferInfo>> _locationWafers = new Dictionary<ModuleName, Dictionary<int, WaferInfo>>();

		public List<ModuleName> _onlyWaferModule = new List<ModuleName>();

		public List<ModuleName> _onlyTrayModule = new List<ModuleName>();

		public List<ModuleName> _WaferTrayModule = new List<ModuleName>();

		private const string EventWaferLeft = "WAFER_LEFT_POSITION";

		private const string EventWaferArrive = "WAFER_ARRIVE_POSITION";

		private const string Event_STS_AtSourcs = "STS_ATSOURCE";

		private const string Event_STS_AtWork = "STS_ATWORK";

		private const string Event_STS_AtDestination = "STS_ATDESTINATION";

		private const string Event_STS_Deleted = "STS_DELETED";

		private const string Event_STS_NeedProcessing = "STS_NEEDPROCESSING";

		private const string Event_STS_InProcessing = "STS_INPROCESSING";

		private const string Event_STS_Processed = "STS_PROCESSED";

		private const string Event_STS_Aborted = "STS_ABORTED";

		private const string Event_STS_Stopped = "STS_STOPPED";

		private const string Event_STS_Rejected = "STS_REJECTED";

		private const string Event_STS_Lost = "STS_LOST";

		private const string Event_STS_Skipped = "STS_SKIPPED";

		private const string Event_STS_Nostate = "STS_NOSTATE";

		public const string LotID = "LotID";

		public const string SubstDestination = "SubstDestination";

		public const string SubstHistory = "SubstHistory";

		public const string SubstID = "SubstID";

		public const string SubstLocID = "SubstLocID";

		public const string SubstLocState = "SubstLocState";

		public const string SubstProcState = "SubstProcState";

		public const string PrvSubstProcState = "PrvSubstProcState";

		public const string SubstSource = "SubstSource";

		public const string SubstState = "SubstState";

		public const string PrvSubstState = "PrvSubstState";

		public const string SubstType = "SubstType";

		public const string SubstUsage = "SubstUsage";

		public const string SubstMtrlStatus = "SubstMtrlStatus";

		public const string SubstSourceSlot = "SourceSlot";

		public const string SubstSourceCarrierID = "SourceCarrier";

		public const string SubstCurrentSlot = "Slot";

		private PeriodicJob _thread;

		private bool _needSerialize;

		public Dictionary<ModuleName, Dictionary<int, WaferInfo>> AllLocationWafers => _locationWafers;

		public bool Serialize()
		{
			if (!_needSerialize)
			{
				return true;
			}
			_needSerialize = false;
			try
			{
				if (_locationWafers != null)
				{
					BinarySerializer<Dictionary<ModuleName, Dictionary<int, WaferInfo>>>.ToStream(_locationWafers, "WaferManager");
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}

		public void Deserialize()
		{
			try
			{
				Dictionary<ModuleName, Dictionary<int, WaferInfo>> dictionary = BinarySerializer<Dictionary<ModuleName, Dictionary<int, WaferInfo>>>.FromStream("WaferManager");
				if (dictionary == null)
				{
					return;
				}
				foreach (KeyValuePair<ModuleName, Dictionary<int, WaferInfo>> item in dictionary)
				{
					if (ModuleHelper.IsLoadPort(item.Key))
					{
						foreach (KeyValuePair<int, WaferInfo> item2 in item.Value)
						{
							item2.Value.SetEmpty();
						}
					}
					if (_onlyTrayModule.Contains(item.Key))
					{
						foreach (KeyValuePair<int, WaferInfo> item3 in item.Value)
						{
							item3.Value.Status = WaferStatus.Empty;
						}
					}
					if (!_onlyWaferModule.Contains(item.Key))
					{
						continue;
					}
					foreach (KeyValuePair<int, WaferInfo> item4 in item.Value)
					{
						item4.Value.TrayState = WaferTrayStatus.Empty;
					}
				}
				_locationWafers = dictionary;
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
		}

		public void Initialize()
		{
			EV.Subscribe(new EventItem("Event", "WAFER_LEFT_POSITION", "Wafer Left"));
			EV.Subscribe(new EventItem("Event", "WAFER_ARRIVE_POSITION", "Wafer Arrived"));
			EV.Subscribe(new EventItem("Event", "STS_ATSOURCE", "Substrate transport state is AT_SOURCE."));
			EV.Subscribe(new EventItem("Event", "STS_ATWORK", "Substrate transport state is AT_WORK."));
			EV.Subscribe(new EventItem("Event", "STS_ATDESTINATION", "Substrate transport state is AT_DESTINATION."));
			EV.Subscribe(new EventItem("Event", "STS_DELETED", "Substrate transport state is DELETED."));
			EV.Subscribe(new EventItem("Event", "STS_NEEDPROCESSING", "Substrate process state is NEEDPROCESSING."));
			EV.Subscribe(new EventItem("Event", "STS_INPROCESSING", "Substrate process state is INPROCESSING."));
			EV.Subscribe(new EventItem("Event", "STS_PROCESSED", "Substrate process state is PROCESSED."));
			EV.Subscribe(new EventItem("Event", "STS_ABORTED", "Substrate process state is ABORTED."));
			EV.Subscribe(new EventItem("Event", "STS_STOPPED", "Substrate process state is STOPPED."));
			EV.Subscribe(new EventItem("Event", "STS_REJECTED", "Substrate process state is REJECTED."));
			EV.Subscribe(new EventItem("Event", "STS_LOST", "Substrate process state is LOST."));
			EV.Subscribe(new EventItem("Event", "STS_SKIPPED", "Substrate process state is SKIPPED."));
			EV.Subscribe(new EventItem("Event", "STS_NOSTATE", "Substrate process state is NOSTATE."));
			Deserialize();
			_thread = new PeriodicJob(1000, Serialize, "SerializeMonitorHandler", isStartNow: true);
		}

		public void SubscribeLocation(string module, int slotNumber, bool withWafer = true, bool withTray = true)
		{
			if (Enum.TryParse<ModuleName>(module, out var result))
			{
				if (withWafer && !withTray && !_onlyWaferModule.Contains(result))
				{
					_onlyWaferModule.Add(result);
				}
				else if (withTray && !withWafer && !_onlyTrayModule.Contains(result))
				{
					_onlyTrayModule.Add(result);
				}
				else if (withWafer && withTray && !_WaferTrayModule.Contains(result))
				{
					_WaferTrayModule.Add(result);
				}
				SubscribeLocation(result, slotNumber);
			}
			else
			{
				LOG.Write($"Failed SubscribeLocation, module name invalid, {module} ");
			}
		}

		public void SubscribeLocation(ModuleName module, int slotNumber)
		{
			if (!_locationWafers.ContainsKey(module))
			{
				_locationWafers[module] = new Dictionary<int, WaferInfo>();
				for (int i = 0; i < slotNumber; i++)
				{
					_locationWafers[module][i] = new WaferInfo();
					if (_onlyTrayModule.Contains(module))
					{
						_locationWafers[module][i].Status = WaferStatus.Empty;
					}
					if (_onlyWaferModule.Contains(module))
					{
						_locationWafers[module][i].TrayState = WaferTrayStatus.Empty;
					}
					if (_WaferTrayModule.Contains(module))
					{
						_locationWafers[module][i].Status = WaferStatus.Empty;
						_locationWafers[module][i].TrayState = WaferTrayStatus.Empty;
					}
				}
			}
			DATA.Subscribe(module.ToString(), "ModuleWaferList", () => _locationWafers[module].Values.ToArray());
		}

		public void WaferMoved(ModuleName moduleFrom, int slotFrom, ModuleName moduleTo, int slotTo)
		{
			UpdateWaferHistory(moduleFrom, slotFrom, SubstAccessType.Left);
			if (_locationWafers[moduleFrom][slotFrom].IsEmpty && _locationWafers[moduleFrom][slotFrom].TrayState == WaferTrayStatus.Empty)
			{
				LOG.Write($"Invalid wafer move, no wafer at source, {moduleFrom}{slotFrom + 1}=>{moduleTo}{slotTo + 1}");
				return;
			}
			if (!_locationWafers[moduleFrom][slotFrom].IsEmpty && !_locationWafers[moduleTo][slotTo].IsEmpty)
			{
				LOG.Write($"Invalid wafer move, destination has wafer, {moduleFrom}{slotFrom + 1}=>{moduleTo}{slotTo + 1}");
				return;
			}
			if (_locationWafers[moduleFrom][slotFrom].TrayState != 0 && _locationWafers[moduleTo][slotTo].TrayState != 0)
			{
				LOG.Write($"Invalid tray move, destination has tray, {moduleFrom}{slotFrom + 1}=>{moduleTo}{slotTo + 1}");
				return;
			}
			WaferTrayStatus trayState = _locationWafers[moduleFrom][slotFrom].TrayState;
			int trayUsedForWhichPM = _locationWafers[moduleFrom][slotFrom].TrayUsedForWhichPM;
			int trayOriginStation = _locationWafers[moduleFrom][slotFrom].TrayOriginStation;
			int trayOriginSlot = _locationWafers[moduleFrom][slotFrom].TrayOriginSlot;
			int trayProcessCount = _locationWafers[moduleFrom][slotFrom].TrayProcessCount;
			WaferTrayStatus trayState2 = _locationWafers[moduleTo][slotTo].TrayState;
			int trayUsedForWhichPM2 = _locationWafers[moduleTo][slotTo].TrayUsedForWhichPM;
			int trayOriginStation2 = _locationWafers[moduleTo][slotTo].TrayOriginStation;
			int trayOriginSlot2 = _locationWafers[moduleTo][slotTo].TrayOriginSlot;
			int trayProcessCount2 = _locationWafers[moduleTo][slotTo].TrayProcessCount;
			string waferOrigin = _locationWafers[moduleFrom][slotFrom].WaferOrigin;
			WaferInfo waferInfo = CopyWaferInfo(moduleTo, slotTo, _locationWafers[moduleFrom][slotFrom]);
			UpdateWaferHistory(moduleTo, slotTo, SubstAccessType.Arrive);
			DeleteWaferForMove(moduleFrom, slotFrom);
			if (_onlyWaferModule.Contains(moduleTo) && _WaferTrayModule.Contains(moduleFrom))
			{
				_locationWafers[moduleTo][slotTo].TrayState = WaferTrayStatus.Empty;
				_locationWafers[moduleTo][slotTo].TrayUsedForWhichPM = 0;
				_locationWafers[moduleTo][slotTo].TrayOriginStation = 0;
				_locationWafers[moduleTo][slotTo].TrayOriginSlot = 0;
				_locationWafers[moduleTo][slotTo].TrayProcessCount = 0;
				_locationWafers[moduleFrom][slotFrom].TrayState = trayState;
				_locationWafers[moduleFrom][slotFrom].TrayUsedForWhichPM = trayUsedForWhichPM;
				_locationWafers[moduleFrom][slotFrom].TrayOriginStation = trayOriginStation;
				_locationWafers[moduleFrom][slotFrom].TrayOriginSlot = trayOriginSlot;
				_locationWafers[moduleFrom][slotFrom].TrayProcessCount = trayProcessCount;
			}
			else if (_onlyWaferModule.Contains(moduleFrom) && _WaferTrayModule.Contains(moduleTo))
			{
				_locationWafers[moduleTo][slotTo].TrayState = trayState2;
				_locationWafers[moduleTo][slotTo].TrayUsedForWhichPM = trayUsedForWhichPM2;
				_locationWafers[moduleTo][slotTo].TrayOriginStation = trayOriginStation2;
				_locationWafers[moduleTo][slotTo].TrayOriginSlot = trayOriginSlot2;
				_locationWafers[moduleTo][slotTo].TrayProcessCount = trayProcessCount2;
				_locationWafers[moduleFrom][slotFrom].TrayState = WaferTrayStatus.Empty;
				_locationWafers[moduleFrom][slotFrom].TrayUsedForWhichPM = 0;
				_locationWafers[moduleFrom][slotFrom].TrayOriginStation = 0;
				_locationWafers[moduleFrom][slotFrom].TrayOriginSlot = 0;
				_locationWafers[moduleFrom][slotFrom].TrayProcessCount = 0;
			}
			else
			{
				_locationWafers[moduleTo][slotTo].TrayState = trayState;
				_locationWafers[moduleTo][slotTo].TrayUsedForWhichPM = trayUsedForWhichPM;
				_locationWafers[moduleTo][slotTo].TrayOriginStation = trayOriginStation;
				_locationWafers[moduleTo][slotTo].TrayOriginSlot = trayOriginSlot;
				_locationWafers[moduleTo][slotTo].TrayProcessCount = trayProcessCount;
				_locationWafers[moduleFrom][slotFrom].TrayState = WaferTrayStatus.Empty;
				_locationWafers[moduleFrom][slotFrom].TrayUsedForWhichPM = 0;
				_locationWafers[moduleFrom][slotFrom].TrayOriginStation = 0;
				_locationWafers[moduleFrom][slotFrom].TrayOriginSlot = 0;
				_locationWafers[moduleFrom][slotFrom].TrayProcessCount = 0;
			}
			EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferMoved, waferOrigin, moduleFrom.ToString(), slotFrom + 1, moduleTo.ToString(), slotTo + 1);
			WaferMoveHistoryRecorder.WaferMoved(waferInfo.InnerId.ToString(), moduleTo.ToString(), slotTo, waferInfo.Status.ToString());
			EV.Notify("WAFER_LEFT_POSITION", new SerializableDictionary<string, string>
			{
				{
					"SLOT_NO",
					(slotFrom + 1).ToString("D2")
				},
				{ "WAFER_ID", waferInfo.WaferID },
				{ "LOT_ID", waferInfo.LotId },
				{
					"CAR_ID",
					GetCarrierID(moduleFrom)
				},
				{
					"LEFT_POS_NAME",
					string.Format("{0}.{1}", moduleFrom, (slotFrom + 1).ToString("D2"))
				}
			});
			if (ModuleHelper.IsLoadPort(moduleFrom))
			{
				UpdateWaferTransportState(waferInfo.WaferID, SubstrateTransportStatus.AtWork);
				UpdateWaferE90State(waferInfo.WaferID, EnumE90Status.InProcess);
			}
			EV.Notify("WAFER_ARRIVE_POSITION", new SerializableDictionary<string, string>
			{
				{
					"SLOT_NO",
					(slotTo + 1).ToString("D2")
				},
				{ "WAFER_ID", waferInfo.WaferID },
				{ "LOT_ID", waferInfo.LotId },
				{
					"CAR_ID",
					GetCarrierID(moduleTo)
				},
				{
					"ARRIVE_POS_NAME",
					string.Format("{0}.{1}", moduleTo, (slotTo + 1).ToString("D2"))
				}
			});
			if (ModuleHelper.IsLoadPort(moduleTo))
			{
				if (waferInfo.SubstE90Status == EnumE90Status.InProcess)
				{
					UpdateWaferE90State(waferInfo.WaferID, EnumE90Status.Processed);
				}
				if (moduleTo == (ModuleName)waferInfo.OriginStation && waferInfo.SubstE90Status != EnumE90Status.Processed)
				{
					UpdateWaferTransportState(waferInfo.WaferID, SubstrateTransportStatus.AtSource);
				}
				else
				{
					UpdateWaferTransportState(waferInfo.WaferID, SubstrateTransportStatus.AtDestination);
				}
			}
			LOG.Write($"WaferMoved From {moduleFrom} to {moduleTo}   [{_locationWafers[moduleFrom][slotFrom].TrayState}_{_locationWafers[moduleFrom][slotFrom].TrayOriginStation}_{_locationWafers[moduleFrom][slotFrom].TrayOriginSlot}_{_locationWafers[moduleFrom][slotFrom].TrayProcessCount}]-->[{_locationWafers[moduleTo][slotTo].TrayState}_{_locationWafers[moduleTo][slotTo].TrayOriginStation}_{_locationWafers[moduleTo][slotTo].TrayOriginSlot}_{_locationWafers[moduleTo][slotTo].TrayProcessCount}]");
			_needSerialize = true;
		}

		public void TrayMoved(ModuleName moduleFrom, int slotFrom, ModuleName moduleTo, int slotTo)
		{
			WaferTrayStatus trayState = _locationWafers[moduleFrom][slotFrom].TrayState;
			UpdateWaferHistory(moduleFrom, slotFrom, SubstAccessType.Left);
			if (_locationWafers[moduleFrom][slotFrom].TrayState == WaferTrayStatus.Empty)
			{
				LOG.Write($"Invalid wafer move, no wafer at tray, {moduleFrom}{slotFrom + 1}=>{moduleTo}{slotTo + 1}");
				return;
			}
			if (_locationWafers[moduleTo][slotTo].TrayState != 0)
			{
				LOG.Write($"Invalid wafer move, destination has tray, {moduleFrom}{slotFrom + 1}=>{moduleTo}{slotTo + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[moduleTo][slotTo].TrayState = trayState;
				_locationWafers[moduleTo][slotTo].TrayUsedForWhichPM = _locationWafers[moduleFrom][slotFrom].TrayUsedForWhichPM;
				_locationWafers[moduleTo][slotTo].TrayOriginStation = _locationWafers[moduleFrom][slotFrom].TrayOriginStation;
				_locationWafers[moduleTo][slotTo].TrayOriginSlot = _locationWafers[moduleFrom][slotFrom].TrayOriginSlot;
				_locationWafers[moduleTo][slotTo].TrayProcessCount = _locationWafers[moduleFrom][slotFrom].TrayProcessCount;
				_locationWafers[moduleFrom][slotFrom].TrayState = WaferTrayStatus.Empty;
				_locationWafers[moduleFrom][slotFrom].TrayUsedForWhichPM = 0;
				_locationWafers[moduleFrom][slotFrom].TrayOriginStation = 0;
				_locationWafers[moduleFrom][slotFrom].TrayOriginSlot = 0;
				_locationWafers[moduleFrom][slotFrom].TrayProcessCount = 0;
				if (_onlyTrayModule.Contains(moduleTo))
				{
					_locationWafers[moduleTo][slotTo].WaferOrigin = $"{ModuleHelper.GetAbbr((ModuleName)_locationWafers[moduleTo][slotTo].TrayOriginStation)}.{_locationWafers[moduleTo][slotTo].TrayOriginSlot + 1:D2}";
					_locationWafers[moduleTo][slotTo].OriginSlot = _locationWafers[moduleFrom][slotFrom].TrayOriginSlot;
				}
			}
			_needSerialize = true;
			LOG.Write($"TrayMoved From {moduleFrom} to {moduleTo} [{_locationWafers[moduleFrom][slotFrom].TrayState}_{_locationWafers[moduleFrom][slotFrom].TrayOriginStation}_{_locationWafers[moduleFrom][slotFrom].TrayOriginSlot}_{_locationWafers[moduleFrom][slotFrom].TrayProcessCount}]-->[{_locationWafers[moduleTo][slotTo].TrayState}_{_locationWafers[moduleTo][slotTo].TrayOriginStation}_{_locationWafers[moduleTo][slotTo].TrayOriginSlot}_{_locationWafers[moduleTo][slotTo].TrayProcessCount}]");
		}

		public void WaferDuplicated(ModuleName moduleFrom, int slotFrom, ModuleName moduleTo, int slotTo)
		{
			UpdateWaferHistory(moduleFrom, slotFrom, SubstAccessType.Left);
			if (_locationWafers[moduleFrom][slotFrom].IsEmpty)
			{
				LOG.Write($"Invalid wafer move, no wafer at source, {moduleFrom}{slotFrom + 1}=>{moduleTo}{slotTo + 1}");
				return;
			}
			if (!_locationWafers[moduleTo][slotTo].IsEmpty)
			{
				LOG.Write($"Invalid wafer move, destination has wafer, {moduleFrom}{slotFrom + 1}=>{moduleTo}{slotTo + 1}");
				return;
			}
			string waferOrigin = _locationWafers[moduleFrom][slotFrom].WaferOrigin;
			WaferInfo waferInfo = CopyWaferInfo(moduleTo, slotTo, _locationWafers[moduleFrom][slotFrom]);
			UpdateWaferHistory(moduleTo, slotTo, SubstAccessType.Arrive);
			EV.PostMessage(ModuleName.System.ToString(), EventEnum.WaferMoved, waferOrigin, moduleFrom.ToString(), slotFrom + 1, moduleTo.ToString(), slotTo + 1);
			WaferMoveHistoryRecorder.WaferMoved(waferInfo.InnerId.ToString(), moduleTo.ToString(), slotTo, waferInfo.Status.ToString());
			EV.Notify("WAFER_LEFT_POSITION", new SerializableDictionary<string, string>
			{
				{
					"SLOT_NO",
					(slotFrom + 1).ToString("D2")
				},
				{
					"Slot",
					(slotFrom + 1).ToString("D2")
				},
				{ "WAFER_ID", waferInfo.WaferID },
				{ "SubstID", waferInfo.WaferID },
				{ "LOT_ID", waferInfo.LotId },
				{ "LotID", waferInfo.LotId },
				{
					"CAR_ID",
					GetCarrierID(moduleFrom)
				},
				{
					"CarrierID",
					GetCarrierID(moduleFrom)
				},
				{
					"LEFT_POS_NAME",
					string.Format("{0}.{1}", moduleFrom, (slotFrom + 1).ToString("D2"))
				}
			});
			if (ModuleHelper.IsLoadPort(moduleFrom))
			{
				UpdateWaferTransportState(waferInfo.WaferID, SubstrateTransportStatus.AtWork);
				UpdateWaferE90State(waferInfo.WaferID, EnumE90Status.InProcess);
			}
			EV.Notify("WAFER_ARRIVE_POSITION", new SerializableDictionary<string, string>
			{
				{
					"SLOT_NO",
					(slotTo + 1).ToString("D2")
				},
				{ "WAFER_ID", waferInfo.WaferID },
				{ "SubstID", waferInfo.WaferID },
				{ "LOT_ID", waferInfo.LotId },
				{ "LotID", waferInfo.LotId },
				{
					"CAR_ID",
					GetCarrierID(moduleTo)
				},
				{
					"CarrierID",
					GetCarrierID(moduleTo)
				},
				{
					"ARRIVE_POS_NAME",
					string.Format("{0}.{1}", moduleTo, (slotTo + 1).ToString("D2"))
				}
			});
			if (ModuleHelper.IsLoadPort(moduleTo))
			{
				if (waferInfo.SubstE90Status == EnumE90Status.InProcess)
				{
					UpdateWaferE90State(waferInfo.WaferID, EnumE90Status.Processed);
				}
				if (moduleTo == (ModuleName)waferInfo.OriginStation && waferInfo.SubstE90Status != EnumE90Status.Processed)
				{
					UpdateWaferTransportState(waferInfo.WaferID, SubstrateTransportStatus.AtSource);
				}
				else
				{
					UpdateWaferTransportState(waferInfo.WaferID, SubstrateTransportStatus.AtDestination);
				}
			}
			UpdateWaferProcessStatus(moduleFrom, slotFrom, EnumWaferProcessStatus.Failed);
			UpdateWaferProcessStatus(moduleTo, slotTo, EnumWaferProcessStatus.Failed);
			_needSerialize = true;
		}

		private string GetCarrierID(ModuleName module)
		{
			if (!ModuleHelper.IsLoadPort(module))
			{
				return "";
			}
			return DATA.Poll(module.ToString() + ".CarrierId").ToString();
		}

		public bool IsWaferSlotLocationValid(ModuleName module, int slot)
		{
			return _locationWafers.ContainsKey(module) && _locationWafers[module].ContainsKey(slot);
		}

		public WaferInfo[] GetWafers(ModuleName module)
		{
			if (!_locationWafers.ContainsKey(module))
			{
				return null;
			}
			return _locationWafers[module].Values.ToArray();
		}

		public WaferInfo[] GetWaferByProcessJob(string jobName)
		{
			List<WaferInfo> list = new List<WaferInfo>();
			foreach (KeyValuePair<ModuleName, Dictionary<int, WaferInfo>> locationWafer in _locationWafers)
			{
				foreach (KeyValuePair<int, WaferInfo> item in locationWafer.Value)
				{
					if (item.Value != null && !item.Value.IsEmpty && item.Value.ProcessJob != null && item.Value.ProcessJob.Name == jobName)
					{
						list.Add(item.Value);
					}
				}
			}
			return list.ToArray();
		}

		public WaferInfo[] GetWafer(string waferID)
		{
			List<WaferInfo> list = new List<WaferInfo>();
			foreach (KeyValuePair<ModuleName, Dictionary<int, WaferInfo>> locationWafer in _locationWafers)
			{
				foreach (KeyValuePair<int, WaferInfo> item in locationWafer.Value)
				{
					if (item.Value.WaferID == waferID)
					{
						list.Add(item.Value);
					}
				}
			}
			return list.ToArray();
		}

		public WaferInfo GetWafer(ModuleName module, int slot)
		{
			if (!_locationWafers.ContainsKey(module))
			{
				return null;
			}
			if (!_locationWafers[module].ContainsKey(slot))
			{
				return null;
			}
			return _locationWafers[module][slot];
		}

		public WaferInfo[] GetWafers(string Originalcarrier, int Originalslot)
		{
			List<WaferInfo> list = new List<WaferInfo>();
			foreach (KeyValuePair<ModuleName, Dictionary<int, WaferInfo>> locationWafer in _locationWafers)
			{
				foreach (KeyValuePair<int, WaferInfo> item in locationWafer.Value)
				{
					if (item.Value.OriginCarrierID == Originalcarrier && item.Value.OriginSlot == Originalslot)
					{
						list.Add(item.Value);
					}
				}
			}
			return list.ToArray();
		}

		public string GetWaferID(ModuleName module, int slot)
		{
			return IsWaferSlotLocationValid(module, slot) ? _locationWafers[module][slot].WaferID : "";
		}

		public WaferSize GetWaferSize(ModuleName module, int slot)
		{
			return IsWaferSlotLocationValid(module, slot) ? _locationWafers[module][slot].Size : WaferSize.WS0;
		}

		public bool CheckNoWafer(ModuleName module, int slot)
		{
			return IsWaferSlotLocationValid(module, slot) && _locationWafers[module][slot].IsEmpty;
		}

		public bool CheckNoWafer(string module, int slot)
		{
			return CheckNoWafer((ModuleName)Enum.Parse(typeof(ModuleName), module), slot);
		}

		public bool CheckHasWafer(ModuleName module, int slot)
		{
			return IsWaferSlotLocationValid(module, slot) && !_locationWafers[module][slot].IsEmpty;
		}

		public bool CheckWaferIsDummy(ModuleName module, int slot)
		{
			return IsWaferSlotLocationValid(module, slot) && !_locationWafers[module][slot].IsEmpty && _locationWafers[module][slot].Status == WaferStatus.Dummy;
		}

		public bool CheckWaferExistFlag(string moduleNo, string[] flagStrings, out string reason)
		{
			int num = 0;
			reason = string.Empty;
			if (!Enum.TryParse<ModuleName>("LP" + moduleNo, out var result))
			{
				reason = "Port Number Error";
				return false;
			}
			foreach (string text in flagStrings)
			{
				if (text == "1")
				{
					if (IsWaferSlotLocationValid(result, num) && _locationWafers[result][num].IsEmpty)
					{
						reason = "Flag Mis-Match";
						return false;
					}
				}
				else if (IsWaferSlotLocationValid(result, num) && !_locationWafers[result][num].IsEmpty)
				{
					reason = "Flag Mis-Match";
					return false;
				}
				num++;
			}
			return true;
		}

		public bool CheckHasWafer(string module, int slot)
		{
			return CheckHasWafer((ModuleName)Enum.Parse(typeof(ModuleName), module), slot);
		}

		public bool CheckNoTray(ModuleName module, int slot)
		{
			return IsWaferSlotLocationValid(module, slot) && _locationWafers[module][slot].TrayState == WaferTrayStatus.Empty;
		}

		public bool CheckHasTray(ModuleName module, int slot)
		{
			return IsWaferSlotLocationValid(module, slot) && _locationWafers[module][slot].TrayState != WaferTrayStatus.Empty;
		}

		public bool CheckWaferFull(ModuleName module)
		{
			Dictionary<int, WaferInfo> dictionary = _locationWafers[module];
			foreach (KeyValuePair<int, WaferInfo> item in dictionary)
			{
				if (item.Value.IsEmpty)
				{
					return false;
				}
			}
			return true;
		}

		public bool CheckWaferEmpty(ModuleName module)
		{
			Dictionary<int, WaferInfo> dictionary = _locationWafers[module];
			foreach (KeyValuePair<int, WaferInfo> item in dictionary)
			{
				if (!item.Value.IsEmpty)
				{
					return false;
				}
			}
			return true;
		}

		public bool CheckWafer(ModuleName module, int slot, WaferStatus state)
		{
			return IsWaferSlotLocationValid(module, slot) && _locationWafers[module][slot].Status == state;
		}

		public WaferInfo CreateWafer(ModuleName module, int slot, WaferStatus state)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Invalid wafer create, invalid parameter, {module}, {slot + 1}");
				return null;
			}
			string carrierGuid = "";
			string text = string.Empty;
			if (ModuleHelper.IsLoadPort(module) || ModuleHelper.IsCassette(module))
			{
				CarrierInfo carrier = Singleton<CarrierManager>.Instance.GetCarrier(module.ToString());
				if (carrier == null)
				{
					EV.PostMessage(ModuleName.System.ToString(), EventEnum.DefaultWarning, $"No carrier at {module}, can not create wafer");
					return null;
				}
				carrierGuid = carrier.InnerId.ToString();
				text = carrier.CarrierId;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].Status = state;
				_locationWafers[module][slot].ProcessState = EnumWaferProcessStatus.Idle;
				_locationWafers[module][slot].SubstE90Status = EnumE90Status.NeedProcessing;
				_locationWafers[module][slot].WaferID = GenerateWaferId(module, slot, text);
				_locationWafers[module][slot].WaferOrigin = GenerateOrigin(module, slot);
				_locationWafers[module][slot].Station = (int)module;
				_locationWafers[module][slot].Slot = slot;
				_locationWafers[module][slot].InnerId = Guid.NewGuid();
				_locationWafers[module][slot].TrayUsedForWhichPM = 0;
				_locationWafers[module][slot].OriginStation = (int)module;
				_locationWafers[module][slot].OriginSlot = slot;
				_locationWafers[module][slot].OriginCarrierID = text;
				_locationWafers[module][slot].LotId = "";
				_locationWafers[module][slot].HostLaserMark1 = "";
				_locationWafers[module][slot].HostLaserMark2 = "";
				_locationWafers[module][slot].LaserMarker = "";
				_locationWafers[module][slot].T7Code = "";
				_locationWafers[module][slot].PPID = "";
				_locationWafers[module][slot].TrayState = WaferTrayStatus.Normal;
				_locationWafers[module][slot].TrayOriginStation = (int)module;
				_locationWafers[module][slot].TrayOriginSlot = slot;
				_locationWafers[module][slot].TrayProcessCount = (SC.ContainsItem("System.DefaultTrayProcessCount") ? SC.GetValue<int>("System.DefaultTrayProcessCount") : 10);
				if (_onlyTrayModule.Contains(module))
				{
					_locationWafers[module][slot].Status = WaferStatus.Empty;
				}
				else if (_onlyWaferModule.Contains(module))
				{
					_locationWafers[module][slot].TrayState = WaferTrayStatus.Empty;
				}
				SubstHistory substHistory = new SubstHistory(module.ToString(), slot, DateTime.Now, SubstAccessType.Create);
				_locationWafers[module][slot].SubstHists = new SubstHistory[1] { substHistory };
				_dict[_locationWafers[module][slot].WaferID] = _locationWafers[module][slot];
			}
			UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.NeedProcessing);
			UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.AtSource);
			WaferDataRecorder.CreateWafer(_locationWafers[module][slot].InnerId.ToString(), carrierGuid, module.ToString(), slot, _locationWafers[module][slot].WaferID, _locationWafers[module][slot].ProcessState.ToString());
			_needSerialize = true;
			return _locationWafers[module][slot];
		}

		public WaferInfo CreateWafer(ModuleName module, int slot, WaferStatus state, WaferSize wz)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Invalid wafer create, invalid parameter, {module}, {slot + 1}");
				return null;
			}
			string carrierGuid = "";
			string text = string.Empty;
			if (ModuleHelper.IsLoadPort(module) || ModuleHelper.IsCassette(module))
			{
				CarrierInfo carrier = Singleton<CarrierManager>.Instance.GetCarrier(module.ToString());
				if (carrier == null)
				{
					EV.PostMessage(ModuleName.System.ToString(), EventEnum.DefaultWarning, $"No carrier at {module}, can not create wafer");
					return null;
				}
				carrierGuid = carrier.InnerId.ToString();
				text = carrier.CarrierId;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].Status = state;
				_locationWafers[module][slot].ProcessState = EnumWaferProcessStatus.Idle;
				_locationWafers[module][slot].SubstE90Status = EnumE90Status.NeedProcessing;
				_locationWafers[module][slot].WaferID = GenerateWaferId(module, slot, text);
				_locationWafers[module][slot].WaferOrigin = GenerateOrigin(module, slot);
				_locationWafers[module][slot].Station = (int)module;
				_locationWafers[module][slot].Slot = slot;
				_locationWafers[module][slot].InnerId = Guid.NewGuid();
				_locationWafers[module][slot].TrayUsedForWhichPM = 0;
				_locationWafers[module][slot].OriginStation = (int)module;
				_locationWafers[module][slot].OriginSlot = slot;
				_locationWafers[module][slot].OriginCarrierID = text;
				_locationWafers[module][slot].LotId = "";
				_locationWafers[module][slot].HostLaserMark1 = "";
				_locationWafers[module][slot].HostLaserMark2 = "";
				_locationWafers[module][slot].LaserMarker = "";
				_locationWafers[module][slot].T7Code = "";
				_locationWafers[module][slot].PPID = "";
				_locationWafers[module][slot].Size = wz;
				_locationWafers[module][slot].TrayState = WaferTrayStatus.Normal;
				_locationWafers[module][slot].TrayOriginStation = (int)module;
				_locationWafers[module][slot].TrayOriginSlot = slot;
				_locationWafers[module][slot].TrayProcessCount = (SC.ContainsItem("System.DefaultTrayProcessCount") ? SC.GetValue<int>("System.DefaultTrayProcessCount") : 10);
				if (_onlyTrayModule.Contains(module))
				{
					_locationWafers[module][slot].Status = WaferStatus.Empty;
				}
				else if (_onlyWaferModule.Contains(module))
				{
					_locationWafers[module][slot].TrayState = WaferTrayStatus.Empty;
				}
				SubstHistory substHistory = new SubstHistory(module.ToString(), slot, DateTime.Now, SubstAccessType.Create);
				_locationWafers[module][slot].SubstHists = new SubstHistory[1] { substHistory };
				_dict[_locationWafers[module][slot].WaferID] = _locationWafers[module][slot];
			}
			UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.NeedProcessing);
			UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.AtSource);
			WaferDataRecorder.CreateWafer(_locationWafers[module][slot].InnerId.ToString(), carrierGuid, module.ToString(), slot, _locationWafers[module][slot].WaferID, _locationWafers[module][slot].ProcessState.ToString());
			_needSerialize = true;
			return _locationWafers[module][slot];
		}

		public WaferInfo CreateTray(ModuleName module, int slot, WaferTrayStatus trayStatu = WaferTrayStatus.Normal)
		{
			if (_onlyWaferModule.Contains(module))
			{
				return null;
			}
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Invalid wafer create, invalid parameter, {module}, {slot + 1}");
				return null;
			}
			string carrierGuid = "";
			string text = string.Empty;
			if (ModuleHelper.IsLoadPort(module) || ModuleHelper.IsCassette(module))
			{
				CarrierInfo carrier = Singleton<CarrierManager>.Instance.GetCarrier(module.ToString());
				if (carrier == null)
				{
					EV.PostMessage(ModuleName.System.ToString(), EventEnum.DefaultWarning, $"No carrier at {module}, can not create wafer");
					return null;
				}
				carrierGuid = carrier.InnerId.ToString();
				text = carrier.CarrierId;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].Status = WaferStatus.Empty;
				_locationWafers[module][slot].ProcessState = EnumWaferProcessStatus.Idle;
				_locationWafers[module][slot].SubstE90Status = EnumE90Status.NeedProcessing;
				_locationWafers[module][slot].WaferID = GenerateWaferId(module, slot, text);
				_locationWafers[module][slot].WaferOrigin = GenerateOrigin(module, slot);
				_locationWafers[module][slot].Station = (int)module;
				_locationWafers[module][slot].Slot = slot;
				_locationWafers[module][slot].InnerId = Guid.NewGuid();
				_locationWafers[module][slot].TrayUsedForWhichPM = 0;
				_locationWafers[module][slot].OriginStation = (int)module;
				_locationWafers[module][slot].OriginSlot = slot;
				_locationWafers[module][slot].OriginCarrierID = text;
				_locationWafers[module][slot].LotId = "";
				_locationWafers[module][slot].HostLaserMark1 = "";
				_locationWafers[module][slot].HostLaserMark2 = "";
				_locationWafers[module][slot].LaserMarker = "";
				_locationWafers[module][slot].T7Code = "";
				_locationWafers[module][slot].PPID = "";
				_locationWafers[module][slot].TrayState = trayStatu;
				_locationWafers[module][slot].TrayOriginStation = (int)module;
				_locationWafers[module][slot].TrayOriginSlot = slot;
				_locationWafers[module][slot].TrayProcessCount = (SC.ContainsItem("System.DefaultTrayProcessCount") ? SC.GetValue<int>("System.DefaultTrayProcessCount") : 10);
				SubstHistory substHistory = new SubstHistory(module.ToString(), slot, DateTime.Now, SubstAccessType.Create);
				_locationWafers[module][slot].SubstHists = new SubstHistory[1] { substHistory };
				_dict[_locationWafers[module][slot].WaferID] = _locationWafers[module][slot];
			}
			UpdateWaferE90State(_locationWafers[module][slot].WaferID, EnumE90Status.NeedProcessing);
			UpdateWaferTransportState(_locationWafers[module][slot].WaferID, SubstrateTransportStatus.AtSource);
			WaferDataRecorder.CreateWafer(_locationWafers[module][slot].InnerId.ToString(), carrierGuid, module.ToString(), slot, _locationWafers[module][slot].WaferID, _locationWafers[module][slot].ProcessState.ToString());
			_needSerialize = true;
			return _locationWafers[module][slot];
		}

		public void DeleteWafer(ModuleName module, int slotFrom, int count = 1)
		{
			lock (_lockerWaferList)
			{
				for (int i = 0; i < count; i++)
				{
					int num = slotFrom + i;
					if (!IsWaferSlotLocationValid(module, num))
					{
						LOG.Write($"Invalid wafer delete, invalid parameter, {module}, {num + 1}");
						continue;
					}
					UpdateWaferTrayStatus(module, num, WaferTrayStatus.Empty);
					UpdateWaferE90State(_locationWafers[module][num].WaferID, EnumE90Status.None);
					UpdateWaferTransportState(_locationWafers[module][num].WaferID, SubstrateTransportStatus.None);
					WaferDataRecorder.DeleteWafer(_locationWafers[module][num].InnerId.ToString());
					_locationWafers[module][num].SetEmpty();
					_locationWafers[module][num].SubstHists = null;
					_dict.Remove(_locationWafers[module][num].WaferID);
				}
			}
			_needSerialize = true;
		}

		public void DeleteWaferOnly(ModuleName module, int slotFrom, int count = 1)
		{
			lock (_lockerWaferList)
			{
				for (int i = 0; i < count; i++)
				{
					int num = slotFrom + i;
					if (!IsWaferSlotLocationValid(module, num))
					{
						LOG.Write($"Invalid wafer delete, invalid parameter, {module}, {num + 1}");
						continue;
					}
					if (_onlyTrayModule.Contains(module))
					{
						return;
					}
					_locationWafers[module][num].Status = WaferStatus.Empty;
					_locationWafers[module][num].ProcessState = EnumWaferProcessStatus.Idle;
					_locationWafers[module][num].ProcessJobID = "";
					_locationWafers[module][num].ProcessJob = null;
				}
			}
			_needSerialize = true;
		}

		public void DeleteWaferForMove(ModuleName module, int slotFrom, int count = 1)
		{
			lock (_lockerWaferList)
			{
				for (int i = 0; i < count; i++)
				{
					int num = slotFrom + i;
					if (!IsWaferSlotLocationValid(module, num))
					{
						LOG.Write($"Invalid wafer delete, invalid parameter, {module}, {num + 1}");
						continue;
					}
					_locationWafers[module][num].SetEmpty();
					_locationWafers[module][num].SubstHists = null;
					_dict.Remove(_locationWafers[module][num].WaferID);
				}
			}
			_needSerialize = true;
		}

		public void ManualDeleteWafer(ModuleName module, int slotFrom, int count = 1)
		{
			for (int i = 0; i < count; i++)
			{
				int num = slotFrom + i;
				if (!IsWaferSlotLocationValid(module, num))
				{
					LOG.Write($"Invalid wafer delete, invalid parameter, {module}, {num + 1}");
					continue;
				}
				UpdateWaferE90State(_locationWafers[module][num].WaferID, EnumE90Status.Lost);
				UpdateWaferTransportState(_locationWafers[module][num].WaferID, SubstrateTransportStatus.None);
				UpdateWaferTrayStatus(_locationWafers[module][num].WaferID, WaferTrayStatus.Empty);
				UpdateWaferHistory(module, slotFrom, SubstAccessType.Delete);
				lock (_lockerWaferList)
				{
					WaferDataRecorder.DeleteWafer(_locationWafers[module][num].InnerId.ToString());
					_locationWafers[module][num].SetEmpty();
					_dict.Remove(_locationWafers[module][num].WaferID);
				}
			}
			_needSerialize = true;
		}

		public void UpdateWaferLaser(ModuleName module, int slot, string laserMarker)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferLaser, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].LaserMarker = laserMarker;
				WaferDataRecorder.SetWaferMarker(_locationWafers[module][slot].InnerId.ToString(), laserMarker);
			}
			_needSerialize = true;
		}

		public void UpdateWaferDestination(ModuleName module, int slot, string destCarrierID, int destslot)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferLaser, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].DestinationCarrierID = destCarrierID;
				_locationWafers[module][slot].DestinationSlot = destslot;
			}
			_needSerialize = true;
		}

		public void UpdateWaferDestination(ModuleName module, int slot, ModuleName destmodule, int destslot)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferLaser, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].DestinationStation = (int)destmodule;
				_locationWafers[module][slot].DestinationSlot = destslot;
			}
			_needSerialize = true;
		}

		public void UpdateWaferLaserWithScoreAndFileName(ModuleName module, int slot, string laserMarker, string laserMarkerScore, string fileName, string filePath)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferLaser, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].LaserMarker = laserMarker;
				_locationWafers[module][slot].LaserMarkerScore = laserMarkerScore;
				_locationWafers[module][slot].ImageFileName = fileName;
				_locationWafers[module][slot].ImageFilePath = filePath;
				WaferDataRecorder.SetWaferMarkerWithScoreAndFileName(_locationWafers[module][slot].InnerId.ToString(), laserMarker, laserMarkerScore, fileName, filePath);
			}
			_needSerialize = true;
		}

		public void UpdateWaferT7Code(ModuleName module, int slot, string T7Code)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferT7Code, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].T7Code = T7Code;
				WaferDataRecorder.SetWaferT7Code(_locationWafers[module][slot].InnerId.ToString(), T7Code);
			}
			_needSerialize = true;
		}

		public void UpdataWaferPPID(ModuleName module, int slot, string PPID)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferPPID, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].PPID = PPID;
				WaferDataRecorder.SetWaferSequence(_locationWafers[module][slot].InnerId.ToString(), PPID);
			}
			_needSerialize = true;
		}

		public void UpdateWaferT7CodeWithScoreAndFileName(ModuleName module, int slot, string t7Code, string t7CodeScore, string fileName, string filePath)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferT7Code, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].T7Code = t7Code;
				_locationWafers[module][slot].T7CodeScore = t7CodeScore;
				_locationWafers[module][slot].ImageFileName = fileName;
				_locationWafers[module][slot].ImageFilePath = filePath;
				WaferDataRecorder.SetWaferT7CodeWithScoreAndFileName(_locationWafers[module][slot].InnerId.ToString(), t7Code, t7CodeScore, fileName, filePath);
			}
			_needSerialize = true;
		}

		public void UpdateWaferTransFlag(ModuleName module, int slot, string flag)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferTransFlag, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].TransFlag = flag;
			}
			_needSerialize = true;
		}

		public void UpdateWaferNotch(ModuleName module, int slot, int angle)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferNotch, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].Notch = angle;
			}
			_needSerialize = true;
		}

		public void UpdateWaferProcessStatus(ModuleName module, int slot, EnumWaferProcessStatus status)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferProcessStatus, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].ProcessState = status;
				WaferDataRecorder.SetWaferStatus(_locationWafers[module][slot].InnerId.ToString(), status.ToString());
			}
			_needSerialize = true;
		}

		public void UpdateWaferProcessStatus(ModuleName module, int slot, ProcessStatus status)
		{
			switch (status)
			{
			case ProcessStatus.Busy:
				UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.InProcess);
				break;
			case ProcessStatus.Completed:
				UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.Completed);
				break;
			case ProcessStatus.Failed:
			case ProcessStatus.Abort:
				UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.Failed);
				break;
			case ProcessStatus.Idle:
				UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.Idle);
				break;
			case ProcessStatus.Wait:
				UpdateWaferProcessStatus(module, slot, EnumWaferProcessStatus.InProcess);
				break;
			}
		}

		public void UpdateWaferProcessStatus(string waferID, ProcessStatus status)
		{
			switch (status)
			{
			case ProcessStatus.Busy:
				UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.InProcess);
				break;
			case ProcessStatus.Completed:
				UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.Completed);
				break;
			case ProcessStatus.Failed:
				UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.Failed);
				break;
			case ProcessStatus.Idle:
				UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.Idle);
				break;
			case ProcessStatus.Wait:
				UpdateWaferProcessStatus(waferID, EnumWaferProcessStatus.InProcess);
				break;
			}
		}

		public void UpdateWaferId(ModuleName module, int slot, string waferId)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferId, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].WaferID = waferId;
			}
			UpdateWaferHistory(module, slot, SubstAccessType.UpdateWaferID);
			_needSerialize = true;
		}

		public void UpdateWaferJodID(ModuleName module, int slot, string pjID, string cjID)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferId, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].ProcessJobID = pjID;
				_locationWafers[module][slot].ControlJobID = cjID;
			}
			_needSerialize = true;
		}

		public void SlotMapVerifyOK(ModuleName module)
		{
			WaferInfo[] wafers = GetWafers(module);
			WaferInfo[] array = wafers;
			foreach (WaferInfo waferInfo in array)
			{
				lock (_lockerWaferList)
				{
					if (!waferInfo.IsEmpty)
					{
					}
				}
			}
			_needSerialize = true;
		}

		public void UpdateWaferTransportState(string waferid, SubstrateTransportStatus ststate)
		{
			if (string.IsNullOrEmpty(waferid))
			{
				return;
			}
			WaferInfo[] wafer = GetWafer(waferid);
			lock (_lockerWaferList)
			{
				WaferInfo[] array = wafer;
				foreach (WaferInfo waferInfo in array)
				{
					waferInfo.SubstTransStatus = ststate;
					SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>
					{
						{ "SubstID", waferInfo.WaferID },
						{ "LotID", waferInfo.LotId },
						{
							"SubstMtrlStatus",
							(int)waferInfo.ProcessState
						},
						{
							"SubstDestination",
							((ModuleName)waferInfo.DestinationStation).ToString()
						},
						{ "SubstHistory", waferInfo.SubstHists },
						{
							"SubstLocID",
							((ModuleName)waferInfo.Station).ToString()
						},
						{
							"SubstProcState",
							(int)waferInfo.SubstE90Status
						},
						{
							"SubstSource",
							((ModuleName)waferInfo.OriginStation).ToString()
						},
						{
							"SubstState",
							(int)waferInfo.SubstTransStatus
						}
					};
					if (ststate == SubstrateTransportStatus.AtWork)
					{
						EV.Notify("STS_ATWORK", dvid);
					}
					if (ststate == SubstrateTransportStatus.AtDestination)
					{
						EV.Notify("STS_ATDESTINATION", dvid);
					}
					if (ststate == SubstrateTransportStatus.AtSource)
					{
						EV.Notify("STS_ATSOURCE", dvid);
					}
					if (ststate == SubstrateTransportStatus.None)
					{
						EV.Notify("STS_DELETED", dvid);
					}
				}
			}
			_needSerialize = true;
			_needSerialize = true;
		}

		public void UpdateWaferE90State(string waferid, EnumE90Status E90state)
		{
			if (string.IsNullOrEmpty(waferid))
			{
				return;
			}
			WaferInfo[] wafer = GetWafer(waferid);
			lock (_lockerWaferList)
			{
				WaferInfo[] array = wafer;
				foreach (WaferInfo waferInfo in array)
				{
					if (waferInfo.SubstE90Status != E90state)
					{
						waferInfo.SubstE90Status = E90state;
						string text = "";
						if (ModuleHelper.IsLoadPort((ModuleName)waferInfo.Station))
						{
							text = Singleton<CarrierManager>.Instance.GetCarrier(((ModuleName)waferInfo.Station).ToString()).CarrierId;
						}
						SECsDataItem sECsDataItem = new SECsDataItem(SECsFormat.List);
						sECsDataItem.Add("SourceCarrier", waferInfo.OriginCarrierID ?? "");
						sECsDataItem.Add("SourceSlot", (waferInfo.OriginSlot + 1).ToString());
						sECsDataItem.Add("CurrentCarrier", text ?? "");
						sECsDataItem.Add("CurrentSlot", (waferInfo.Slot + 1).ToString());
						sECsDataItem.Add("LaserMark1", waferInfo.LaserMarker ?? "");
						sECsDataItem.Add("LaserMark1Score", waferInfo.LaserMarkerScore ?? "");
						sECsDataItem.Add("LaserMark2", waferInfo.T7Code ?? "");
						sECsDataItem.Add("LaserMark2Score", waferInfo.T7CodeScore ?? "");
						SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>
						{
							{ "SubstID", waferInfo.WaferID },
							{ "LotID", waferInfo.LotId },
							{
								"SubstMtrlStatus",
								(int)waferInfo.ProcessState
							},
							{
								"SubstDestination",
								((ModuleName)waferInfo.DestinationStation).ToString()
							},
							{ "SubstHistory", waferInfo.SubstHists },
							{
								"SubstLocID",
								((ModuleName)waferInfo.Station).ToString()
							},
							{
								"SubstProcState",
								(int)waferInfo.SubstE90Status
							},
							{
								"SubstState",
								(int)waferInfo.SubstTransStatus
							},
							{
								"SubstSource",
								((ModuleName)waferInfo.OriginStation).ToString()
							},
							{ "SourceCarrier", waferInfo.OriginCarrierID },
							{
								"SourceSlot",
								waferInfo.OriginSlot + 1
							},
							{
								"Slot",
								waferInfo.Slot + 1
							},
							{
								"LASERMARK",
								waferInfo.LaserMarker ?? ""
							},
							{
								"OCRScore",
								waferInfo.LaserMarkerScore ?? ""
							},
							{
								"CarrierID",
								text ?? ""
							},
							{
								"LASERMARK1",
								waferInfo.LaserMarker ?? ""
							},
							{
								"LASERMARK2",
								waferInfo.T7Code ?? ""
							},
							{
								"LASERMARK1SCORE",
								waferInfo.LaserMarkerScore ?? ""
							},
							{
								"LASERMARK2SCORE",
								waferInfo.T7CodeScore ?? ""
							},
							{ "PortID", waferInfo.Station },
							{ "PORT_ID", waferInfo.Station },
							{ "SubstInfoList", sECsDataItem }
						};
						if (E90state == EnumE90Status.InProcess)
						{
							EV.Notify("STS_INPROCESSING", dvid);
						}
						if (E90state == EnumE90Status.NeedProcessing)
						{
							EV.Notify("STS_NEEDPROCESSING", dvid);
						}
						if (E90state == EnumE90Status.Processed)
						{
							EV.Notify("STS_PROCESSED", dvid);
						}
						if (E90state == EnumE90Status.Aborted)
						{
							EV.Notify("STS_ABORTED", dvid);
						}
						if (E90state == EnumE90Status.Lost)
						{
							EV.Notify("STS_LOST", dvid);
						}
						if (E90state == EnumE90Status.Rejected)
						{
							EV.Notify("STS_REJECTED", dvid);
						}
						if (E90state == EnumE90Status.Skipped)
						{
							EV.Notify("STS_SKIPPED", dvid);
						}
						if (E90state == EnumE90Status.Stopped)
						{
							EV.Notify("STS_STOPPED", dvid);
						}
					}
				}
			}
			_needSerialize = true;
		}

		public void UpdateWaferE90State(ModuleName module, int slot, EnumE90Status E90state)
		{
			WaferInfo wafer = GetWafer(module, slot);
			if (wafer.IsEmpty)
			{
				return;
			}
			lock (_lockerWaferList)
			{
				wafer.SubstE90Status = E90state;
				string value = "";
				if (ModuleHelper.IsLoadPort((ModuleName)wafer.Station))
				{
					value = Singleton<CarrierManager>.Instance.GetCarrier(((ModuleName)wafer.Station).ToString()).CarrierId;
				}
				SerializableDictionary<string, object> dvid = new SerializableDictionary<string, object>
				{
					{ "SubstID", wafer.WaferID },
					{ "LotID", wafer.LotId },
					{
						"SubstMtrlStatus",
						(int)wafer.ProcessState
					},
					{
						"SubstDestination",
						((ModuleName)wafer.DestinationStation).ToString()
					},
					{ "SubstHistory", wafer.SubstHists },
					{
						"SubstLocID",
						((ModuleName)wafer.Station).ToString()
					},
					{
						"SubstProcState",
						(int)wafer.SubstE90Status
					},
					{
						"SubstState",
						(int)wafer.SubstTransStatus
					},
					{
						"SubstSource",
						((ModuleName)wafer.OriginStation).ToString()
					},
					{ "SourceCarrier", wafer.OriginCarrierID },
					{
						"SourceSlot",
						wafer.OriginSlot + 1
					},
					{
						"Slot",
						wafer.Slot + 1
					},
					{ "LASERMARK", wafer.LaserMarker },
					{ "OCRScore", wafer.LaserMarkerScore },
					{ "CarrierID", value }
				};
				if (E90state == EnumE90Status.InProcess)
				{
					EV.Notify("STS_INPROCESSING", dvid);
				}
				if (E90state == EnumE90Status.NeedProcessing)
				{
					EV.Notify("STS_NEEDPROCESSING", dvid);
				}
				if (E90state == EnumE90Status.Processed)
				{
					EV.Notify("STS_PROCESSED", dvid);
				}
				if (E90state == EnumE90Status.Aborted)
				{
					EV.Notify("STS_ABORTED", dvid);
				}
				if (E90state == EnumE90Status.Lost)
				{
					EV.Notify("STS_LOST", dvid);
				}
				if (E90state == EnumE90Status.Rejected)
				{
					EV.Notify("STS_REJECTED", dvid);
				}
				if (E90state == EnumE90Status.Skipped)
				{
					EV.Notify("STS_SKIPPED", dvid);
				}
				if (E90state == EnumE90Status.Stopped)
				{
					EV.Notify("STS_STOPPED", dvid);
				}
			}
			_needSerialize = true;
		}

		public void UpdateWaferHistory(ModuleName module, int slot, SubstAccessType accesstype)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferHistory, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				SubstHistory substHistory = new SubstHistory(module.ToString(), slot, DateTime.Now, accesstype);
				if (_locationWafers[module][slot].SubstHists == null)
				{
					_locationWafers[module][slot].SubstHists = new SubstHistory[1] { substHistory };
				}
				else
				{
					_locationWafers[module][slot].SubstHists = _locationWafers[module][slot].SubstHists.Concat(new SubstHistory[1] { substHistory }).ToArray();
				}
			}
			_needSerialize = true;
		}

		public void UpdateWaferLotId(ModuleName module, int slot, string lotId)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferLotId, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].LotId = lotId;
				WaferDataRecorder.SetWaferLotId(_locationWafers[module][slot].InnerId.ToString(), lotId);
				Singleton<CarrierManager>.Instance.UpdateCarrierLot(module.ToString(), lotId);
			}
			_needSerialize = true;
		}

		public void UpdateWaferHostLM1(ModuleName module, int slot, string lasermark1)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferHostLM1, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].HostLaserMark1 = lasermark1;
			}
			_needSerialize = true;
		}

		public void UpdateWaferHostLM2(ModuleName module, int slot, string lasermark2)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferHostLM2, invalid parameter, {module}, {slot + 1}");
				return;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].HostLaserMark2 = lasermark2;
			}
			_needSerialize = true;
		}

		public void UpdateWaferProcessStatus(string waferID, EnumWaferProcessStatus status)
		{
			WaferInfo[] wafer = GetWafer(waferID);
			lock (_lockerWaferList)
			{
				WaferInfo[] array = wafer;
				foreach (WaferInfo waferInfo in array)
				{
					waferInfo.ProcessState = status;
				}
			}
			_needSerialize = true;
		}

		public void UpdateWaferProcess(string waferID, string processId)
		{
			WaferInfo[] wafer = GetWafer(waferID);
			lock (_lockerWaferList)
			{
				WaferInfo[] array = wafer;
				foreach (WaferInfo waferInfo in array)
				{
					WaferDataRecorder.SetProcessInfo(waferInfo.InnerId.ToString(), processId);
				}
			}
			_needSerialize = true;
		}

		public WaferInfo CopyWaferInfo(ModuleName module, int slot, WaferInfo wafer)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed CopyWaferInfo, invalid parameter, {module}, {slot + 1}");
				return null;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].Update(wafer);
				_locationWafers[module][slot].Station = (int)module;
				_locationWafers[module][slot].Slot = slot;
			}
			return _locationWafers[module][slot];
		}

		public bool CheckWaferSize(ModuleName module, int slot, WaferSize size)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferProcessStatus, invalid parameter, {module}, {slot + 1}");
				return false;
			}
			WaferSize size2;
			lock (_lockerWaferList)
			{
				size2 = _locationWafers[module][slot].Size;
				if (size2 == WaferSize.WS0)
				{
					_locationWafers[module][slot].Size = size;
				}
			}
			if (size2 == WaferSize.WS0)
			{
				_needSerialize = true;
				return true;
			}
			return size2 == size;
		}

		public bool UpdateWaferSize(ModuleName module, int slot, WaferSize size)
		{
			if (!IsWaferSlotLocationValid(module, slot))
			{
				LOG.Write($"Failed UpdateWaferSize, invalid parameter, {module}, {slot + 1}");
				return false;
			}
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].Size = size;
			}
			_needSerialize = true;
			Serialize();
			return true;
		}

		private string GenerateWaferId(ModuleName module, int slot, string carrierID)
		{
			string text = "";
			text = ((!string.IsNullOrEmpty(carrierID)) ? carrierID : (module.ToString() + DateTime.Now.ToString("yyyyMMddHHmmssffff")));
			return string.Format(text + "." + (slot + 1).ToString("00"));
		}

		private string GenerateOrigin(ModuleName module, int slot)
		{
			return $"{ModuleHelper.GetAbbr(module)}.{slot + 1:D2}";
		}

		public void UpdateWaferTrayStatus(string waferid, WaferTrayStatus state)
		{
			if (string.IsNullOrEmpty(waferid))
			{
				return;
			}
			WaferInfo[] wafer = GetWafer(waferid);
			lock (_lockerWaferList)
			{
				WaferInfo[] array = wafer;
				WaferInfo[] array2 = array;
				foreach (WaferInfo waferInfo in array2)
				{
					if (waferInfo.TrayState != state)
					{
						waferInfo.TrayState = state;
					}
				}
			}
		}

		public void UpdateWaferTrayStatus(ModuleName module, int slot, WaferTrayStatus status)
		{
			lock (_lockerWaferList)
			{
				_locationWafers[module][slot].TrayState = status;
				if (status == WaferTrayStatus.Empty)
				{
					_locationWafers[module][slot].TrayProcessCount = 0;
				}
			}
		}
	}
}
