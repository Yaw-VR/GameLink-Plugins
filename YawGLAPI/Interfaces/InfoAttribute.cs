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
        public string? Name { get; set; }
        public string? Description { get; set; }
      
    }

}
