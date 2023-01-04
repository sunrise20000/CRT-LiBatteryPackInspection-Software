using Microsoft.VisualStudio.TestTools.UnitTesting;
using MECF.Framework.UI.Client.CenterViews.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sicentury.Core;

namespace MECF.Framework.UI.Client.CenterViews.Core.Tests
{
    [TestClass()]
    public class DateRangeHelperTests
    {
        [TestMethod()]
        public void SplitInToHoursTest()
        {
            var dr = new DateRangeHelper(
                DateTime.Parse("2022/8/2 20:00:00"),
                DateTime.Parse("2022/8/3 3:00:00"));

            var list = DateRangeHelper.SplitInToHours(dr, 1);
            Assert.AreEqual(7, list.Count());

            list = DateRangeHelper.SplitInToHours(dr, 8);
            Assert.AreEqual(2, list.Count());

            dr.Start = dr.Start.AddSeconds(1);
            list = DateRangeHelper.SplitInToHours(dr, 1);
            


            dr = new DateRangeHelper(
                DateTime.Parse("2022/8/3 17:59:59"),
                DateTime.Parse("2022/8/3 17:00:00"));
            list = DateRangeHelper.SplitInToHours(dr, 1);
            Assert.AreEqual(0, list.Count());
        }
    }
}