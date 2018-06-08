namespace SimpleScripting
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The scripting.
    /// </summary>
    public abstract class ScriptingBase
    {
        /// <summary>
        /// The method start.
        /// </summary>
        public const char MethodStart = '(';

        /// <summary>
        /// The method end.
        /// </summary>
        public const char MethodEnd = ')';

        /// <summary>
        /// The define start.
        /// </summary>
        public const char DefineStart = '#';

        /// <summary>
        /// The define end.
        /// </summary>
        public const char DefineEnd = ';';

        /// <summary>
        /// The defines.
        /// </summary>
        private static readonly Dictionary<string, string> Defines = new Dictionary<string, string>();

        /// <summary>
        /// The tokens.
        /// </summary>
        private static Dictionary<string, MethodInfo> tokens;

        /// <summary>
        /// The pause.
        /// </summary>
        private static bool pause;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptingBase"/> class.
        /// </summary>
        protected ScriptingBase()
        {
            IsConsole = GetConsoleWindow() != IntPtr.Zero;

            BuildTokens(this.GetType());

            this.Steps = new List<Step>();
        }

        /// <summary>
        /// Gets or sets a value indicating whether disable display.
        /// </summary>
        public static bool DisableDisplay
        {
            get;
            set;
        }

        /// <summary>
        /// Gets or sets a value indicating whether pause.
        /// </summary>
        public static bool Pause
        {
            get => pause;

            set
            {
                pause = value;

                if (pause)
                {
                    Paused();
                }
            }
        }

        /// <summary>
        /// Gets a value indicating whether a console is available
        /// </summary>
        public static bool IsConsole { get; private set; }

        /// <summary>
        /// Gets The supported string.
        /// </summary>
        public string Supported
        {
            get
            {
                var response = new StringBuilder("Tag" + MethodStart + "ArgumentName:ArgumentType" + MethodEnd + Environment.NewLine);
                response.AppendLine("      :: Description");
                response.AppendLine("    If argument is shown inside of [ ] this is optional");
                response.AppendLine("    TAG" + MethodStart + "[ArgumentName=DefaultValue]:ArgumentType" + MethodEnd);
                response.AppendLine();
                response.AppendLine("======Supported Commands======");
                response.AppendLine();

                foreach (var method in tokens.Values.Distinct().OrderBy(x => x.GetCustomAttributes<ScriptingAttribute>().First().Tag))
                {
                    var name = method.GetCustomAttributes<ScriptingAttribute>().First();

                    response.Append('\t' + name.Tag + MethodStart);

                    var param = method.GetParameters();

                    foreach (var arg in param)
                    {
                        var typeName = arg.ParameterType.Name;

                        if (arg.ParameterType.IsGenericType)
                        {
                            typeName = arg.ParameterType.GetGenericArguments()[0].Name;
                        }

                        if (!arg.HasDefaultValue)
                        {
                            response.Append(arg.Name + ":" + arg.ParameterType.Name + ",");
                        }
                        else
                        {
                            response.Append("[" + arg.Name + "=" + (arg.DefaultValue ?? "Null") + "]:" + typeName + ",");
                        }
                    }

                    if (param.Any())
                    {
                        // trim the last Character of [ the extra ,] 
                        response.Remove(response.Length - 1, 1);
                    }

                    response.AppendLine(MethodEnd.ToString());

                    if (string.IsNullOrEmpty(name.Description))
                    {
                        response.AppendLine("\t  :: ");
                    }
                    else
                    {
                        response.AppendLine("\t  :: " + name.Description);
                    }

                    response.AppendLine();
                }

                return response.ToString();
            }
        }

        /// <summary>
        /// Gets the steps.
        /// </summary>
        public List<Step> Steps
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether the script steps are valid.
        /// </summary>
        public bool IsValid
        {
            get
            {
                return this.Steps.Any() && this.Steps.All(x => x != null);
            }
        }

        /// <summary>
        /// The Parse steps.
        /// </summary>
        /// <param name="steps">
        /// The step text to convert.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        public bool SetSteps(string steps)
        {
            try
            {
                // change our internal reference to this list of steps. 
                this.Steps = this.ReadSteps(steps);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// The parse steps.
        /// </summary>
        /// <param name="rawStep">
        /// The steps.
        /// </param>
        /// <returns>
        /// The List of Steps. 
        /// </returns>
        public List<Step> ReadSteps(string rawStep)
        {
            var startOfStep = -1;
            var endOfStep = -1;

            var steps = new List<Step>();

            // itrate through the steps string.
            for (var index = 0; index < rawStep.Length; index++)
            {
                switch (rawStep[index])
                {
                    case DefineStart:
                        if (startOfStep > -1)
                        {
                            break;
                        }

                        HandleTag(ref index, rawStep);
                        break;

                    case MethodStart:
                        var offset = index;

                        // our initial string. 
                        while (--offset > -1 && char.IsLetterOrDigit(rawStep[offset]))
                        {
                        }

                        startOfStep = offset + 1;
                        break;

                    case MethodEnd:
                        endOfStep = index + 1;
                        break;

                    default:
                        continue;
                }

                if (startOfStep < 0 || endOfStep < 0)
                {
                    continue;
                }

                // calculate our step length
                var length = endOfStep - startOfStep;

                // add the Step as an option. 
                steps.Add(this.HandleStep(rawStep.Substring(startOfStep, length)));

                // reset our counters. 
                startOfStep = endOfStep = -1;
            }

            return steps;
        }

        /// <summary>
        /// The wait.
        /// </summary>
        /// <param name="duration">
        /// The duration.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        [Scripting(Tag = "WA|WAIT|W", Description = "Run a wait with Timer output if more then 1 second")]
        public void Wait(TimeSpan duration, string message = null)
        {
            if (duration < TimeSpan.FromSeconds(1.25))
            {
                Thread.Sleep(duration);
                return;
            }

            var wait = DateTime.Now.Add(duration);

            var left = IsConsole ? Console.CursorLeft : 0;
            var top = IsConsole ? Console.CursorTop : 0;

            while (DateTime.Now <= wait)
            {
                Thread.Sleep(100);

                if (!IsConsole || DisableDisplay || Pause)
                {
                    continue;
                }

                Console.WriteLine("\t\t--{0} {1:hh}:{1:mm}:{1:ss}.{1:ff}--", message, wait - DateTime.Now);
                Console.CursorLeft = left;
                Console.CursorTop = top;
            }

            if (!IsConsole || DisableDisplay)
            {
                return;
            }

            Console.CursorLeft = left;
            Console.CursorTop = top;
            Console.WriteLine(new string(' ', Console.BufferWidth));
            Console.CursorLeft = left;
            Console.CursorTop = top;
        }

        /// <summary>
        /// The wait while a function is false. 
        /// </summary>
        /// <param name="isTrue">
        /// The is true.
        /// </param>
        /// <param name="duration">
        /// The duration.
        /// </param>
        /// <param name="messageDelay">
        /// The message delay.
        /// </param>
        /// <param name="message">
        /// The message.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        protected static bool WaitFunc(Func<bool> isTrue, TimeSpan duration, TimeSpan messageDelay, string message = null)
        {
            var iterationWait = DateTime.Now.Add(duration);

            var left = !IsConsole ? 0 : Console.CursorLeft;
            var top = !IsConsole ? 0 : Console.CursorTop;

            while (DateTime.Now <= iterationWait && isTrue())
            {
                Thread.Sleep(messageDelay);

                if (!IsConsole || DisableDisplay || pause)
                {
                    continue;
                }

                Console.WriteLine("\t\t--{0} {1:hh}:{1:mm}:{1:ss}.{1:ff}--", message, iterationWait - DateTime.Now);
                Console.CursorLeft = left;
                Console.CursorTop = top;
            }

            if (!IsConsole || DisableDisplay || pause)
            {
                return !isTrue();
            }

            Console.CursorLeft = left;
            Console.CursorTop = top;
            Console.WriteLine(new string(' ', Console.BufferWidth));
            Console.CursorLeft = left;
            Console.CursorTop = top;
            return !isTrue();
        }

        /// <summary>
        /// The handle tag.
        /// </summary>
        /// <param name="index">
        /// The index.
        /// </param>
        /// <param name="rawStep">
        /// The raw step.
        /// </param>
        protected static void HandleTag(ref int index, string rawStep)
        {
            var end = 1;
            var start = index++;

            while (rawStep[index] != DefineEnd && rawStep[index] != '\r')
            {
                index++;
                end++;
            }

            var define = rawStep.Substring(start, end).Split('=');
            Defines.Add(define[0].Trim(), define[1].TrimEnd(DefineStart).Trim());
        }

        /// <summary>
        /// The handle step.
        /// </summary>
        /// <param name="step">
        /// The step.
        /// </param>
        /// <returns>
        /// The <see cref="Step"/>.
        /// </returns>
        protected Step HandleStep(string step)
        {
            var offset = step.IndexOf(MethodStart);
            var tag = step.Substring(0, offset).ToUpper();

            if (!tokens.ContainsKey(tag))
            {
                return null;
            }

            var arguments = step.Substring(offset + 1).TrimEnd(MethodEnd).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            var taggedArguments = arguments.Count(x => x.Trim().StartsWith(DefineStart.ToString()));

            // check for tagged arguments. 
            if (taggedArguments > 0)
            {
                var found = new bool[taggedArguments];

                for (var index = 0; index < arguments.Length; index++)
                {
                    if (!Defines.ContainsKey(arguments[index].Trim()))
                    {
                        continue;
                    }

                    found[index] = true;
                    arguments[index] = Defines[arguments[index].Trim()];
                }

                if (!found.All(x => x))
                {
                    return null;
                }
            }

            var method = tokens[tag];
            var expectedParameters = method.GetParameters();
            var parameters = new List<object>(arguments.Select((x, index) => this.ProcessStep(x, expectedParameters[index].ParameterType)));

            // we have the same count so we can return the parameters. 
            if (parameters.Count == expectedParameters.Length)
            {
                return new Step(this, method, parameters, tag, step);
            }

            for (var index = 0; index < expectedParameters.Length; index++)
            {
                if (index < parameters.Count)
                {
                    if (parameters[index] == null || expectedParameters[index].ParameterType == parameters[index].GetType())
                    {
                        continue;
                    }

                    if (expectedParameters[index].ParameterType.IsGenericType)
                    {
                        if (expectedParameters[index].ParameterType.GetGenericArguments().Any(x => x == parameters[index].GetType()))
                        {
                            continue;
                        }
                    }
                }

                if (expectedParameters[index].HasDefaultValue)
                {
                    parameters.Add(expectedParameters[index].DefaultValue);
                }
                else
                {
                    // failed to parse send a null step. 
                    return null;
                }
            }

            return new Step(this, method, parameters, tag, step);
        }

        /// <summary>
        /// The process step.
        /// </summary>
        /// <param name="argument">
        /// The arguments to process.
        /// </param>
        /// <param name="type">
        /// The parameter type.
        /// </param>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        protected virtual object ProcessStep(string argument, Type type)
        {
            if (type.IsGenericType)
            {
                // process the generic type.
                return this.ProcessStep(argument, type.GetGenericArguments().FirstOrDefault());
            }

            // check if we have an enum.
            if (type.IsEnum)
            {
                // check if we are a Tagged member or not.
                // if we are tagged then handle as a tagged enum
                // else use the default Enum conversion.
                return
                    type.GetMembers().Any(x => x.GetCustomAttributes<ScriptingAttribute>().Any())
                        ? ConvertToTaggedEnum(argument, type)
                        : argument.ConvertToEnum(type, argument.Length < 2);
            }

            // not an enum handle if this is a timespan or regular conversion.
            if (type == typeof(TimeSpan))
            {
                return ScriptingTimeFormats.ConvertToTime(argument);
            }

            if (!argument.StartsWith("0x"))
            {
                return Convert.ChangeType(argument, type);
            }

            var number = long.Parse(argument.Replace("0x", string.Empty), NumberStyles.AllowHexSpecifier);
            return Convert.ChangeType(number, type);
        }

        /// <summary>
        /// The build tokens.
        /// </summary>
        /// <param name="type">
        /// The type.
        /// </param>
        private static void BuildTokens(Type type)
        {
            tokens = new Dictionary<string, MethodInfo>();

            // read all the methods with our scripting Tag. 
            var allMethods = type.GetMethods().Where(x => x.GetCustomAttributes<ScriptingAttribute>().Any()).ToList();

            // handle all base methods.
            HandleTokens(allMethods.Where(x => x.DeclaringType == typeof(ScriptingBase)));

            // handle all sub base methods that arent Scripting base.
            HandleTokens(allMethods.Where(x => x.DeclaringType != type && x.DeclaringType != typeof(ScriptingBase)));

            // handle all type methods. 
            HandleTokens(allMethods.Where(x => x.DeclaringType == type));
        }

        /// <summary>
        /// The handle tokens.
        /// </summary>
        /// <param name="source">
        /// The source.
        /// </param>
        private static void HandleTokens(IEnumerable<MethodInfo> source)
        {
            foreach (var method in source)
            {
                var attribute = method.GetCustomAttributes<ScriptingAttribute>().First();

                foreach (var tag in attribute.Tag.ToUpper().Split('|').Select(x => x.Trim()))
                {
                    if (tokens.ContainsKey(tag))
                    {
                        tokens[tag] = method;
                    }
                    else
                    {
                        tokens.Add(tag, method);
                    }
                }
            }
        }

        /// <summary>
        /// The handle key type.
        /// </summary>
        /// <param name="key">
        /// The key arguments.
        /// </param>
        /// <param name="type">
        /// The parameter Type.
        /// </param>
        /// <returns>
        /// The Enumeration value
        /// </returns>
        private static object ConvertToTaggedEnum(string key, Type type)
        {
            if (!type.IsEnum)
            {
                return null;
            }

            foreach (var element in Enum.GetNames(type))
            {
                // find each member of the enum by name and check the tag.
                if (type.GetMember(element)[0].GetCustomAttributes<ScriptingAttribute>()
                    .First()
                    .Tag.StartsWith(key, true, CultureInfo.InvariantCulture))
                {
                    return element.ConvertToEnum(type);
                }
            }

            return null;
        }

        /// <summary>
        /// The paused.
        /// </summary>
        private static async void Paused()
        {
            if (IsConsole)
            {
                Extensions.ClearConsoleLine();
            }

            var left = !IsConsole ? 0 : Console.CursorLeft;
            var top = !IsConsole ? 0 : Console.CursorTop;

            if (IsConsole)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
            }

            var pauseStart = DateTime.Now;

            await Task.Run(() =>
                    {
                        while (Pause)
                        {
                            var delta = DateTime.Now - pauseStart;
                            if (IsConsole)
                            {
                                Console.WriteLine($"\t[User Requested Pause] {delta:hh}:{delta:mm}:{delta:ss}.{delta:ff}");
                                Console.CursorLeft = left;
                                Console.CursorTop = top;
                            }

                            Thread.Sleep(250);
                        }
                    });

            if (IsConsole)
            {
                Console.ResetColor();
                Extensions.ClearConsoleLine();

                // reset our view. 
                Console.CursorLeft = left;
                Console.CursorTop = top;
            }

            Thread.Sleep(50);
        }

        [DllImport("kernel32.dll")]
        private static extern IntPtr GetConsoleWindow();
    }
}
