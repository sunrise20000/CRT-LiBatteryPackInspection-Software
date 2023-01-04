using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading;
using System.Threading.Tasks;
using Sicentury.Core.Pipelines;

namespace MECF.Framework.UI.Client.CenterViews.DataLogs.Core.Tests
{
    [TestClass()]
    public class TwoStagePipelineBasedTaskExecutorTests
    {
        private double Test1(double p, int sleep = 1000)
        {
            Console.WriteLine($"Invoke method {nameof(Test1)}, param = {p}");
            Thread.Sleep(sleep);
            var ret = p * p;
            Console.WriteLine($"Method {nameof(Test1)}, Result = {ret}");
            return ret;
        }

        private string Test2(double p, int sleep = 100)
        {
            Console.WriteLine($"Invoke method {nameof(Test2)}, param = {p}");
            Thread.Sleep(sleep);
            var ret = p.ToString("F6");
            Console.WriteLine($"Method {nameof(Test2)}, Result = {ret}");
            return ret;
        }

        [TestMethod()]
        public void StartTest()
        {
            var pl =
                new TwoStagePipelineBasedTaskExecutor<double, string>();

            pl.AppendFunc1(new Func<double>(() => Test1(1)));
            pl.AppendFunc1(new Func<double>(() => Test1(2)));
            pl.AppendFunc1(new Func<double>(() => Test1(3)));
            pl.AppendFunc1(new Func<double>(() => Test1(4)));
            pl.AppendFunc1(null);

            pl.AppendFunc2(new Func<double, string>(e => Test2(e)));
            pl.AppendFunc2(new Func<double, string>(e => Test2(e)));
            pl.AppendFunc2(new Func<double, string>(e => Test2(e)));
            pl.AppendFunc2(new Func<double, string>(e => Test2(e)));
            pl.AppendFunc2(null);

            var t = pl.Start(null);
            Task.WaitAll(t.ToArray());
        }
    }
}