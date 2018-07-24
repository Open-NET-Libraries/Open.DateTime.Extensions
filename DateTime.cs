using Open;
using Open.Numeric.Precision;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;

namespace System
{
	public static class DateTimeExtensions
	{
		public static DateTime FirstOfTheMonth(this DateTime date)
		{
			return new DateTime(date.Year, date.Month, 1);
		}

		public static DateTime ToDateTime(this TimeSpan time)
		{
			var ticks = time.Ticks;
			if (ticks < 0 || ticks > 3155378975999999999)
				throw new ArgumentOutOfRangeException(nameof(time));
			Contract.EndContractBlock();

			return new DateTime(ticks);
		}

		public static double ToOADate(this TimeSpan time)
		{
			return (double)time.Ticks / TimeSpan.TicksPerDay;
		}

		public static double ToMilliseconds(this DateTime time)
		{
			return TimeSpan.FromTicks(time.Ticks).TotalMilliseconds;
		}

		public static string ToAlphaNumeric(this DateTime date)
		{
			return date.ToString("yyyyMMddZHHmmssT");
		}

		public static TimeSpan ExcuteAndMeasureDuration(this Action closure)
		{
			if (closure == null)
				throw new NullReferenceException();
			Contract.EndContractBlock();

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			closure();
			stopwatch.Stop();
			return stopwatch.Elapsed;
		}

		public static string ElapsedTimeString(this Stopwatch target)
		{
			if (target == null)
				throw new NullReferenceException();
			return target.Elapsed.ToStringVerbose();
		}

		public static TimeSpan RemainingTime(this Stopwatch target, int completed, int total)
		{
			if (target == null)
				throw new NullReferenceException();
			Contract.EndContractBlock();

			if (completed == 0 || total == 0)
				return TimeSpan.MaxValue;

			var remaining = total - completed;
			//Contract.Assume(remaining != int.MinValue || completed !=-1);

			double m = remaining * target.ElapsedMilliseconds;
			return TimeSpan.FromMilliseconds(m / completed);
		}


		public static string RemainingTimeString(this Stopwatch target, int completed, int total)
		{
			if (target == null)
				throw new NullReferenceException();
			Contract.EndContractBlock();

			return target.RemainingTime(completed, total).ToStringVerbose();
		}

		public static string ToStringVerbose(this TimeSpan time, TimeSpan? minimumIncrement = null)
		{
			if (time == TimeSpan.MaxValue)
				return "?";

			if (minimumIncrement == null)
				minimumIncrement = TimeSpan.FromSeconds(1);

			var result = string.Empty;

			if (time.Ticks > 0)
			{
				var isMinimum = time <= minimumIncrement;
				var days = Math.Round(10 * time.TotalDays) / 10;
				if (days > 2 || isMinimum && minimumIncrement >= TimeSpan.FromDays(1))
					result = days + " days";
				else
				{

					var hours = Math.Round(10 * time.TotalHours) / 10;
					if (hours > 1 || isMinimum && minimumIncrement >= TimeSpan.FromHours(1))
						result = hours + " hours";
					else
					{
						var minutes = time.TotalMinutes;
						if (minutes > 1.5 || isMinimum && minimumIncrement >= TimeSpan.FromMinutes(1))
							result = (minutes > 10 ? Math.Ceiling(minutes) : (Math.Ceiling(minutes * 10) / 10)) + " minutes";
						else
						{
							var seconds = time.TotalSeconds;
							if (seconds.IsPreciseEqual(1) || isMinimum && minimumIncrement >= TimeSpan.FromSeconds(1))
								result = "1 second";
							else if (seconds > 1)
								result = Math.Ceiling(seconds) + " seconds";
							else
							{
								var ms = time.TotalMilliseconds;
								// ReSharper disable once CompareOfFloatsByEqualityOperator
								if (ms == 1 || ms.IsPreciseEqual(1))
									result = "1 millisecond";
								else
									result = (ms > 1 ? Math.Ceiling(ms).ToString("n0") : ms.ToString("n3")) + " milliseconds";
							}
						}
					}
				}
			}
			return result;
		}

		public static Range<DateTime> ParseRange(string source)
		{
			return ParseRange(source, DateTime.MinValue, DateTime.MaxValue);
		}

		public static readonly Regex EXACTMONTH = new Regex(@"^(?<year>\d\d\d\d)/(?<month>\d\d?)$");

		public static DateTime Parse(string date, string time, DateTime defaultValue)
		{
			if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out var result)) return defaultValue;
			if (string.IsNullOrWhiteSpace(time)) return result;

			TimeSpan ts;
			if (NumericTime.TimeDigitsPattern.IsMatch(time))
				ts = NumericTime.FromUnknownType(time);
			else
				TimeSpan.TryParse(time, out ts);

			if (ts == TimeSpan.Zero)
				return result;

			// Within range?
			var ticks = result.Ticks + ts.Ticks;

			if (ticks < DateTime.MinValue.Ticks)
				return DateTime.MinValue;

			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (ticks > DateTime.MaxValue.Ticks)
				return DateTime.MaxValue;

			return result.Add(ts);
		}

		public static Range<DateTime> ParseRange(string source, DateTime defaultStart, DateTime defaultEnd)
		{
			var startDate = defaultStart;
			var endDate = defaultEnd;

			if (!string.IsNullOrWhiteSpace(source))
			{
				var exactMonth = EXACTMONTH.Match(source);
				if (exactMonth.Success)
				{
					startDate = DateTime.Parse(source + "/1");
					endDate = startDate.AddMonths(1);
				}
				else
				{
					var dd = source.Split('-');
					var left = dd[0].Trim();
					var right = dd.Length != 1 ? (dd[1] ?? string.Empty).Trim() : string.Empty;
					if (!string.IsNullOrWhiteSpace(left))
						startDate = DateTime.Parse(left);
					if (!string.IsNullOrWhiteSpace(right))
						endDate = DateTime.Parse(right);
				}
			}

			return new Range<DateTime>(startDate, endDate);
		}



		public static TimeSpan Delta(this DateTime fromtime, DateTime? totime = null)
		{
			var to = totime ?? DateTime.Now;
			return TimeSpan.FromTicks(to.Ticks - fromtime.Ticks);
		}


		public static TimeSpan DivideBy(this TimeSpan target, long divisor)
		{
			if (divisor == 0)
				throw new ArgumentException("Cannot be zero.", "divisor");
			Contract.EndContractBlock();

			return TimeSpan.FromTicks(target.Ticks / divisor);
		}

		public static TimeSpan DivideBy(this TimeSpan target, int divisor)
		{
			if (divisor == 0)
				throw new ArgumentException("Cannot be zero.", "divisor");


			return TimeSpan.FromTicks(target.Ticks / divisor);
		}

		public static TimeSpan MultiplyBy(this TimeSpan target, long divisor)
		{
			return TimeSpan.FromTicks(target.Ticks * divisor);
		}

		public static TimeSpan MultiplyBy(this TimeSpan target, int divisor)
		{
			return TimeSpan.FromTicks(target.Ticks * divisor);
		}

		public static bool IsInRange(this TimeSpan target, Range<TimeSpan> range)
		{
			return target >= range.Low && target < range.High;
		}

	}



	public static class NumericTime
	{
		#region Types enum
		public enum Types
		{
			Hours,
			HoursMinutes,
			HoursMinutesSeconds
		}
		#endregion


		public static readonly Regex TimeDigitsPattern = new Regex(@"(\d?\d)(\d\d)(\d\d)?");

		public static TimeSpan From(object numerictime, Types expectedType, bool assertType)
		{
			if (numerictime is string dddd)
				return From(dddd, expectedType);

			switch (expectedType)
			{
				case Types.HoursMinutesSeconds:
					if (!assertType || numerictime is int)
						return From(Convert.ToInt32(numerictime), expectedType);
					throw new InvalidOperationException(
						"Attempting to convert to unexpected numeric time. (Expected: HoursMinutesSeconds)");
				case Types.HoursMinutes:
					if (!assertType || numerictime is ushort)
						return From(Convert.ToUInt16(numerictime));
					throw new InvalidOperationException("Attempting to convert to unexpected numeric time. (Expected: HoursMinutes)");
				case Types.Hours:
					if (!assertType || numerictime is byte)
						return From(Convert.ToByte(numerictime));
					throw new InvalidOperationException("Attempting to convert to unexpected numeric time. (Expected: Hours)");
			}

			throw new InvalidCastException("'time' is an invalid type to convert to numeric time.");
		}

		public static DateTime From(object numerictime, Types expectedType, bool assertType, object date)
		{
			var d = date as DateTime?;
			if (!d.HasValue && date != null)
				throw new InvalidCastException();

			return From(From(numerictime, expectedType, assertType), d);
		}

		public static DateTime From(TimeSpan time, DateTime date)
		{
			return new DateTime(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds);
		}

		public static DateTime From(TimeSpan time, DateTime? date = null)
		{
			if (date == null)
				date = default(DateTime);

			return From(time, date.Value);
		}

		public static TimeSpan From(string numerictime, Types type)
		{
			if (numerictime == null)
				throw new ArgumentNullException(nameof(numerictime));
			Contract.EndContractBlock();

			return From(int.Parse(numerictime), type);
		}

		public static TimeSpan FromUnknownType(string numerictime)
		{
			if (numerictime == null)
				throw new ArgumentNullException(nameof(numerictime));
			if (numerictime.Length > 4)
				return From(int.Parse(numerictime), Types.HoursMinutesSeconds);
			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (numerictime.Length > 2)
				return From(ushort.Parse(numerictime));

			return From(byte.Parse(numerictime));
		}


		public static DateTime From(string dddd, Types expectedType, DateTime date)
		{
			if (dddd != null)
				throw new ArgumentNullException(nameof(dddd));
			Contract.EndContractBlock();

			return From(From(dddd, expectedType), date);
		}

		public static TimeSpan From(int hours, int minutes, int seconds)
		{
			return
				TimeSpan.FromTicks(TimeSpan.TicksPerHour * hours + TimeSpan.TicksPerMinute * minutes +
					TimeSpan.TicksPerSecond * seconds);
		}

		public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static DateTime FromUnix(TimeSpan unixtimespan)
		{
			return Epoch.Add(unixtimespan);
		}

		public static TimeSpan ToUnix(this DateTime date)
		{
			return TimeSpan.FromTicks(date.Ticks - Epoch.Ticks);
		}

		public static long ToUnixMilliseconds(this DateTime date)
		{
			return Convert.ToInt64(TimeSpan.FromTicks(date.Ticks - Epoch.Ticks).TotalMilliseconds);
		}

		public static DateTime FromUnixMilliseconds(long unixmilliseconds)
		{
			return Epoch.AddMilliseconds(unixmilliseconds);
		}

		private static int Reduce(ref int reduction)
		{
			return Reduce(ref reduction, 100);
		}

		private static int Reduce(ref int reduction, int factor)
		{
			if (factor <= 0)
				throw new ArgumentException(nameof(factor));
			Contract.EndContractBlock();

			var remainder = reduction % factor;
			reduction = (reduction - remainder) / factor;
			return remainder;
		}


		/// <summary>
		/// Returns a numeric time based on the integer source and the number of groupings (1=hours, 2
		/// </summary>
		/// <param name="dddddd"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		public static TimeSpan From(int dddddd, Types type)
		{
			if (dddddd < 0 || dddddd >= 240000)
				throw new ArgumentOutOfRangeException(nameof(dddddd));
			Contract.EndContractBlock();

			var d = dddddd;
			var seconds = (type == Types.HoursMinutesSeconds) ? Reduce(ref d) : 0;
			var minutes = (type == Types.HoursMinutesSeconds || type == Types.HoursMinutes) ? Reduce(ref d) : 0;
			var hours = Reduce(ref d);

			return From(hours, minutes, seconds);
		}

		public static TimeSpan FromHoursMinutes(int? dddd)
		{
			if (dddd == null)
				throw new NullReferenceException();

			return FromHoursMinutes(dddd.Value);
		}


		public static TimeSpan FromHoursMinutes(int dddd)
		{
			return From(dddd, Types.HoursMinutes);
		}

		public static DateTime FromHoursMinutes(int dddd, DateTime? date)
		{
			return From(FromHoursMinutes(dddd), date);
		}

		public static TimeSpan From(ushort dddd)
		{
			if (dddd >= 2400)
				throw new ArgumentOutOfRangeException(nameof(dddd));
			Contract.EndContractBlock();

			var d = (int)dddd;
			var minutes = Reduce(ref d);
			var hours = Reduce(ref d);

			return From(hours, minutes, 0);
		}

		public static TimeSpan From(byte dd)
		{
			if (dd >= 24)
				throw new ArgumentOutOfRangeException(nameof(dd));
			Contract.EndContractBlock();

			return From(dd, 0, 0);
		}

		public static DateTime From(ushort dddd, DateTime date)
		{
			return From(From(dddd), date);
		}

		public static ushort ToNumericTime(this TimeSpan time)
		{
			return (ushort)(time.Hours * 100 + time.Minutes);
		}

		public static ushort ToNumericTime(this DateTime time)
		{
			return ToNumericTime(time.TimeOfDay);
		}

		public static ushort ToNumericTime(this DateTimeOffset time)
		{
			return ToNumericTime(time.TimeOfDay);
		}

		public static int ToNumericTimeWithSeconds(this TimeSpan time)
		{
			return time.Hours * 10000 + time.Minutes * 100 + time.Seconds;
		}

		public static int ToNumericTimeWithSeconds(this DateTime time)
		{
			return ToNumericTimeWithSeconds(time.TimeOfDay);
		}

		public static int ToNumericTimeWithSeconds(this DateTimeOffset time)
		{
			return ToNumericTimeWithSeconds(time.TimeOfDay);
		}
	}
}
