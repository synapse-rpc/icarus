using System;
using System.Collections.Generic;
namespace Icarus
{
    public class BaseCallback
    {
        public virtual Dictionary<string, string> RegAlias()
        {
            return new Dictionary<string, string>() { };
        }
    }
}
