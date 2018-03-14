using System.Collections.Generic;
namespace Rpc.Synapse.Icarus
{
    public class BaseCallback
    {
        public virtual Dictionary<string, string> RegAlias()
        {
            return new Dictionary<string, string>() { };
        }
    }
}
