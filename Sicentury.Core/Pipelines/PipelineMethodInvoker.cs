/************************************************************************
*@file FrameworkLocal\UIClient\CenterViews\Core\Pipelines\PipelineMethodInvoker.cs
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

namespace Sicentury.Core.Pipelines
{
    public class PipelineMethodInvoker<TResult>
    {
        public PipelineMethodInvoker(Func<TResult> func)
        {
            Function = func;
        }

        public bool IsEmpty => Function == null;

        public Func<TResult> Function { get; }

        public TResult Invoke()
        {
            return Function.Invoke();
        }

    }

    public class PipelineMethodInvoker<TParam, TResult>
    {
        public PipelineMethodInvoker(Func<TParam, TResult> func)
        {
            Function = func;
        }

        public bool IsEmpty => Function == null;

        public Func<TParam, TResult> Function { get; }


        public TResult Invoke(TParam arg)
        {
            return Function.Invoke(arg);
        }

    }

   
}
