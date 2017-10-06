namespace SimpleScripting
{
    using System;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// The extensions.
    /// </summary>
    internal static class Extensions
    {
        /// <summary>
        /// The handle enum.
        /// </summary>
        /// <param name="value">
        /// The value.
        /// </param>
        /// <param name="elementType">
        /// The element type.
        /// </param>
        /// <param name="singleChar">
        /// The single Char.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public static object ConvertToEnum(this object value, Type elementType, bool singleChar = false)
        {
            if (!elementType.IsEnum)
            {
                return null;
            }

            // check if this is a number that has been passed.
            if (int.TryParse(value.ToString(), out var t))
            {
                return Enum.ToObject(elementType, t);
            }

            if (elementType.GetCustomAttributes<FlagsAttribute>().Any())
            {
                try
                {
                    var members = value.ToString().Split(new[] { ',', '-', ' ', '|', '_' }, StringSplitOptions.RemoveEmptyEntries);

                    if (members.Length > 1)
                    {
                        return members.Aggregate(0, (current, member) => current | (int)member.ConvertToEnum(elementType, member.Length < 2));
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }

            var name = Enum.GetNames(elementType)
                .FirstOrDefault(
                    x => singleChar
                             ? x.StartsWith(value.ToString(), StringComparison.OrdinalIgnoreCase)
                             : x.Equals(value.ToString(), StringComparison.OrdinalIgnoreCase));

            return string.IsNullOrEmpty(name) ? null : Enum.Parse(elementType, name, true);
        }

        /// <summary>
        /// The clear console line.
        /// </summary>
        public static void ClearConsoleLine()
        {
            // reset out cursor to the left.
            Console.CursorLeft = 0;

            // clear the line
            Console.Write(new string(' ', Console.BufferWidth));

            Console.CursorTop--;

            // reset out cursor to the left.
            Console.CursorLeft = 0;
        }
    }
}
