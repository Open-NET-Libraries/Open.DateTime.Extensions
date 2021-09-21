using System;
using Open.Text;
using Open.Numeric.Precision;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Open.DateTimeExtensions
{
	public static class Extensions
	{
		public static DateTime FirstOfTheMonth(this DateTime date)
		=> new(date.Year, date.Month, 1);

		public static DateTime ToDateTime(this TimeSpan time)
		{
			var ticks = time.Ticks;
			if (ticks < 0 || ticks > 3155378975999999999)
				throw new ArgumentOutOfRangeException(nameof(time));
			Contract.EndContractBlock();

			return new DateTime(ticks);
		}

		public static double ToOADate(this TimeSpan time)
		=> (double)time.Ticks / TimeSpan.TicksPerDay;

		public static double ToMilliseconds(this DateTime time)
		=> TimeSpan.FromTicks(time.Ticks).TotalMilliseconds;

		public static string ToAlphaNumeric(this DateTime date)
		=> date.ToString("yyyyMMddZHHmmssT", CultureInfo.InvariantCulture);

		public static TimeSpan ExcuteAndMeasureDuration(this Action closure)
		{
			if (closure is null)
				throw new ArgumentNullException(nameof(closure));

			Contract.EndContractBlock();

			var stopwatch = new Stopwatch();
			stopwatch.Start();
			closure();
			stopwatch.Stop();
			return stopwatch.Elapsed;
		}

		public static string ElapsedTimeString(this Stopwatch target)
		{
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			Contract.EndContractBlock();

			return target.Elapsed.ToStringVerbose();
		}

		public static TimeSpan RemainingTime(this Stopwatch target, int completed, int total)
		{
			if (target is null)
				throw new ArgumentNullException(nameof(target));
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
			if (target is null)
				throw new ArgumentNullException(nameof(target));
			Contract.EndContractBlock();

			return target.RemainingTime(completed, total).ToStringVerbose();
		}

		public static string ToStringVerbose(this TimeSpan time, TimeSpan? minimumIncrement = null, IFormatProvider? formatProvider = null)
		{
			if (time == TimeSpan.MaxValue)
				return "?";

			if (minimumIncrement is null)
				minimumIncrement = TimeSpan.FromSeconds(1);

			if (time.Ticks <= 0)
				return string.Empty;
			var isMinimum = time <= minimumIncrement;
			var days = Math.Round(10 * time.TotalDays) / 10;

			if (days > 2 || isMinimum && minimumIncrement >= TimeSpan.FromDays(1))
				return $"{days} days";

			var hours = Math.Round(10 * time.TotalHours) / 10;
			if (hours > 1 || isMinimum && minimumIncrement >= TimeSpan.FromHours(1))
				return $"{hours} hours";

			var minutes = time.TotalMinutes;
			if (minutes > 1.5 || isMinimum && minimumIncrement >= TimeSpan.FromMinutes(1))
				return $"{(minutes > 10 ? Math.Ceiling(minutes) : (Math.Ceiling(minutes * 10) / 10))} minutes";

			var seconds = time.TotalSeconds;
			if (seconds.IsPreciseEqual(1) || isMinimum && minimumIncrement >= TimeSpan.FromSeconds(1))
				return "1 second";

			if (seconds > 1)
				return $"{Math.Ceiling(seconds)} seconds";

			var ms = time.TotalMilliseconds;
			// ReSharper disable once CompareOfFloatsByEqualityOperator
			if (ms == 1 || ms.IsPreciseEqual(1))
				return "1 millisecond";

			return (ms > 1
					? Math.Ceiling(ms).ToString("n0", formatProvider ?? CultureInfo.InvariantCulture)
					: ms.ToString("n3", formatProvider ?? CultureInfo.InvariantCulture))
					+ " milliseconds";
		}

		public static Range<DateTime> ParseRange(string source)
		=> ParseRange(source, DateTime.MinValue, DateTime.MaxValue);

		private const string CANNOT_BE_ZERO = "Cannot be zero.";

		public static readonly Regex EXACTMONTH
			= new(@"^(?<year>\d\d\d\d)/(?<month>\d\d?)$", RegexOptions.Compiled);

		public static DateTime Parse(string date, string time, DateTime defaultValue, IFormatProvider? formatProvider = null)
		{
			if (string.IsNullOrWhiteSpace(date) || !DateTime.TryParse(date, out var result)) return defaultValue;
			if (string.IsNullOrWhiteSpace(time)) return result;

			var ts = NumericTime.TimeDigitsPattern.IsMatch(time)
				? NumericTime.FromUnknownType(time)
				: TimeSpan.Parse(time, formatProvider ?? CultureInfo.InvariantCulture);

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

		public static Range<DateTime> ParseRange(string source, DateTime defaultStart, DateTime defaultEnd, IFormatProvider? formatProvider = null)
		{
			if (string.IsNullOrWhiteSpace(source))
				return new Range<DateTime>(defaultStart, defaultEnd);

			if (EXACTMONTH.IsMatch(source))
				return new Range<DateTime>(
					DateTime.Parse($"{source}/1", formatProvider ?? CultureInfo.CurrentCulture),
					defaultStart.AddMonths(1));

			var left = source.FirstSplit('-', out int nextIndex).Trim();
			var right = nextIndex == -1 ? ReadOnlySpan<char>.Empty : source.FirstSplit('-', out _, nextIndex).Trim();

			var startDate = defaultStart;
			var endDate = defaultEnd;
			if (formatProvider is null) formatProvider = CultureInfo.InvariantCulture;

#if NETSTANDARD2_1_OR_GREATER
			if (left.Length != 0) startDate = DateTime.Parse(left, formatProvider);
			if (right.Length != 0) endDate = DateTime.Parse(right, formatProvider);
#else
			if (left.Length != 0) startDate = DateTime.Parse(left.ToString(), formatProvider);
			if (right.Length != 0) endDate = DateTime.Parse(right.ToString(), formatProvider);
#endif

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
				throw new ArgumentException(CANNOT_BE_ZERO, nameof(divisor));
			Contract.EndContractBlock();

			return TimeSpan.FromTicks(target.Ticks / divisor);
		}

		public static TimeSpan DivideBy(this TimeSpan target, int divisor)
		{
			if (divisor == 0)
				throw new ArgumentException(CANNOT_BE_ZERO, nameof(divisor));

			return TimeSpan.FromTicks(target.Ticks / divisor);
		}

		public static TimeSpan MultiplyBy(this TimeSpan target, long divisor)
		=> TimeSpan.FromTicks(target.Ticks * divisor);

		public static TimeSpan MultiplyBy(this TimeSpan target, int divisor)
		=> TimeSpan.FromTicks(target.Ticks * divisor);

		public static bool IsInRange(this TimeSpan target, Range<TimeSpan> range)
		=> target >= range.Low && target < range.High;

	}

	public static class NumericTime
	{
		#region Types enum
		public enum Type
		{
			Hours,
			HoursMinutes,
			HoursMinutesSeconds
		}
		#endregion


		public static readonly Regex TimeDigitsPattern = new(@"(\d?\d)(\d\d)(\d\d)?", RegexOptions.Compiled);

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters", Justification = "Unique")]
		public static TimeSpan From(object numerictime, Type expectedType, bool assertType, IFormatProvider? formatProvider = null)
		{
			if (numerictime is string dddd)
				return From(dddd, expectedType);

			switch (expectedType)
			{
				case Type.HoursMinutesSeconds:
					if (!assertType || numerictime is int)
						return From(Convert.ToInt32(numerictime, formatProvider ?? CultureInfo.InvariantCulture), expectedType);
					throw new InvalidOperationException(
						"Attempting to convert to unexpected numeric time. (Expected: HoursMinutesSeconds)");

				case Type.HoursMinutes:
					if (!assertType || numerictime is ushort)
						return From(Convert.ToUInt16(numerictime, formatProvider ?? CultureInfo.InvariantCulture));
					throw new InvalidOperationException(
						"Attempting to convert to unexpected numeric time. (Expected: HoursMinutes)");

				case Type.Hours:
					if (!assertType || numerictime is byte)
						return From(Convert.ToByte(numerictime, formatProvider ?? CultureInfo.InvariantCulture));
					throw new InvalidOperationException(
						"Attempting to convert to unexpected numeric time. (Expected: Hours)");
			}

			throw new InvalidCastException("'time' is an invalid type to convert to numeric time.");
		}

		public static DateTime From(object numerictime, Type expectedType, bool assertType, object date)
		{
			var d = date as DateTime?;
			if (!d.HasValue && date != null)
				throw new InvalidCastException();

			return From(From(numerictime, expectedType, assertType), d);
		}

		public static DateTime From(TimeSpan time, DateTime date)
		=> new(date.Year, date.Month, date.Day, time.Hours, time.Minutes, time.Seconds);

		public static DateTime From(TimeSpan time, DateTime? date = null)
		=> From(time, date ?? default);

		public static TimeSpan From(string numerictime, Type type, IFormatProvider? formatProvider = null)
		{
			if (numerictime is null)
				throw new ArgumentNullException(nameof(numerictime));
			Contract.EndContractBlock();

			return From(int.Parse(numerictime, formatProvider ?? CultureInfo.InvariantCulture), type);
		}

		public static TimeSpan FromUnknownType(string numerictime, IFormatProvider? formatProvider = null)
		{
			if (numerictime is null)
				throw new ArgumentNullException(nameof(numerictime));
			if (numerictime.Length > 4)
				return From(int.Parse(numerictime, formatProvider ?? CultureInfo.CurrentCulture), Type.HoursMinutesSeconds);
			// ReSharper disable once ConvertIfStatementToReturnStatement
			if (numerictime.Length > 2)
				return From(ushort.Parse(numerictime, formatProvider ?? CultureInfo.CurrentCulture));

			return From(byte.Parse(numerictime, formatProvider ?? CultureInfo.CurrentCulture));
		}


		public static DateTime From(string dddd, Type expectedType, DateTime date)
		{
			if (dddd is null)
				throw new ArgumentNullException(nameof(dddd));
			Contract.EndContractBlock();

			return From(From(dddd, expectedType), date);
		}

		public static TimeSpan From(int hours, int minutes, int seconds)
		=> TimeSpan.FromTicks(TimeSpan.TicksPerHour * hours + TimeSpan.TicksPerMinute * minutes + TimeSpan.TicksPerSecond * seconds);

		public static readonly DateTime Epoch
			= new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

		public static DateTime FromUnix(TimeSpan unixtimespan)
		=> Epoch.Add(unixtimespan);

		public static TimeSpan ToUnix(this DateTime date)
		=> TimeSpan.FromTicks(date.Ticks - Epoch.Ticks);

		public static long ToUnixMilliseconds(this DateTime date)
		=> Convert.ToInt64(TimeSpan.FromTicks(date.Ticks - Epoch.Ticks).TotalMilliseconds);

		public static DateTime FromUnixMilliseconds(long unixmilliseconds)
		=> Epoch.AddMilliseconds(unixmilliseconds);

		private static int Reduce(ref int reduction)
		=> Reduce(ref reduction, 100);

		private static int Reduce(ref int reduction, int factor)
		{
			if (factor < 1)
				throw new ArgumentOutOfRangeException(nameof(factor), factor, "Must be at least 1.");
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
		public static TimeSpan From(int dddddd, Type type)
		{
			if (dddddd < 0 || dddddd >= 240000)
				throw new ArgumentOutOfRangeException(nameof(dddddd));
			Contract.EndContractBlock();

			var d = dddddd;
			var seconds = (type == Type.HoursMinutesSeconds) ? Reduce(ref d) : 0;
			var minutes = (type == Type.HoursMinutesSeconds || type == Type.HoursMinutes) ? Reduce(ref d) : 0;
			var hours = Reduce(ref d);

			return From(hours, minutes, seconds);
		}

		public static TimeSpan FromHoursMinutes(int? dddd)
		{
			if (dddd is null)
				throw new ArgumentNullException(nameof(dddd));

			return FromHoursMinutes(dddd.Value);
		}


		public static TimeSpan FromHoursMinutes(int dddd)
		=> From(dddd, Type.HoursMinutes);

		public static DateTime FromHoursMinutes(int dddd, DateTime? date)
		=> From(FromHoursMinutes(dddd), date);

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
		=> From(From(dddd), date);

		public static ushort ToNumericTime(this TimeSpan time)
		=> (ushort)(time.Hours * 100 + time.Minutes);

		public static ushort ToNumericTime(this DateTime time)
		=> ToNumericTime(time.TimeOfDay);

		public static ushort ToNumericTime(this DateTimeOffset time)
		=> ToNumericTime(time.TimeOfDay);

		public static int ToNumericTimeWithSeconds(this TimeSpan time)
		=> time.Hours * 10000 + time.Minutes * 100 + time.Seconds;

		public static int ToNumericTimeWithSeconds(this DateTime time)
		=> ToNumericTimeWithSeconds(time.TimeOfDay);

		public static int ToNumericTimeWithSeconds(this DateTimeOffset time)
		=> ToNumericTimeWithSeconds(time.TimeOfDay);
	}
}
