
using MECF.Framework.RT.Core.IoProviders.Siemens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Aitex.Core.RT.Event;
using MECF.Framework.RT.Core.IoProviders.Siemens.Transfer;
using MECF.Framework.RT.Core.IoProviders.Siemens.Net;

namespace SicRT.Equipments
{
    /// <summary>
    /// DEMO程序的一些静态变量信息
    /// </summary>
    public class DemoUtils
    {
        /// <summary>
        /// 统一的读取结果的数据解析，显示
        /// </summary>
        /// <typeparam name="T">类型对象</typeparam>
        /// <param name="result">读取的结果值</param>
        /// <param name="address">地址信息</param>
        /// <param name="textBox">输入的控件</param>
        //public static void ReadResultRender<T>(OperateResult<T> result, string address, TextBox textBox)
        //{
        //    if (result.IsSuccess)
        //    {
        //        if (result.Content is Array)
        //        {
        //            textBox.AppendText(DateTime.Now.ToString("[HH:mm:ss] ") + $"[{address}] {SoftBasic.ArrayFormat(result.Content)}{Environment.NewLine}");
        //        }
        //        else
        //        {
        //            textBox.AppendText(DateTime.Now.ToString("[HH:mm:ss] ") + $"[{address}] {result.Content}{Environment.NewLine}");
        //        }
        //    }
        //    else
        //    {
        //        MessageBox.Show(DateTime.Now.ToString("[HH:mm:ss] ") + $"[{address}] Read Failed {Environment.NewLine}Reason：{result.ToMessageShowString()}");
        //    }
       // }

        /// <summary>
        /// 统一的数据写入的结果显示
        /// </summary>
        /// <param name="result">写入的结果信息</param>
        /// <param name="address">地址信息</param>
        public static void WriteResultRender(OperateResult result, string address)
        {
            if (result.IsSuccess)
            {
                MessageBox.Show(DateTime.Now.ToString("[HH:mm:ss] ") + $"[{address}] Write Success");
            }
            else
            {
                MessageBox.Show(DateTime.Now.ToString("[HH:mm:ss] ") + $"[{address}] Write Failed {Environment.NewLine} Reason：{result.ToMessageShowString()}");
            }
        }

        /// <summary>
        /// 统一的数据写入的结果显示
        /// </summary>
        /// <param name="result">写入的结果信息</param>
        /// <param name="address">地址信息</param>
        public static void WriteResultRender(OperateResult result)
        {
            if (result.IsSuccess)
            {
                MessageBox.Show(DateTime.Now.ToString("[HH:mm:ss] ") + $"Success");
            }
            else
            {
                MessageBox.Show(DateTime.Now.ToString("[HH:mm:ss] ") + $"Failed {Environment.NewLine} Reason：{result.ToMessageShowString()}");
            }
        }

        /// <summary>
        /// 统一的数据写入的结果显示
        /// </summary>
        /// <param name="result">写入的结果信息</param>
        /// <param name="address">地址信息</param>
        public static void WriteResultRender(Func<OperateResult> write, string address)
        {
            try
            {
                OperateResult result = write();
                if (result.IsSuccess)
                {
                    MessageBox.Show(DateTime.Now.ToString("[HH:mm:ss] ") + $"[{address}] Write Success");
                }
                else
                {
                    MessageBox.Show(DateTime.Now.ToString("[HH:mm:ss] ") + $"[{address}] Write Failed {Environment.NewLine} Reason：{result.ToMessageShowString()}");
                }
            }
            catch (Exception ex)
            {
                // 主要是为了捕获写入的值不正确的情况
                MessageBox.Show("Data for writting is not corrent: " + ex.Message);
            }
        }

        public static bool BulkReadRenderResult(IReadWriteNet readWrite, string Adrress, ushort length,out byte[] data)
        {
            try
            {
                if (readWrite == null)
                {
                    data = null;
                    return false;
                }
                OperateResult<byte[]> read =  readWrite.Read(Adrress, length);// ushort.Parse(lengthTextBox.Text));
                if (read.IsSuccess)
                {
                    data = read.Content;
                    return true;
                }
                else
                {
                    data = null;
                    return false;
                }
            }
            catch (Exception ex)
            {
             // EV.PostAlarmLog ( ,"Read Failed：" + ex.Message);
            }

            data = null;
            return false;
        }
        public static bool BulkWriteFloatRenderResult(IReadWriteNet readWrite, string Adrress, float data)
        {
            try
            {
                if (readWrite == null)
                {
                    data =0;
                    return false;
                }
                OperateResult write = readWrite.Write(Adrress, data);

                return write.IsSuccess;
                
            }
            catch (Exception ex)
            {
               // MessageBox.Show("Read Failed：" + ex.Message);
            }
            return false;
        }
        public static bool BulkWriteByteRenderResult(IReadWriteNet readWrite, string Adrress, byte[] data)
        {
            try
            {
                if (readWrite == null)
                {
                    data = null;
                    return false;
                }
                OperateResult write = readWrite.Write(Adrress, data);
                return write.IsSuccess;
            }
            catch (Exception ex)
            {
                // MessageBox.Show("Read Failed：" + ex.Message);
            }
            return false;
        }
        public static readonly string IpAddressInputWrong = "IpAddress input wrong";
        public static readonly string PortInputWrong = "Port input wrong";
        public static readonly string SlotInputWrong = "Slot input wrong";
        public static readonly string BaudRateInputWrong = "Baud rate input wrong";
        public static readonly string DataBitsInputWrong = "Data bit input wrong";
        public static readonly string StopBitInputWrong = "Stop bit input wrong";
    }
}
