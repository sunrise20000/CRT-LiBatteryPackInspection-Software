/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\DataLogs\Core\TwoStagePipelineBasedTaskExecutor.cs
* @author Su Liang
* @Date 2022-08-01
*
* @copyright &copy Sicentury Inc.
*
* @brief The class is design to operate data querying and UI rendering processes in a 2-stage pipeline.
*
* @details
*    2 queues are used to buffer the querying functions and UI rendering functions, and execute them
*    one by one.
*    
* *****************************************************************************/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sicentury.Core.Pipelines
{
    public class TwoStagePipelineBasedTaskExecutor<TResult1, TResult2> : IDisposable
    {

        #region Variables

        /// <summary>
        /// 1级流水线中的任务全部完成事件。
        /// </summary>
        public event EventHandler Stage1Finished;

        public event EventHandler Stage2Finished;

        /// <summary>
        /// 1级流水线中的单个任务启动事件。
        /// </summary>
        public event EventHandler Stage1ActionStarted;

        /// <summary>
        /// 1级流水线中的单个任务完成事件。
        /// </summary>
        public event EventHandler Stage1ActionFinished;

        /// <summary>
        /// 2级流水线中的单个任务启动事件。
        /// </summary>
        public event EventHandler Stage2ActionStarted;

        /// <summary>
        /// 2级流水线中的单个任务完成事件。
        /// </summary>
        public event EventHandler Stage2ActionFinished;


        private readonly ConcurrentQueue<PipelineMethodInvoker<TResult1>> _queueAction1;
        private readonly ConcurrentQueue<PipelineMethodInvoker<TResult1, TResult2>> _queueAction2;
        private readonly ConcurrentQueue<TResult1> _queueAction1Result;
        private readonly ConcurrentQueue<TResult2> _queueAction2Result;

        private bool _isDisposed;
        private readonly int _workerThreadsMin;
        private readonly int _mIocMin;

        #endregion

        #region Constructors

        public TwoStagePipelineBasedTaskExecutor()
        {
            //ThreadPool.GetMinThreads(out _workerThreadsMin, out _mIocMin);
            //ThreadPool.SetMinThreads(100, 100);

            _queueAction1 = new ConcurrentQueue<PipelineMethodInvoker<TResult1>>();
            _queueAction2 = new ConcurrentQueue<PipelineMethodInvoker<TResult1, TResult2>>();
            _queueAction1Result = new ConcurrentQueue<TResult1>();
            _queueAction2Result = new ConcurrentQueue<TResult2>();
        }

        #endregion

        #region Properties

        #endregion

        #region Methods

        private Task ExecuteFunc1(CancellationTokenSource cancellation)
        {
            return Task.Run(() =>
            {
                while (_isDisposed == false)
                {
                    if (_queueAction1.Count <= 0)
                    {
                        Thread.Sleep(1);
                        if (cancellation?.Token.IsCancellationRequested == true)
                            return;
                        continue;
                    }

                    if (!_queueAction1.TryDequeue(out var invoker))
                    {
                        Thread.Sleep(1);

                        if (cancellation?.Token.IsCancellationRequested == true)
                            return;

                        continue;
                    }

                    if (invoker.IsEmpty)
                    {
                        // 列队结束
                        Stage1Finished?.Invoke(this, System.EventArgs.Empty);
                        break;
                    }

                    Stage1ActionStarted?.Invoke(this, System.EventArgs.Empty);
                    _queueAction1Result.Enqueue(invoker.Invoke());
                    Stage1ActionFinished?.Invoke(this, System.EventArgs.Empty);

                    Thread.Sleep(1);

                    if (cancellation?.Token.IsCancellationRequested == true)
                        return;
                }
            });
        }

        private Task ExecuteFunc2(CancellationTokenSource cancellation)
        {
            return Task.Run(() =>
            {
                while (_isDisposed == false)
                {
                    if (_queueAction2.Count <= 0)
                    {
                        Thread.Sleep(1);
                        if (cancellation?.Token.IsCancellationRequested == true)
                            return;
                        continue;
                    }

                    if (!_queueAction2.TryDequeue(out var invoker))
                    {
                        Thread.Sleep(1);

                        if (cancellation?.Token.IsCancellationRequested == true)
                            return;

                        continue;
                    }

                    if (invoker.IsEmpty)
                    {
                        // 列队结束
                        Stage2Finished?.Invoke(this, System.EventArgs.Empty);
                        return;
                    }

                    while (_isDisposed == false)
                    {
                        // 等待 Stage 1 方法返回的结果。
                        if (_queueAction1Result.Count > 0)
                            break;

                        Thread.Sleep(1);

                        if (cancellation?.Token.IsCancellationRequested == true)
                            return;
                    }

                    if (!_queueAction1Result.TryDequeue(out var arg))
                    {
                        Thread.Sleep(1);

                        if (cancellation?.Token.IsCancellationRequested == true)
                            return;

                        continue;
                    }

                    Stage2ActionStarted?.Invoke(this, System.EventArgs.Empty);
                    _queueAction2Result.Enqueue(invoker.Invoke(arg));
                    Stage2ActionFinished?.Invoke(this, System.EventArgs.Empty);

                    Thread.Sleep(1);

                    if (cancellation?.Token.IsCancellationRequested == true)
                        return;
                }
            });
        }

        /// <summary>
        /// 添加 Stage1 功能函数。
        /// </summary>
        /// <param name="func"></param>
        public void AppendFunc1(Func<TResult1> func)
        {
            _queueAction1.Enqueue(new PipelineMethodInvoker<TResult1>(func));
        }

        /// <summary>
        /// 添加 Stage2 功能函数。
        /// </summary>
        public void AppendFunc2(Func<TResult1, TResult2> func)
        {
            _queueAction2.Enqueue(new PipelineMethodInvoker<TResult1, TResult2>(func));
        }

        /// <summary>
        /// 启动流水线。
        /// </summary>
        public List<Task> Start(CancellationTokenSource cancellation)
        {
            var t1 = ExecuteFunc1(cancellation);
            var t2 = ExecuteFunc2(cancellation);
            
            return new List<Task>() { t1, t2 };
        }

        public void Dispose()
        {
            _isDisposed = true;

            //try
            //{
            //    ThreadPool.SetMinThreads(_workerThreadsMin, _mIocMin);
            //}
            //catch (Exception e)
            //{
            //    // Ignore
            //}
        }

        #endregion


    }
}
