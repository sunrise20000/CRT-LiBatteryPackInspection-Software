using Aitex.Core.Util;

namespace Aitex.Core.RT.Tolerance
{
	public class ToleranceChecker
	{
		private DeviceTimer _timer = new DeviceTimer();

		private R_TRIG _trigger = new R_TRIG();

		private bool _started;

		public bool Trig => _trigger.Q;

		public bool Result => _trigger.M;

		public bool RST
		{
			set
			{
				_started = false;
				_trigger.RST = value;
			}
		}

		public ToleranceChecker()
		{
			_timer.Start(0.0);
		}

		public ToleranceChecker(double time)
		{
			_timer.Start(time * 1000.0);
		}

		public void Reset(double time)
		{
			_timer.Start(time * 1000.0);
			RST = true;
		}

		public void Monitor(double value, double min, double max, double time)
		{
			if (!_started || (value >= min && value <= max))
			{
				_started = true;
				_timer.Start(time * 1000.0);
			}
			_trigger.CLK = _timer.IsTimeout();
		}
	}
}
