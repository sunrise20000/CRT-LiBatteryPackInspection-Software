using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.RT.Simulator
{
	public class SimulatorCaseManager : Singleton<SimulatorCaseManager>
	{
		private const string FileName = "ExceptionCase.Obj";

		private PeriodicJob _thread;

		private Dictionary<string, bool> _dicValue = new Dictionary<string, bool>();

		public void Initialize()
		{
			if (File.Exists("ExceptionCase.Obj"))
			{
				SerializeStatic.Load(typeof(ExceptionCase), "ExceptionCase.Obj");
			}
			PropertyInfo[] properties = typeof(ExceptionCase).GetProperties();
			foreach (PropertyInfo propertyInfo in properties)
			{
				_dicValue[propertyInfo.Name] = (bool)propertyInfo.GetValue(null, null);
			}
			_thread = new PeriodicJob(1000, OnMonitor, "simualtor case serializer", isStartNow: true);
		}

		private bool OnMonitor()
		{
			try
			{
				PropertyInfo[] properties = typeof(ExceptionCase).GetProperties();
				foreach (PropertyInfo propertyInfo in properties)
				{
					if (_dicValue[propertyInfo.Name] != (bool)propertyInfo.GetValue(null, null))
					{
						SerializeStatic.Save(typeof(ExceptionCase), "ExceptionCase.Obj");
						break;
					}
				}
			}
			catch (Exception ex)
			{
				LOG.Write(ex);
			}
			return true;
		}
	}
}
