using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

#if NET452
using System.Reflection.Emit;
#endif

namespace LeanMapper
{
    /// <summary>
    /// Provides access to the LeanMapper API
    /// </summary>
    public static class Mapper
    {
        private const int DefaultMaxDepth = 10;
        private static ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>> _mapperMethodCache;
        private static ConcurrentDictionary<Type, ConcurrentDictionary<Type, MappingConfigBase>> _mappingConfigs;

        public static int MaxDepth { get; set; } = DefaultMaxDepth;

        static Mapper()
        {
            _mapperMethodCache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>>();
            _mappingConfigs = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, MappingConfigBase>>();
        }

        public static void Reset()
        {
            MaxDepth = DefaultMaxDepth;
            _mapperMethodCache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>>();
            _mappingConfigs = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, MappingConfigBase>>();
        }

        /// <summary>
        /// Declare a new custom configuration that maps objects of TSrc onto objects of type TDest.
        /// </summary>
        /// <typeparam name="TSrc">The type of the source (in) object</typeparam>
        /// <typeparam name="TDest">The type of the destination (out) object</typeparam>
        /// <returns>A new MappingConfig instance for the specified types</returns>
        public static MappingConfig<TSrc, TDest> Config<TSrc, TDest>()
        {
            var config = new MappingConfig<TSrc, TDest>();
            AddConfig(typeof(TSrc), typeof(TDest), config);
            return config;
        }

        private static void AddConfig(Type srcType, Type destType, MappingConfigBase config)
        {
            if (!_mappingConfigs.ContainsKey(srcType))
                _mappingConfigs[srcType] = new ConcurrentDictionary<Type, MappingConfigBase>();

            _mappingConfigs[srcType][destType] = config;
        }

        private static MappingConfigBase FindConfig(Type srcType, Type destType)
        {
            if (_mappingConfigs.ContainsKey(srcType) && _mappingConfigs[srcType].ContainsKey(destType))
                return _mappingConfigs[srcType][destType];

            return null;
        }

        private static Dictionary<Type, MappingConfigBase> FindBaseConfigs(Type srcType, Type destType)
        {
            var configs = new Dictionary<Type, MappingConfigBase>();

            if (srcType != null && _mappingConfigs.ContainsKey(srcType))
                configs = _mappingConfigs[srcType].Where(d => d.Key.GetTypeInfo().IsAssignableFrom(destType) && d.Key != destType)
                    .ToDictionary(k => k.Key, e => e.Value);

            return configs;
        }

        private static Dictionary<Type, MappingConfigBase> FindInheritedConfigs(Type srcType, Type destType)
        {
            var sourceTypes = _mappingConfigs.Where(x => x.Key.GetTypeInfo().IsAssignableFrom(srcType));
            var result = sourceTypes
                .SelectMany(x => x.Value.Where(y => y.Key.GetTypeInfo().IsAssignableFrom(destType) && y.Key != destType))
                .ToDictionary(z => z.Key, z => z.Value);
            return result;
        }

        private static List<MappingConfigBase> FindAndMergeConfigs(Type srcType, Type destType)
        {
            var configs = new List<MappingConfigBase>();
            var exactTypeConfig = FindConfig(srcType, destType);
            var baseConfigs = FindBaseConfigs(srcType, destType).Values;

            if (exactTypeConfig != null)
                configs.Add(exactTypeConfig);

            configs.AddRange(baseConfigs);

            return configs;
        }

        /// <summary>
        /// Map an object of type TIn onto a new object of type TOut.
        /// </summary>
        /// <typeparam name="TIn">The type of the source (in) object</typeparam>
        /// <typeparam name="TOut">The type of the destination (out) object</typeparam>
        /// <param name="inObj">The object being mapped onto the new TOut instance</param>
        /// <returns>A new instance of TOut, with values from inObj mapped onto its properties</returns>
        public static TOut Map<TIn, TOut>(TIn inObj)
            where TOut : class, new()
        {
            if (inObj == null)
                return null;

            var inType = typeof(TIn);
            var outType = typeof(TOut);

            if (!_mapperMethodCache.ContainsKey(inType))
                _mapperMethodCache[inType] = new ConcurrentDictionary<Type, Func<object, object>>();

            if (!_mapperMethodCache[inType].ContainsKey(outType))
                BuildExpressionTreeForMapping(inType, outType);

            var outObj = (TOut)_mapperMethodCache[inType][outType](inObj);
            var afterMapActions = FindAndMergeConfigs(inType, outType).Cast<MappingConfig<TIn, TOut>>().SelectMany(c => c.AfterMappingActions).ToList();
            afterMapActions.ForEach(a => a(inObj, outObj));

            return outObj;
        }

        /// <summary>
        /// Map a collection of objects of type TIn onto a new collection of TOut objects.
        /// </summary>
        /// <typeparam name="TIn">The type of the source (in) object</typeparam>
        /// <typeparam name="TOut">The type of the destination (out) object</typeparam>
        /// <param name="inCollection">The collection of objects being mapped onto the new TOut collection</param>
        /// <returns>A new collection of TOut objects, created by mapping the individual objects of inCollection
        /// onto new TOut instances.</returns>
        public static IEnumerable<TOut> MapCollection<TIn, TOut>(IEnumerable<TIn> inCollection)
            where TOut : class, new()
        {
            return inCollection?.ToList().Select(Map<TIn, TOut>) ?? new List<TOut>();
        }

        private static Type DetermineUnderlyingType(Type t)
        {
            return Nullable.GetUnderlyingType(t) ?? t;
        }

        private static bool PublicallyAccessible(Type inType, Type outType)
        {
            var inTypeInfo = inType.GetTypeInfo();
            var outTypeInfo = outType.GetTypeInfo();

            if (!inTypeInfo.IsPublic || !outTypeInfo.IsPublic)
                return false;

            return inTypeInfo.GetProperties().All(p => p.PropertyType.GetTypeInfo().IsPublic) &&
                outTypeInfo.GetProperties().All(p => p.PropertyType.GetTypeInfo().IsPublic);
        }

        private static void BuildExpressionTreeForMapping(Type inType, Type outType)
        {
            var inVariableObj = Expression.Variable(typeof(object), $"var_{inType.Name}");
            var inVariable = Expression.Variable(inType);
            var inVariableAssign = Expression.Assign(inVariable, Expression.Convert(inVariableObj, inType));
            var locals = new List<ParameterExpression> { inVariable };
            var initBlock = BuildMemberAssignments(inType, outType, inVariable);
            var block = Expression.Block(locals, inVariableAssign, initBlock);

#if NET452
            if (PublicallyAccessible(inType, outType))
            {
                var assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                    new AssemblyName("MyAssembly_" + Guid.NewGuid().ToString("N")),
                    AssemblyBuilderAccess.Run);

                var moduleBuilder = assemblyBuilder.DefineDynamicModule("Module");
                var typeBuilder = moduleBuilder.DefineType("MyType_" + Guid.NewGuid().ToString("N"), TypeAttributes.Public);
                var methodBuilder = typeBuilder.DefineMethod("MapMethod", MethodAttributes.Public | MethodAttributes.Static);
                var expression = Expression.Lambda<Func<object, object>>(block, inVariableObj);
                expression.CompileToMethod(methodBuilder);

                var resultingType = typeBuilder.CreateType();
                var function = Delegate.CreateDelegate(expression.Type, resultingType.GetMethod("MapMethod"));
                _mapperMethodCache[inType][outType] = (Func<object, object>)function;
                return;
            }
#endif
            var func = Expression.Lambda<Func<object, object>>(block, inVariableObj).Compile();
            _mapperMethodCache[inType][outType] = func;
        }

        private static MemberAssignment DetermineMemberAssignmentForProperties(PropertyInfo p, PropertyInfo inProp, MethodCallExpression inPropertyGet, Type inType)
        {
            Expression getExpression = null;

            if (p.PropertyType != inProp.PropertyType)
            {
                var outPropertyType = p.PropertyType;
                var outUnderlyingType = DetermineUnderlyingType(outPropertyType);
                var inUnderlyingType = DetermineUnderlyingType(inProp.PropertyType);

                if (outUnderlyingType == inUnderlyingType && outUnderlyingType.GetTypeInfo().IsValueType)
                {
                    getExpression = Expression.Convert(inPropertyGet, outPropertyType);
                }
                else if (outPropertyType == typeof(string))
                {
                    var toString = inProp.PropertyType.GetTypeInfo().GetMethod("ToString", new Type[] { });

                    if (!inProp.PropertyType.GetTypeInfo().IsValueType)
                    {
                        var nullCheck = Expression.NotEqual(inPropertyGet, Expression.Constant(null, inProp.PropertyType));
                        getExpression = Expression.Condition(nullCheck, Expression.Call(inPropertyGet, toString), Expression.Constant(null, typeof(string)));
                    }
                    else
                        getExpression = Expression.Call(inPropertyGet, toString);
                }
                else if (typeof(IEnumerable).GetTypeInfo().IsAssignableFrom(p.PropertyType))
                {
                    getExpression = DetermineGetExpressionForCollection(p, inProp, inPropertyGet);

                }
                else if (outPropertyType.GetTypeInfo().BaseType == typeof(Enum))
                {
                    getExpression = DetermineGetExpressionForEnum(p, inProp, inPropertyGet);
                }
                else if (typeof(IConvertible).GetTypeInfo().IsAssignableFrom(inType))
                {
                    var outTypeConst = Expression.Constant(outPropertyType, typeof(Type));
                    var convert = Expression.Call(null, typeof(Convert).GetTypeInfo().GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }),
                        Expression.Convert(inPropertyGet, typeof(object)), outTypeConst);
                    getExpression = Expression.Convert(convert, outPropertyType);
                }
                else
                {
                    // Build an initializer block for the child object
                    getExpression = BuildInitializerBlock(p.PropertyType, inProp.PropertyType, inPropertyGet);
                }
            }
            else
            {
                getExpression = DetermineGetExpression(p, inProp, inPropertyGet);
            }

            return Expression.Bind(p, getExpression);
        }

        private static Expression DetermineGetExpression(PropertyInfo p, PropertyInfo inProp, Expression inPropertyGet)
        {
            if (p.PropertyType.GetTypeInfo().IsValueType || p.PropertyType == p.DeclaringType || p.PropertyType.GetTypeInfo().GetConstructor(new Type[] { }) == null)
                return inPropertyGet; // Do a ref assign, otherwise we create a circular reference
            else
            {
                // Build an initializer block for the child object
                return BuildInitializerBlock(p.PropertyType, inProp.PropertyType, inPropertyGet);
            }
        }

        private static Expression BuildInitializerBlock(Type outType, Type inType, Expression inValue)
        {
            var initBlock = BuildMemberAssignments(inType, outType, inValue);
            var nullCheck = Expression.NotEqual(inValue, Expression.Constant(null, inType));
            return Expression.Condition(nullCheck, initBlock, Expression.Constant(null, outType));
        }

        private static int stack = 0;
        private static int stopDepth = MaxDepth;
        private static int previousStopDepth = MaxDepth;

        private static MemberInitExpression BuildMemberAssignments(Type inType, Type outType, Expression inVariable)
        {
            var config = FindConfig(inType, outType);
            if (stack == 0)
            {
                previousStopDepth = stopDepth;
                stopDepth = (config?.GetDepth() ?? MaxDepth);
            }

            stack++;
            var ctor = Expression.New(outType);
            var memberAssignments = new List<MemberAssignment>();
            var props = outType.GetTypeInfo().GetProperties();
            var baseConfigs = FindInheritedConfigs(inType, outType);

            if (stack <= stopDepth)
            {
            foreach (var p in props)
            {
                MemberAssignment outValueAssign = null;
                var baseMapping = baseConfigs?.Values.FirstOrDefault(v => v.HasMappingForProperty(p.Name));

                if (config?.ShouldIgnore(p.Name) == true)
                    continue;

                if (config?.HasMappingForProperty(p.Name) == true)
                {
                    var mappingFunc = Expression.Invoke(config.GetMapping(p.Name), inVariable);
                    outValueAssign = Expression.Bind(p, mappingFunc);
                }
                else if (baseMapping != null)
                {
                    var mappingFunc = Expression.Invoke(baseMapping.GetMapping(p.Name), inVariable);
                    outValueAssign = Expression.Bind(p, mappingFunc);
                }
                else
                {
                    var inProp = inType.GetTypeInfo().GetProperty(p.Name);

                    if (inProp == null)
                        continue;

                    var inPropertyGet = Expression.Call(inVariable, inProp.GetGetMethod());
                    outValueAssign = DetermineMemberAssignmentForProperties(p, inProp, inPropertyGet, inType);
                }

                memberAssignments.Add(outValueAssign);
            }
            }

            stack--;
            if (stack == 0)
            {
                stopDepth = previousStopDepth;
            }
            return Expression.MemberInit(ctor, memberAssignments);
        }

        private static Expression DetermineGetExpressionForCollection(PropertyInfo p, PropertyInfo inProp, MethodCallExpression inPropertyGet)
        {
            var inPropTypeinfo = inProp.PropertyType.GetTypeInfo();
            var inElementType = inProp.PropertyType.GetElementType() ?? inPropTypeinfo.GetGenericArguments().First();
            var outElementType = p.PropertyType.GetElementType() ?? p.PropertyType.GetTypeInfo().GetGenericArguments().First();
            var loopVar = Expression.Variable(typeof(int), "idx");
            var collectionLength = Expression.Variable(typeof(int), "len");
            var sourceCollection = Expression.Variable(inElementType.MakeArrayType(), "src");
            var arrVar = Expression.Variable(outElementType.MakeArrayType(), "arr");
            var breakLabel = Expression.Label("EndLoop");
            var initAssign = Expression.Assign(loopVar, Expression.Constant(0, typeof(int)));
            var increment = Expression.PostIncrementAssign(loopVar);

            var propInfos = GetPublicProperties(inProp.PropertyType);

            var lengthPropInfo = inPropTypeinfo.GetProperty("Length");
            var countPropInfo = inPropTypeinfo.GetProperty("Count");
            var lengthMethodInfo = lengthPropInfo?.GetGetMethod();
            var countMethodInfo = countPropInfo?.GetGetMethod();

            var getLen = (lengthMethodInfo != null)
                ? Expression.Call(inPropertyGet, lengthMethodInfo)
                : (countMethodInfo != null)
                    ? Expression.Call(inPropertyGet, countMethodInfo)
                    : Expression.Call(inPropertyGet, propInfos.First(x => x.Name == "Length" || x.Name == "Count").GetGetMethod());

            var newArr = Expression.NewArrayBounds(outElementType, getLen);

            var arr = Expression.Assign(arrVar, newArr);
            var lenAssign = Expression.Assign(collectionLength, Expression.ArrayLength(arrVar));
            var srcAssign = (inPropTypeinfo.BaseType == typeof(Array))
                ? Expression.Assign(sourceCollection, inPropertyGet)
                : Expression.Assign(sourceCollection, Expression.Call(null, typeof(Enumerable).GetTypeInfo().GetMethod("ToArray").MakeGenericMethod(inElementType), inPropertyGet));

            var loop = Expression.Block(
                new[] { arrVar, loopVar, collectionLength, sourceCollection },
                arr,
                initAssign,
                srcAssign,
                lenAssign,
                Expression.Loop(
                    Expression.IfThenElse(
                        Expression.LessThan(loopVar, collectionLength),
                        Expression.Block(
                            Expression.Assign(Expression.ArrayAccess(arrVar, loopVar),
                                BuildMemberAssignments(inElementType, outElementType,
                                Expression.ArrayIndex(sourceCollection, loopVar))),
                            increment
                        ),
                        Expression.Break(breakLabel, arrVar)
                    ),
                    breakLabel),
                arrVar);

            var convertExpression = p.PropertyType.GetTypeInfo().BaseType == typeof(Array)
                ? loop
                : p.PropertyType.GetTypeInfo().GetInterfaces().Any(i => i.Name.Contains("IList"))
                    ? Expression.Call(null, typeof(Enumerable).GetTypeInfo().GetMethod("ToList").MakeGenericMethod(outElementType), loop)
                    : Expression.Convert(loop, p.PropertyType) as Expression;

            var nullCheck = Expression.NotEqual(inPropertyGet, Expression.Constant(null, inProp.PropertyType));
            return Expression.Condition(nullCheck, convertExpression, Expression.Constant(null, p.PropertyType));
        }

        private static Expression DetermineGetExpressionForEnum(PropertyInfo p, PropertyInfo inProp, MethodCallExpression inPropertyGet)
        {
            var outTypeConst = Expression.Constant(p.PropertyType, typeof(Type));

            if (inProp.PropertyType == typeof(string))
            {
                var parse = Expression.Call(null, typeof(Enum).GetTypeInfo().GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) }),
                    outTypeConst, inPropertyGet, Expression.Constant(true, typeof(bool)));
                var castAfterParse = Expression.Convert(parse, p.PropertyType);
                var defaultValue = Expression.Default(p.PropertyType);
                var nullOrEmpty = Expression.Call(null, typeof(string).GetTypeInfo().GetMethod("IsNullOrEmpty"), inPropertyGet);
                return Expression.Condition(Expression.NotEqual(nullOrEmpty, Expression.Constant(true, typeof(bool))), castAfterParse, defaultValue);
            }
            else
            {
                var objCast = Expression.Convert(inPropertyGet, typeof(object));
                var enumConvert = Expression.Call(null, typeof(Enum).GetTypeInfo().GetMethod("ToObject", new[] { typeof(Type), typeof(object) }), outTypeConst, objCast);
                return Expression.Convert(enumConvert, p.PropertyType);
            }
        }

        /// <summary>
        /// Builds a collection of PropertyInfo for the properties of a type including properties on inherited interfaces
        /// </summary>
        /// <param name="type">The Type for which to get properties.</param>
        /// <returns>An array of PropertyInfo of a type including properties on inherited interfaces</returns>
        private static PropertyInfo[] GetPublicProperties(this Type type)
        {
            var typeInfo = type.GetTypeInfo();
            if (typeInfo.IsInterface)
            {
                var propertyInfos = new List<PropertyInfo>();

                var considered = new List<TypeInfo>();
                var queue = new Queue<TypeInfo>();
                considered.Add(typeInfo);
                queue.Enqueue(typeInfo);
                while (queue.Count > 0)
                {
                    var subType = queue.Dequeue();
                    foreach (var subInterface in subType.GetInterfaces())
                    {
                        var subInterfaceTypeInfo = subInterface.GetTypeInfo();
                        if (considered.Contains(subInterfaceTypeInfo)) continue;

                        considered.Add(subInterfaceTypeInfo);
                        queue.Enqueue(subInterfaceTypeInfo);
                    }

                    var typeProperties = subType.GetProperties(
                        BindingFlags.FlattenHierarchy
                        | BindingFlags.Public
                        | BindingFlags.Instance);

                    var newPropertyInfos = typeProperties
                        .Where(x => !propertyInfos.Contains(x));

                    propertyInfos.InsertRange(0, newPropertyInfos);
                }

                return propertyInfos.ToArray();
            }

            return typeInfo.GetProperties(BindingFlags.FlattenHierarchy
                | BindingFlags.Public | BindingFlags.Instance);
    }
}

}