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

        /// <summary>
        /// Declare that the specified property on the mapping destination should not have its value set during mapping
        /// </summary>
        /// <typeparam name="TPropertyType"></typeparam>
        /// <param name="property">The property to ignore</param>
        /// <returns></returns>
        public MappingConfig<TSrc, TDest> Ignore<TPropertyType>(Expression<Func<TDest, TPropertyType>> property)
        {
            Ignored.Add(GetPropertyName(property));
            return this;
        }

        /// <summary>
        /// Define custom mapping logic for the specified property on the destination type
        /// </summary>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="destProperty">The property that will take on the result of the mapping expression</param>
        /// <param name="mappingExpression">An expression that determines the value to set on the specified property</param>
        /// <returns></returns>
        public MappingConfig<TSrc, TDest> MapProperty<TValue>(Expression<Func<TDest, TValue>> destProperty, Expression<Func<TSrc, TValue>> mappingExpression)
        {
            var destPropetyName = GetPropertyName(destProperty);
            MappingFunctions[destPropetyName] = mappingExpression;
            return this;
        }

        /// <summary>
        /// Define an action that will be executed after mapping has completed
        /// </summary>
        /// <param name="action">The action to perform</param>
        /// <returns></returns>
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

        public MappingConfig<TSrc, TDest> SetDepth(int newDepth)
        {
            this.depth = newDepth;
            return this;
        }
    }
}
