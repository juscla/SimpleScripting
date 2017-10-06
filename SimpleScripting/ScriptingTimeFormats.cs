namespace SimpleScripting
{
    using System;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Text.RegularExpressions;

    /// <summary>
    /// The scripting time formats.
    /// </summary>
    public struct ScriptingTimeFormats
    {
        /// <summary>
        /// The milliseconds.
        /// </summary>
        public const string Milliseconds = "m";

        /// <summary>
        /// The seconds.
        /// </summary>
        public const string Seconds = "S";

        /// <summary>
        /// The minutes.
        /// </summary>
        public const string Minutes = "M";

        /// <summary>
        /// The hours.
        /// </summary>
        public const string Hours = "H";

        /// <summary>
        /// The days.
        /// </summary>
        public const string Days = "D";

        /// <summary>
        /// The to string.
        /// </summary>
        /// <param name="time">
        /// The time.
        /// </param>
        /// <param name="arg">
        /// The argument.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string ToString(TimeSpan time, string arg)
        {
            if (arg.EndsWith(Milliseconds))
            {
                return time.TotalMilliseconds + arg;
            }

            if (arg.EndsWith(Minutes))
            {
                return time.TotalMinutes + arg;
            }

            if (arg.EndsWith(Seconds, true, CultureInfo.InvariantCulture))
            {
                return time.TotalSeconds + arg;
            }

            if (arg.EndsWith(Hours, true, CultureInfo.InvariantCulture))
            {
                return time.TotalHours + arg;
            }

            return arg.EndsWith(Days, true, CultureInfo.InvariantCulture)
                      ? time.TotalDays + arg
                      : "0" + Milliseconds;
        }

        /// <summary>
        /// The convert to time.
        /// </summary>
        /// <param name="arg">
        /// The argument.
        /// </param>
        /// <returns>
        /// The <see cref="TimeSpan"/>.
        /// </returns>
        public static TimeSpan ConvertToTime(string arg)
        {
            TimeSpan timer;

            if (TimeSpan.TryParse(arg, out timer))
            {
                // parsed as a true timespan. 
                return timer;
            }

            var time = Regex.Replace(arg, "[^.0-9]", string.Empty);

            if (arg.EndsWith(Milliseconds))
            {
                return TimeSpan.FromMilliseconds(double.Parse(time));
            }

            if (arg.EndsWith(Minutes))
            {
                return TimeSpan.FromMinutes(double.Parse(time));
            }

            if (arg.EndsWith(Seconds, true, CultureInfo.InvariantCulture))
            {
                return TimeSpan.FromSeconds(double.Parse(time));
            }

            if (arg.EndsWith(Hours, true, CultureInfo.InvariantCulture))
            {
                return TimeSpan.FromHours(double.Parse(time));
            }

            return arg.EndsWith(Days, true, CultureInfo.InvariantCulture)
                       ? TimeSpan.FromDays(double.Parse(time))
                       : default(TimeSpan);
        }

        /// <summary>
        /// The help.
        /// </summary>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        public static string Help()
        {
            var response = new StringBuilder("Supported Time Formats:" + Environment.NewLine);

            foreach (var x in typeof(ScriptingTimeFormats).GetFields(BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Static).Where(x => x.IsLiteral && !x.IsInitOnly))
            {
                response.AppendLine(x.Name + ":" + x.GetRawConstantValue());
            }

            return response.ToString();
        }
    }
}