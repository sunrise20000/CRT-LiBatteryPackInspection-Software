using System;
using System.Runtime.InteropServices;

namespace Aitex.Core.Util
{
	public class DeviceTimer
	{
		private enum DeviceTimerState
		{
			TM_ST_IDLE = 0,
			TM_ST_BUSY = 1,
			TM_ST_TIMEOUT = 2
		}

		private DeviceTimerState _state;

		private long _startTime;

		private long _timeOut;

		private double _freq;

		private double _duration;

		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceCounter(out long lpPerformanceCount);

		[DllImport("Kernel32.dll")]
		private static extern bool QueryPerformanceFrequency(out long lpFrequency);

		public DeviceTimer()
		{
			if (!QueryPerformanceFrequency(out var lpFrequency))
			{
				throw new Exception("本计算机不支持高性能计数器");
			}
			_freq = (double)lpFrequency / 1000.0;
			SetState(DeviceTimerState.TM_ST_IDLE);
			QueryPerformanceCounter(out _startTime);
			_timeOut = _startTime;
			_duration = 0.0;
		}

		private void SetState(DeviceTimerState state)
		{
			_state = state;
		}

		private DeviceTimerState GetState()
		{
			return _state;
		}

		public double GetElapseTime()
		{
			QueryPerformanceCounter(out var lpPerformanceCount);
			return (double)(lpPerformanceCount - _startTime) / _freq;
		}

		public double GetTotalTime()
		{
			return _duration;
		}

		public void Stop()
		{
			SetState(DeviceTimerState.TM_ST_IDLE);
			QueryPerformanceCounter(out _startTime);
			_timeOut = _startTime;
			_duration = 0.0;
		}

		public void Start(double delay_ms)
		{
			QueryPerformanceCounter(out _startTime);
			_timeOut = Convert.ToInt64((double)_startTime + delay_ms * _freq);
			SetState(DeviceTimerState.TM_ST_BUSY);
			_duration = delay_ms;
		}

		public void Restart(double delay_ms)
		{
			_timeOut = Convert.ToInt64((double)_startTime + delay_ms * _freq);
			SetState(DeviceTimerState.TM_ST_BUSY);
			_duration = delay_ms;
		}

		public bool IsTimeout()
		{
			if (_state == DeviceTimerState.TM_ST_IDLE)
			{
			}
			QueryPerformanceCounter(out var lpPerformanceCount);
			if (_state == DeviceTimerState.TM_ST_BUSY && lpPerformanceCount >= _timeOut)
			{
				SetState(DeviceTimerState.TM_ST_TIMEOUT);
				return true;
			}
			if (_state == DeviceTimerState.TM_ST_TIMEOUT)
			{
				return true;
			}
			return false;
		}

		public bool IsIdle()
		{
			return _state == DeviceTimerState.TM_ST_IDLE;
		}
	}
}
