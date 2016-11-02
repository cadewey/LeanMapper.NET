using System.Collections.Generic;
using System.Linq.Expressions;

namespace LeanMapper
{
    public abstract class MappingConfigBase
    {
        protected readonly List<string> Ignored;
        protected readonly Dictionary<string, Expression> MappingFunctions;

        protected MappingConfigBase()
        {
            Ignored = new List<string>();
            MappingFunctions = new Dictionary<string, Expression>();
        }

        internal bool ShouldIgnore(string propertyName)
        {
            return Ignored.Contains(propertyName);
        }

        internal bool HasMappingForProperty(string propertyName)
        {
            return MappingFunctions.ContainsKey(propertyName);
        }

        internal Expression GetMapping(string propertyName)
        {
            return MappingFunctions[propertyName];
        }
    }
}
