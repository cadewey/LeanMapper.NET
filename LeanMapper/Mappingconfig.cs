using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace LeanMapper
{
    public sealed class MappingConfig<TSrc, TDest> : MappingConfigBase
    {
        internal readonly List<Action<TSrc, TDest>> AfterMappingActions;

        public MappingConfig()
        {
            AfterMappingActions = new List<Action<TSrc, TDest>>();
        }

        public MappingConfig<TSrc, TDest> Ignore<TPropertyType>(Expression<Func<TDest, TPropertyType>> property)
        {
            Ignored.Add(GetPropertyName(property));
            return this;
        }

        public MappingConfig<TSrc, TDest> MapProperty<TValue>(Expression<Func<TDest, TValue>> destProperty, Expression<Func<TSrc, TValue>> mappingExpression)
        {
            var destPropetyName = GetPropertyName(destProperty);
            MappingFunctions[destPropetyName] = mappingExpression;
            return this;
        }

        public MappingConfig<TSrc, TDest> AfterMapping(Action<TSrc, TDest> action)
        {
            AfterMappingActions.Add(action);
            return this;
        }

        private string GetPropertyName(Expression propertyExpression)
        {
            var memberExpression = (propertyExpression as LambdaExpression)?.Body as MemberExpression;
            var propertyInfo = memberExpression?.Member as PropertyInfo;
            var propertyName = propertyInfo?.Name;

            if (String.IsNullOrEmpty(propertyName))
                throw new ArgumentException("Invalid property expression");

            return propertyName;
        }
    }
}
