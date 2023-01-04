using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Aitex.Core.RT.IOCore;
using Aitex.Core.Util;
using MECF.Framework.Common.IOCore;
using MECF.Framework.RT.Core.IoProviders;
using MECF.Framework.Simulator.Core.IoProviders;

namespace SicSimulator.Instances
{
    public class SimulatorModulePlc
    {
        private Random _rd = new Random();

        public ObservableCollection<NotifiableIoItem> DiItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> DoItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> AiItemList { get; set; }
        public ObservableCollection<NotifiableIoItem> AoItemList { get; set; }

        private string _source;

        public const int BufferSize =600;


        PeriodicJob _threadTimer;

        protected List<PlcBuffer> _buffers = new List<PlcBuffer>();

        public SimulatorModulePlc(int port, string source, string ioMapPathFile, string module)
           
        {
            _source = source;

            _buffers.Add(new PlcBuffer() { Buffer = new byte[BufferSize], Type = IoType.DI, Offset = 0, Size = BufferSize, BoolValue = new bool[BufferSize] });
            _buffers.Add(new PlcBuffer() { Buffer = new byte[BufferSize], Type = IoType.DO, Offset = 0, Size = BufferSize, BoolValue = new bool[BufferSize] });
            _buffers.Add(new PlcBuffer() { Buffer = new byte[BufferSize], Type = IoType.AI, Offset = 0, Size = BufferSize, FloatValue = new float[BufferSize] });
            _buffers.Add(new PlcBuffer() { Buffer = new byte[BufferSize], Type = IoType.AO, Offset = 0, Size = BufferSize, FloatValue = new float[BufferSize] });

            List<IoBlockItem> lstBuffers = new List<IoBlockItem>();
            lstBuffers.Add(new IoBlockItem() { Type = IoType.DI, Offset = 0, Size = BufferSize });
            lstBuffers.Add(new IoBlockItem() { Type = IoType.DO, Offset = 0, Size = BufferSize });
            lstBuffers.Add(new IoBlockItem() { Type = IoType.AI, Offset = 0, Size = BufferSize });
            lstBuffers.Add(new IoBlockItem() { Type = IoType.AO, Offset = 0, Size = BufferSize });

            IoManager.Instance.SetBufferBlock(source, lstBuffers);

            IoManager.Instance.SetIoMap(source, 0, ioMapPathFile, module);

            Init();

            _threadTimer = new PeriodicJob(200, OnTimer, "SimulatorModulePlc", true);
        }


        void Init()
        {
            if (DiItemList == null)
            {
                List<DIAccessor> diItems = IO.GetDiList(_source);
                if (diItems != null)
                {
                    DiItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var diItem in diItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = diItem.Name,
                            Index = diItem.Index,
                            Description = diItem.Description,
                            BoolValue = diItem.Value,
                            Address = diItem.Addr,
                            BlockOffset = diItem.BlockOffset,
                            BlockIndex = diItem.Index,
                        };
                        DiItemList.Add(item);
                    }
                }
            }

            if (DoItemList == null)
            {
                List<DOAccessor> doItems = IO.GetDoList(_source);
                if (doItems != null)
                {
                    DoItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var ioItem in doItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = ioItem.Name,
                            Index = ioItem.Index,
                            Description = ioItem.Description,
                            BoolValue = ioItem.Value,
                            Address = ioItem.Addr,
                            BlockOffset = ioItem.BlockOffset,
                            BlockIndex = ioItem.Index,
                        };
                        DoItemList.Add(item);
                    }
                }
            }

            if (AiItemList == null)
            {
                List<AIAccessor> aiItems = IO.GetAiList(_source);
                if (aiItems != null)
                {
                    AiItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var ioItem in aiItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = ioItem.Name,
                            Index = ioItem.Index,
                            Description = ioItem.Description,
                            ShortValue = ioItem.Value,
                            Address = ioItem.Addr,
                            BlockOffset = ioItem.BlockOffset,
                            BlockIndex = ioItem.Index,
                        };
                        AiItemList.Add(item);
                    }
                }
            }

            if (AoItemList == null)
            {
                List<AOAccessor> aoItems = IO.GetAoList(_source);
                if (aoItems != null)
                {
                    AoItemList = new ObservableCollection<NotifiableIoItem>();
                    foreach (var ioItem in aoItems)
                    {
                        NotifiableIoItem item = new NotifiableIoItem()
                        {
                            Name = ioItem.Name,
                            Index = ioItem.Index,
                            Description = ioItem.Description,
                            ShortValue = ioItem.Value,
                            Address = ioItem.Addr,
                            BlockOffset = ioItem.BlockOffset,
                            BlockIndex = ioItem.Index,
                        };
                        AoItemList.Add(item);
                    }
                }
            }
        }

        protected bool OnTimer()
        {
            bool[] diBuffer = new bool[BufferSize];
            if (DiItemList != null)
            {
                
                foreach (var notifiableIoItem in DiItemList)
                {
                    if (notifiableIoItem.HoldValue)
                    {
                        IO.DI[notifiableIoItem.Name].Value = notifiableIoItem.BoolValue;

                        diBuffer[notifiableIoItem.BlockIndex] = notifiableIoItem.BoolValue;
                    }
                    else
                    {
                        notifiableIoItem.BoolValue = IO.DI[notifiableIoItem.Name].Value;

                        diBuffer[notifiableIoItem.BlockIndex] = IO.DI[notifiableIoItem.Name].Value;
                    }
                    
                    notifiableIoItem.InvokePropertyChanged("BoolValue");
                }
            }

            if (DoItemList != null)
            {
                foreach (var notifiableIoItem in DoItemList)
                {
                    notifiableIoItem.BoolValue = IO.DO[notifiableIoItem.Name].Value;
                    notifiableIoItem.InvokePropertyChanged("BoolValue");
                }
            }

            if (AiItemList != null)
            {
                foreach (var notifiableIoItem in AiItemList)
                {
                    if (notifiableIoItem.HoldValue)
                    {
                        IO.AI[notifiableIoItem.Name].FloatValue = notifiableIoItem.FloatValue;
                    }

                    notifiableIoItem.FloatValue = IO.AI[notifiableIoItem.Name].FloatValue;
                    notifiableIoItem.InvokePropertyChanged("FloatValue");
                }
            }

            if (AoItemList != null)
            {
                foreach (var notifiableIoItem in AoItemList)
                {
                    notifiableIoItem.FloatValue = IO.AO[notifiableIoItem.Name].FloatValue;
                    notifiableIoItem.InvokePropertyChanged("FloatValue");
                }
            }

            foreach (var plcBuffer in _buffers)
            {
                //IO修改 ---> PLC
                if (plcBuffer.Type == IoType.DI)
                {
                    var ioBuffers = IoManager.Instance.GetDiBuffer(_source);
                    if (ioBuffers != null)
                    {
                        foreach (var ioBuffer in ioBuffers)
                        {
                            if (plcBuffer.Offset == ioBuffer.Key)
                            {
                                //plcBuffer.BoolValue = ioBuffer.Value;
                                for (int i = 0; i < plcBuffer.BoolValue.Length; i++)
                                {
                                    plcBuffer.BoolValue[i] = diBuffer[i];
                                }
                            }
                        }
                    }
                }

                // PLC --> IO
                if (plcBuffer.Type == IoType.DO)
                {
                    var ioBuffers = IoManager.Instance.GetDoBuffer(_source);
                    if (ioBuffers != null)
                    {
                        foreach (var buffer in ioBuffers)
                        {
                            if (plcBuffer.Offset == buffer.Key)
                            {
                                //IoManager.Instance.SetDoBuffer(_source, plcBuffer.Offset, plcBuffer.BoolValue);
                            }
                        }
                    }
                }

                //IO修改 ---> PLC
                if (plcBuffer.Type == IoType.AI)
                {
                    var ioBuffers = IoManager.Instance.GetAiBufferFloat(_source);
                    if (ioBuffers != null)
                    {
                        foreach (var buffer in ioBuffers)
                        {
                            if (plcBuffer.Offset == buffer.Key)
                            {
                                plcBuffer.FloatValue =  buffer.Value;
                            }
                        }
                    }
                }

                // PLC --> IO
                if (plcBuffer.Type == IoType.AO)
                {
                    var ioBuffers = IoManager.Instance.GetAoBuffer(_source);
                    if (ioBuffers != null)
                    {
                        foreach (var buffer in ioBuffers)
                        {
                            if (plcBuffer.Offset == buffer.Key)
                            {
                                //IoManager.Instance.SetAoBuffer(_source, plcBuffer.Offset, Array.ConvertAll<ushort, short>(plcBuffer.ShortValue, x => (short)x));
                            }
                        }
                    }
                }
            }

            return true;
        }
    }
}