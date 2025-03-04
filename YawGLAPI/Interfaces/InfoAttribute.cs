using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YawGLAPI
{
    [AttributeUsage(AttributeTargets.Field)]
    public class InfoAttribute : Attribute
    {
        /// <summary>
        /// The Label shown in the UI
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// The Description shown in the UI
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The Regex Validator for the field
        /// </summary>
        public string? RegexValidator { get; set; }
      
    }

}
