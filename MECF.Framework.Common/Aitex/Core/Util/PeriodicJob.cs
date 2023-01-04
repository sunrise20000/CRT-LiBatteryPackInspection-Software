using System;
using System.Threading;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Util
{
	public class PeriodicJob
	{
		private Thread _thread;

		private int _interval;

		private DeviceTimer _elapseTimer;

		private Func<bool> _func;

		private CancellationTokenSource _cancelFlag = new CancellationTokenSource();

		private ManualResetEvent _waitFlag = new ManualResetEvent(initialState: true);

		private ManualResetEvent _sleepFlag = new ManualResetEvent(initialState: false);

		private object _locker = new object();

		public PeriodicJob(int interval, Func<bool> func, string name, bool isStartNow = false, bool isBackground = true)
		{
			_thread = new Thread(ThreadFunction);
			_thread.Name = name;
			_thread.IsBackground = isBackground;
			_interval = interval;
			_func = func;
			_elapseTimer = new DeviceTimer();
			if (isStartNow)
			{
				Start();
			}
		}

		public void Start()
		{
			if (_thread != null)
			{
				_waitFlag.Set();
				if (!_thread.IsAlive)
				{
					_thread.Start(this);
					_elapseTimer.Start(0.0);
				}
			}
		}

		public void Pause()
		{
			_waitFlag.Reset();
		}

		public void Stop()
		{
			try
			{
				_sleepFlag.Set();
				_waitFlag.Set();
				_cancelFlag.Cancel();
				if (_thread == null)
				{
					return;
				}
				if (_thread.ThreadState != ThreadState.Suspended)
				{
					try
					{
						_thread.Abort();
					}
					catch (Exception ex)
					{
						LOG.Error("Thread aborted exception", ex);
					}
				}
				_thread = null;
			}
			catch (Exception ex2)
			{
				LOG.Error("Thread stop exception", ex2);
			}
		}

		private void ThreadFunction(object param)
		{
			PeriodicJob periodicJob = (PeriodicJob)param;
			periodicJob.Run();
		}

		public void ChangeInterval(int msInterval)
		{
			_interval = msInterval;
		}

		private void Run()
		{
			while (!_cancelFlag.IsCancellationRequested)
			{
				_waitFlag.WaitOne();
				_elapseTimer.Start(0.0);
				try
				{
					if (!_func())
					{
						break;
					}
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
				}
				_sleepFlag.WaitOne(Math.Max(_interval - (int)_elapseTimer.GetElapseTime(), 30));
			}
		}
	}
}
