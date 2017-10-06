namespace SimpleScripting
{
    using System;

    /// <summary>
    /// The scripting attribute.
    /// </summary>
    public class ScriptingAttribute : Attribute
    {
        /// <summary>
        /// Gets or sets the tag.
        /// </summary>
        public string Tag { get; set; }

        /// <summary>
        /// Gets or sets the description.
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether is single run.
        /// </summary>
        public bool IsSingleRun { get; set; }

        /// <summary>
        /// Gets or sets the expected.
        /// </summary>
        public object Expected { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the rest of the steps can proceed.
        /// </summary>
        public bool ExitIterationOnFail { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether exit test on fail.
        /// </summary>
        public bool ExitTestOnFail { get; set; }
    }
}