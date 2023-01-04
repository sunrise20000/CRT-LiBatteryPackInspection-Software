#define DEBUG
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Aitex.Core.RT.Log;
using Aitex.Core.Utilities;

namespace Aitex.Core.RT.Fsm
{
	public class StateMachine : StateMachine<object>
	{
		public StateMachine(string name = "FSM", int initState = 268435440, int interval = 100)
			: base(name, initState, interval)
		{
		}
	}
	public class StateMachine<T> : IStateMachine
	{
		private int enterTime;

		private int interval;

		private CancellationTokenSource cancelToken;

		private BlockingCollection<KeyValuePair<int, object[]>> _msgQueue;

		private object _lockerMsgQueue = new object();

		private KeyValuePair<int, object[]> timeoutMsg = new KeyValuePair<int, object[]>(268435440, null);

		private Dictionary<int, Dictionary<int, Tuple<FsmFunc, int>>> transition;

		private Dictionary<int, Tuple<FsmFunc, int>> anyStateTransition;

		private Dictionary<int, Tuple<FsmFunc, int, FsmFunc>> enterExitTransition;

		private Dictionary<int, string> _stringState = new Dictionary<int, string>();

		private Dictionary<int, string> _stringMessage = new Dictionary<int, string>();

		private KeyValuePair<int, object[]> _msgInProcess = new KeyValuePair<int, object[]>(268435440, null);

		private long _msgCounter = 0L;

		public string Name { get; set; }

		public int ElapsedTime
		{
			get
			{
				int tickCount = Environment.TickCount;
				return (tickCount > enterTime) ? (tickCount - enterTime) : (int.MaxValue + tickCount - enterTime);
			}
		}

		public int EnterTime
		{
			set
			{
				enterTime = value;
			}
		}

		public int State { get; set; }

		public int PrevState { get; set; }

		public int LastMsg { get; private set; }

		public StateMachine(string name, int initState = 268435440, int interval = 100)
		{
			Name = name + " FSM";
			State = initState;
			PrevState = initState;
			this.interval = interval;
			cancelToken = new CancellationTokenSource();
			_msgQueue = new BlockingCollection<KeyValuePair<int, object[]>>();
			transition = new Dictionary<int, Dictionary<int, Tuple<FsmFunc, int>>>();
			anyStateTransition = new Dictionary<int, Tuple<FsmFunc, int>>();
			enterExitTransition = new Dictionary<int, Tuple<FsmFunc, int, FsmFunc>>();
			EnumLoop<FSM_STATE>.ForEach(delegate(FSM_STATE item)
			{
				MapState((int)item, item.ToString());
			});
			EnumLoop<FSM_MSG>.ForEach(delegate(FSM_MSG item)
			{
				MapMessage((int)item, item.ToString());
			});
		}

		public void Init(int initState, int intervalTimeMs)
		{
			State = initState;
			interval = intervalTimeMs;
		}

		public void Start()
		{
			OnEnterState(State);
		}

		public void Loop()
		{
			while (!cancelToken.IsCancellationRequested)
			{
				try
				{
					KeyValuePair<int, object[]> item = timeoutMsg;
					lock (_lockerMsgQueue)
					{
						if (!_msgQueue.TryTake(out item, interval, cancelToken.Token))
						{
							_msgInProcess = timeoutMsg;
						}
						else
						{
							_msgInProcess = item;
						}
					}
					if (anyStateTransition.ContainsKey(_msgInProcess.Key))
					{
						if (anyStateTransition[_msgInProcess.Key].Item1 != null && anyStateTransition[_msgInProcess.Key].Item1(_msgInProcess.Value) && anyStateTransition[_msgInProcess.Key].Item2 != State && anyStateTransition[_msgInProcess.Key].Item2 != 268435441)
						{
							OnExitState(268435442);
							OnExitState(State);
							enterTime = Environment.TickCount;
							LastMsg = item.Key;
							PrevState = State;
							State = anyStateTransition[_msgInProcess.Key].Item2;
							OnEnterState(268435442);
							OnEnterState(State);
						}
						if (_msgInProcess.Key != timeoutMsg.Key)
						{
							Interlocked.Decrement(ref _msgCounter);
						}
					}
					else if (!transition.ContainsKey(State))
					{
						if (_msgInProcess.Key != 268435440)
						{
							LOG.Warning("StateMachine [" + Name + "] no definition of state [" + GetStringState(State) + "] ");
						}
						if (_msgInProcess.Key != timeoutMsg.Key)
						{
							Interlocked.Decrement(ref _msgCounter);
						}
					}
					else if (!transition[State].ContainsKey(_msgInProcess.Key))
					{
						if (_msgInProcess.Key != 268435440)
						{
							LOG.Warning("StateMachine " + Name + " no definition of [state=" + GetStringState(State) + "], [message=" + GetStringMessage(_msgInProcess.Key) + "]");
						}
						if (_msgInProcess.Key != timeoutMsg.Key)
						{
							Interlocked.Decrement(ref _msgCounter);
						}
					}
					else
					{
						if ((transition[State][_msgInProcess.Key].Item1 == null || transition[State][_msgInProcess.Key].Item1(_msgInProcess.Value)) && transition[State][_msgInProcess.Key].Item2 != State && transition[State][_msgInProcess.Key].Item2 != 268435441)
						{
							OnExitState(268435442);
							OnExitState(State);
							enterTime = Environment.TickCount;
							PrevState = State;
							State = transition[State][_msgInProcess.Key].Item2;
							OnEnterState(268435442);
							OnEnterState(State);
						}
						if (_msgInProcess.Key != timeoutMsg.Key)
						{
							Interlocked.Decrement(ref _msgCounter);
						}
						_msgInProcess = timeoutMsg;
					}
				}
				catch (OperationCanceledException)
				{
					LOG.Info("FSM " + Name + " is canceled", isTraceOn: false);
				}
				catch (Exception ex2)
				{
					LOG.Error(ex2.StackTrace, ex2);
				}
			}
		}

		public void Stop()
		{
			cancelToken.Cancel();
		}

		private string GetStringState(int state)
		{
			if (_stringState.ContainsKey(state))
			{
				return _stringState[state];
			}
			return state.ToString();
		}

		private string GetStringMessage(int message)
		{
			if (_stringMessage.ContainsKey(message))
			{
				return _stringMessage[message];
			}
			return message.ToString();
		}

		public void MapState(int state, string stringState)
		{
			Debug.Assert(!_stringState.ContainsKey(state));
			_stringState[state] = stringState;
		}

		public void MapMessage(int message, string stringMessage)
		{
			Debug.Assert(!_stringMessage.ContainsKey(message));
			_stringMessage[message] = stringMessage;
		}

		public bool FindTransition(int state, int msg)
		{
			if (anyStateTransition.ContainsKey(msg))
			{
				return true;
			}
			if (transition.ContainsKey(state) && transition[state] != null)
			{
				Dictionary<int, Tuple<FsmFunc, int>> dictionary = transition[state];
				if (dictionary.ContainsKey(msg))
				{
					return true;
				}
			}
			return false;
		}

		public void Transition(int state, int msg, FsmFunc func, int next)
		{
			if (!transition.ContainsKey(state) || transition[state] == null)
			{
				transition[state] = new Dictionary<int, Tuple<FsmFunc, int>>();
			}
			transition[state][msg] = new Tuple<FsmFunc, int>(func, next);
		}

		public void AnyStateTransition(int msg, FsmFunc func, int next)
		{
			anyStateTransition[msg] = new Tuple<FsmFunc, int>(func, next);
		}

		public void EnterExitTransition(int state, FsmFunc enter, int msg, FsmFunc exit)
		{
			enterExitTransition[state] = new Tuple<FsmFunc, int, FsmFunc>(enter, msg, exit);
		}

		public void PostMsg(int msg, params object[] args)
		{
			Interlocked.Increment(ref _msgCounter);
			_msgQueue.Add(new KeyValuePair<int, object[]>(msg, args));
		}

		public void PostMsgWithoutLock(int msg, params object[] args)
		{
			Interlocked.Increment(ref _msgCounter);
			_msgQueue.Add(new KeyValuePair<int, object[]>(msg, args));
		}

		private void OnEnterState(int state)
		{
			if (enterExitTransition.ContainsKey(state))
			{
				if (enterExitTransition[state].Item1 != null)
				{
					enterExitTransition[state].Item1(null);
				}
				if (enterExitTransition[state].Item2 != 268435441)
				{
					PostMsg(enterExitTransition[state].Item2, null);
				}
			}
		}

		private void OnExitState(int state)
		{
			if (enterExitTransition.ContainsKey(state) && enterExitTransition[state].Item3 != null)
			{
				enterExitTransition[state].Item3(null);
			}
		}

		public bool CheckExecuted(int msg)
		{
			return _msgCounter == 0;
		}

		public bool CheckExecuted()
		{
			return _msgCounter == 0;
		}

		public bool CheckExecuted(int msg, out int currentMsg, out List<int> msgQueue)
		{
			currentMsg = 0;
			msgQueue = new List<int>();
			return _msgCounter == 0;
		}
	}
}
