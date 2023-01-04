using System;
using System.Collections.Generic;

namespace Sicentury.Core
{
    /// <summary>
    /// 该类实现对时间范围进行操作，包括按指定间隔分隔时间范围。
    /// </summary>
    public class DateRangeHelper
    {
        #region Constructors

        public DateRangeHelper(DateTime start, DateTime end)
        {
            Start = start;
            End = end;
        }

        #endregion

        #region Properties

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        public TimeSpan Diff => End - Start;

        #endregion

        #region Methods

        /// <summary>
        /// 将时间范围按天为间隔分割为数组。
        /// </summary>
        /// <param name="range"></param>
        /// <returns></returns>
        public static IEnumerable<DateRangeHelper> SplitInToDays(DateRangeHelper range)
        {
            var ranges = new List<DateRangeHelper>();

            var currRange = new DateRangeHelper(range.Start, range.Start.Date.AddDays(1).AddTicks(-1));

            while (true)
            {
                if (currRange.End.Date <= range.End.Date)
                {
                    if (currRange.End > range.End)
                        currRange.End = range.End;

                    ranges.Add(currRange);

                    // 向后移一天
                    currRange = new DateRangeHelper(currRange.Start.Date.AddDays(1),
                        currRange.Start.Date.AddDays(2).AddTicks((-1)));
                }
                else
                {
                    break;
                }

            }

            return ranges;
        }

        /// <summary>
        /// 将时间范围按指定的小时间隔分割为数组。
        /// </summary>
        /// <param name="range"></param>
        /// <param name="hourGap"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static IEnumerable<DateRangeHelper> SplitInToHours(DateRangeHelper range, int hourGap)
        {
            var ranges = new List<DateRangeHelper>();

            var timeBegin = range.Start;
            if (timeBegin > range.End)
                return ranges;

            while (true)
            {
                var timeEnd = timeBegin.AddHours(hourGap);
                // 检查是否超过timeStart日期
                if (timeEnd.Date > timeBegin.Date)
                    timeEnd = timeBegin.Date.AddDays(1).AddTicks(-1);

                if (timeEnd > range.End)
                    timeEnd = range.End;

                ranges.Add(new DateRangeHelper(timeBegin, timeEnd));

                timeBegin = timeEnd.AddTicks(1);
                
                // 如果其实时间大于给定范围，则退出
                if (timeBegin > range.End)
                    break;
            }

            return ranges;
        }

        public override string ToString()
        {
            return $"{Start:yyyy/MM/dd HH:mm:ss.fff} - {End:yyyy/MM/dd HH:mm:ss.fff}";
        }

        #endregion
    }
}
