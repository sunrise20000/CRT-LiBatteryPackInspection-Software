using System;
using System.Collections.Generic;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace Aitex.Core.UI.MVVM
{
	public abstract class TimerViewModelBase : ViewModelBase, ITimerViewModelBase
	{
		private PeriodicJob _timer;

		private static List<TimerViewModelBase> _lstAll = new List<TimerViewModelBase>();

		public static void StopAll()
		{
			foreach (TimerViewModelBase item in _lstAll)
			{
				item.Stop();
			}
		}

		public TimerViewModelBase(string name)
		{
			_timer = new PeriodicJob(1000, OnTimer, "UIUpdaterThread - " + name);
			_lstAll.Add(this);
		}

		protected virtual bool OnTimer()
		{
			try
			{
				Poll();
			}
			catch (Exception ex)
			{
				LOG.Error(ex.Message);
			}
			return true;
		}

		public void Start()
		{
			_timer.Start();
		}

		public void Stop()
		{
			_timer.Stop();
		}

		public void Dispose()
		{
			Stop();
		}

		protected abstract void Poll();

		public virtual void UpdateData()
		{
		}

		public virtual void EnableTimer(bool enable)
		{
			if (enable)
			{
				_timer.Start();
			}
			else
			{
				_timer.Pause();
			}
		}
	}
}
