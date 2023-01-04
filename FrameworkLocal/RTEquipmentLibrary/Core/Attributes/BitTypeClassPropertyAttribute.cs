using System;

namespace MECF.Framework.RT.EquipmentLibrary.Core.Attributes
{
    /// <summary>
    /// 用于标记HIWIN机械臂控制器状态Bit定义的属性。
    /// </summary>
    public class BitTypeClassPropertyAttribute : Attribute
    {
        #region Constructors
        
        public BitTypeClassPropertyAttribute(int bitIndex, string message) 
        {
            BitIndex = bitIndex;
            Message = message;
            IsErrorBit = false;
        }
        
        public BitTypeClassPropertyAttribute(int bitIndex, string message, bool isErrBit)
            : this(bitIndex, message)
        {

            IsErrorBit = isErrBit;
        }

        #endregion

        #region Properties

        /// <summary>
        /// 当前状态所在的位地址。
        /// </summary>
        public int BitIndex { get; }

        /// <summary>
        /// 当前状态信息。
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 当前Bit是否表示错误。
        /// </summary>
        public bool IsErrorBit { get; }

        #endregion
    }
}
