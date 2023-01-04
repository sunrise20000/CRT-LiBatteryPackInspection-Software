using MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Errors;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace MECF.Framework.RT.EquipmentLibrary.HardwareUnits.Robots.HwinRobort.Tests
{
    [TestClass()]
    public class HiwinRobotAggregatedErrorsTests
    {
        [TestMethod()]
        public void HiwinRobotAggregatedErrorsTest()
        {
            const string mockErrCmdResponse = "0002 0000 0000 0000 0021 0001";
            var ae = new HiwinRobotAggregatedErrors(mockErrCmdResponse);
            Assert.IsNotNull(ae);
            Assert.IsTrue(ae.Input.Error102);
            Assert.IsTrue(ae.AxisZ.Error306);
            Assert.IsTrue(ae.AxisZ.Error301);
            Assert.IsTrue(ae.Controller.Error401);

            var aeMsg = ae.AggregateErrorMessages;
            Assert.IsNotNull(aeMsg);
            Assert.AreEqual(4, aeMsg.Count);
        }
    }
}