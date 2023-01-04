#define TRACE
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.IOCore;
using Aitex.Core.RT.Log;
using Aitex.Core.Util;

namespace MECF.Framework.RT.Core.IoProviders
{
	public abstract class IoProvider : IIoProvider
	{
		protected PeriodicJob _thread;

		protected IIoBuffer _buffer;

		protected R_TRIG _trigError = new R_TRIG();

		protected RD_TRIG _trigNotConnected = new RD_TRIG();

		protected string _source;

		protected XmlElement _nodeParameter;

		protected List<IoBlockItem> _blockSections;

		public string Module { get; set; }

		public string Name { get; set; }

		public bool IsOpened => State == IoProviderStateEnum.Opened;

		public IoProviderStateEnum State { get; set; }

		protected abstract void SetParameter(XmlElement nodeParameter);

		protected abstract void Open();

		protected abstract void Close();

		protected abstract bool[] ReadDi(int offset, int size);

		protected abstract short[] ReadAi(int offset, int size);

		protected abstract void WriteDo(int offset, bool[] buffer);

		protected abstract void WriteAo(int offset, short[] buffer);

		protected virtual float[] ReadAiFloat(int offset, int size)
		{
			return null;
		}

		protected virtual void WriteAoFloat(int offset, float[] buffer)
		{
		}

		public virtual void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, Dictionary<int, string> ioMappingPathFile)
		{
			Module = module;
			Name = name;
			_source = module + "." + name;
			_buffer = buffer;
			_nodeParameter = nodeParameter;
			_blockSections = lstBuffers;
			buffer.SetBufferBlock(_source, lstBuffers);
			buffer.SetIoMap(_source, ioMappingPathFile);
			SetParameter(nodeParameter);
			State = IoProviderStateEnum.Uninitialized;
			_thread = new PeriodicJob(50, OnTimer, name);
		}

		public virtual void Initialize(string module, string name, List<IoBlockItem> lstBuffers, IIoBuffer buffer, XmlElement nodeParameter, string ioMappingPathFile, string ioModule)
		{
			Module = module;
			Name = name;
			_source = module + "." + name;
			_buffer = buffer;
			_nodeParameter = nodeParameter;
			_blockSections = lstBuffers;
			buffer.SetBufferBlock(_source, lstBuffers);
			buffer.SetIoMapByModule(_source, 0, ioMappingPathFile, ioModule);
			SetParameter(nodeParameter);
			State = IoProviderStateEnum.Uninitialized;
			_thread = new PeriodicJob(50, OnTimer, name);
		}

		protected void SetState(IoProviderStateEnum newState)
		{
			State = newState;
		}

		public void TraceArray(bool[] data)
		{
			string[] array = new string[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				array[i++] = (data[i] ? "1" : "0");
			}
			Trace.WriteLine(string.Join(",", array));
		}

		protected virtual bool OnTimer()
		{
			if (State == IoProviderStateEnum.Uninitialized)
			{
				SetState(IoProviderStateEnum.Opening);
				Open();
			}
			if (State == IoProviderStateEnum.Opened)
			{
				try
				{
					bool flag = false;
					foreach (IoBlockItem blockSection in _blockSections)
					{
						if (blockSection.Type == IoType.DI)
						{
							bool[] array = ReadDi(blockSection.Offset, blockSection.Size);
							if (array != null)
							{
								_buffer.SetDiBuffer(_source, blockSection.Offset, array);
							}
						}
						else
						{
							if (blockSection.Type != IoType.AI)
							{
								continue;
							}
							short[] array2 = ReadAi(blockSection.Offset, blockSection.Size);
							if (array2 == null)
							{
								continue;
							}
							_buffer.SetAiBuffer(_source, blockSection.Offset, array2);
							if (blockSection.AIOType == typeof(float))
							{
								flag = true;
								_buffer.SetAiBufferFloat(_source, blockSection.Offset, Array.ConvertAll(array2, (Converter<short, float>)((short x) => x)).ToArray());
							}
						}
					}
					Dictionary<int, short[]> aoBuffer = _buffer.GetAoBuffer(_source);
					if (aoBuffer != null)
					{
						foreach (KeyValuePair<int, short[]> item in aoBuffer)
						{
							WriteAo(item.Key, item.Value);
							if (flag)
							{
								WriteAoFloat(item.Key, Array.ConvertAll(item.Value, (Converter<short, float>)((short x) => x)).ToArray());
							}
						}
					}
					Dictionary<int, bool[]> doBuffer = _buffer.GetDoBuffer(_source);
					if (doBuffer != null)
					{
						foreach (KeyValuePair<int, bool[]> item2 in doBuffer)
						{
							WriteDo(item2.Key, item2.Value);
						}
					}
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
					SetState(IoProviderStateEnum.Error);
					Close();
				}
			}
			_trigError.CLK = State == IoProviderStateEnum.Error;
			if (_trigError.Q)
			{
				EV.PostAlarmLog(Module, _source + " error");
			}
			_trigNotConnected.CLK = State != IoProviderStateEnum.Opened;
			if (_trigNotConnected.T)
			{
				EV.PostInfoLog(Module, _source + " connected");
			}
			if (_trigNotConnected.R)
			{
				EV.PostWarningLog(Module, _source + " not connected");
			}
			return true;
		}

		public virtual void Reset()
		{
			_trigError.RST = true;
			_trigNotConnected.RST = true;
		}

		public void Start()
		{
			if (_thread != null)
			{
				_thread.Start();
			}
		}

		public void Stop()
		{
			SetState(IoProviderStateEnum.Closing);
			Close();
			_thread.Stop();
		}

		public virtual bool SetValue(AOAccessor aoItem, short value)
		{
			return true;
		}

		public virtual bool SetValueFloat(AOAccessor aoItem, float value)
		{
			return true;
		}

		public virtual bool SetValue(DOAccessor doItem, bool value)
		{
			return true;
		}
	}
}
