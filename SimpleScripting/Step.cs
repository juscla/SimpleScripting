namespace SimpleScripting
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;

    /// <summary>
    /// The step.
    /// </summary>
    public class Step
    {
        /// <summary>
        /// The instance.
        /// </summary>
        private readonly ScriptingBase instance;

        /// <summary>
        /// The run count.
        /// </summary>
        private int runCount;

        /// <summary>
        /// Initializes a new instance of the <see cref="Step"/> class.
        /// </summary>
        /// <param name="instance">
        /// The instance.
        /// </param>
        /// <param name="method">
        /// The method.
        /// </param>
        /// <param name="parameters">
        /// The parameters.
        /// </param>
        /// <param name="tag">
        /// The tag of the method.
        /// </param>
        public Step(ScriptingBase instance, MethodInfo method, List<object> parameters, string tag)
        {
            this.instance = instance;
            this.Method = method;
            this.Parameters = parameters.ToArray();
            this.Tag = tag;
        }

        /// <summary>
        /// Gets the method.
        /// </summary>
        public MethodInfo Method
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the parameters.
        /// </summary>
        public object[] Parameters
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets a value indicating whether has return.
        /// </summary>
        public bool HasReturn
        {
            get
            {
                return this.ReturnType != typeof(void);
            }
        }

        /// <summary>
        /// Gets a value indicating whether is exit current iteration.
        /// </summary>
        public bool IsExitIteration
        {
            get
            {
                return this.HasReturn &&
                    this.Method.GetCustomAttributes<ScriptingAttribute>().Any(x => x.ExitIterationOnFail);
            }
        }

        /// <summary>
        /// Gets a value indicating whether is exit on fail.
        /// </summary>
        public bool IsExitOnFail
        {
            get
            {
                return this.HasReturn &&
                    this.Method.GetCustomAttributes<ScriptingAttribute>().Any(x => x.ExitTestOnFail);
            }
        }

        /// <summary>
        /// Gets the Actual result value.
        /// </summary>
        public object ActualValue
        {
            get;
            private set;
        }

        /// <summary>
        /// Gets the expected value.
        /// </summary>
        public object ExpectedValue
        {
            get
            {
                return
                    this.IsExitOnFail || this.IsExitIteration ?
                    this.Method.GetCustomAttributes<ScriptingAttribute>().First(x => x.Expected != null).Expected :
                    null;
            }
        }

        /// <summary>
        /// Gets the return type.
        /// </summary>
        public Type ReturnType
        {
            get
            {
                return this.Method.ReturnType;
            }
        }

        /// <summary>
        /// Gets a value indicating whether is run once step.
        /// </summary>
        public bool IsRunOnce
        {
            get
            {
                return this.Method.GetCustomAttributes<ScriptingAttribute>().Any(x => x.IsSingleRun);
            }
        }

        /// <summary>
        /// Gets the tag.
        /// </summary>
        public string Tag
        {
            get;
            private set;
        }

        /// <summary>
        /// The run.
        /// </summary>
        /// <returns>
        /// The <see cref="object"/>.
        /// </returns>
        public object Run()
        {
            if (this.IsRunOnce && this.runCount > 0)
            {
                return this.ActualValue;
            }

            this.runCount++;
            this.ActualValue = this.Method.Invoke(this.instance, this.Parameters);
            return this.ActualValue;
        }

        /// <summary>
        /// The String representation of the Step.
        /// </summary>
        /// <returns>
        /// The String
        /// </returns>
        public override string ToString()
        {
            var response = this.Method.Name + ScriptingBase.MethodStart;
            var parameterNames = this.Method.GetParameters();

            for (int index = 0; index < this.Parameters.Length; index++)
            {
                var arg = this.Parameters[index];
                response += parameterNames[index].Name + ": " + (arg != null ? arg.ToString() : "null") + ",";
            }

            return response.TrimEnd(',') + ScriptingBase.MethodEnd + " ";
        }
    }
}
