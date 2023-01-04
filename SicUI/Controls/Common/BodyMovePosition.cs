using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SicUI.Controls.Common
{
    public class BodyMovePoint
    {
        public int LidX;
        public int LidY;
        
    }

    public class BodyMovePosition
    {
        public BodyMovePoint StartPosition;
        public BodyMovePoint EndPosition;
    }

    public enum ChamberBodyInfo
    {
        Head,
        TopHead,
        MiddleTopHead,
    }
}
