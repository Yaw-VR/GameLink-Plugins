using System;
using System.Collections.Generic;
using System.Text;

namespace SharedLib
{
    internal class Validator
    {
        public const string IP_ADDRESS = @"^\d{1,3}(\.\d{1,3}){3}$";
        public const string PORT = @"^\d{1,5}$";
    }
}
