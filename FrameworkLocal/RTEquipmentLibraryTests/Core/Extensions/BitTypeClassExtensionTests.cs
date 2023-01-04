using Microsoft.VisualStudio.TestTools.UnitTesting;
using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort;

namespace MECF.Framework.RT.EquipmentLibrary.Core.Extensions.Tests
{
    [TestClass()]
    public class BitTypeClassExtensionTests
    {

        [TestMethod()]
        public void ToBitTypeClassTest()
        {
            var bitSize = 16;

            for (var mockStatus = 0; mockStatus < bitSize; mockStatus++)
            {
                var stat = mockStatus.ToBitTypeClass<HiwinRobotStatus>(bitSize);
                Assert.IsNotNull(stat);


                var integValue = stat.FromBitTypeClass();

                Assert.AreEqual(integValue, mockStatus);
            }
        }

        [TestMethod()]
        public void AggregateMessagesTest()
        {
            // 所有Error Bit均设置为1，应该返回6条错误
            var stat = 0x4c70.ToBitTypeClass<HiwinRobotStatus>(16);
            var errs = stat.AggregateErrorMessages();

            Assert.IsNotNull(errs);
            Assert.AreEqual(6, errs.Count);
            Assert.IsTrue(errs.Contains("单轴或多轴马达发生错误"));
            Assert.IsTrue(errs.Contains("单个或多个极限开关被触发"));
            Assert.IsTrue(errs.Contains("单个或多个轴伺服马达归零未成功"));
            Assert.IsTrue(errs.Contains("单个或多个马达已被解除励磁中"));
            Assert.IsTrue(errs.Contains("已回原点但RW轴未在原点位置"));
            Assert.IsTrue(errs.Contains("控制器返回错误"));

            // 所有Bit置0，返回0条错误。
            stat = 0.ToBitTypeClass<HiwinRobotStatus>(16);
            errs = stat.AggregateErrorMessages();
            Assert.IsNotNull(errs);
            Assert.AreEqual(0, errs.Count);
        }
    }
}