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
    public static class Mapper
    {
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>> _mapperMethodCache;
        private static readonly ConcurrentDictionary<Type, ConcurrentDictionary<Type, MappingConfigBase>> _mappingConfigs;

        static Mapper()
        {
            _mapperMethodCache = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, Func<object, object>>>();
            _mappingConfigs = new ConcurrentDictionary<Type, ConcurrentDictionary<Type, MappingConfigBase>>();
        }

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
                configs = _mappingConfigs[srcType].Where(d => d.Key.IsAssignableFrom(destType) && d.Key != destType)
                    .ToDictionary(k => k.Key, e => e.Value);

            return configs;
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
            if (!inType.IsPublic() || !outType.IsPublic())
                return false;

            return inType.GetProperties().All(p => p.PropertyType.IsPublic()) &&
                outType.GetProperties().All(p => p.PropertyType.IsPublic());
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

                if (outUnderlyingType == inUnderlyingType && outUnderlyingType.IsValueType())
                {
                    getExpression = Expression.Convert(inPropertyGet, outPropertyType);
                }
                else if (outPropertyType == typeof(string))
                {
                    var toString = inProp.PropertyType.GetMethod("ToString", new Type[] { });

                    if (!inProp.PropertyType.IsValueType())
                    {
                        var nullCheck = Expression.NotEqual(inPropertyGet, Expression.Constant(null, inProp.PropertyType));
                        getExpression = Expression.Condition(nullCheck, Expression.Call(inPropertyGet, toString), Expression.Constant(null, typeof(string)));
                    }
                    else
                        getExpression = Expression.Call(inPropertyGet, toString);
                }
                else if (typeof(IEnumerable).IsAssignableFrom(p.PropertyType))
                {
                    getExpression = DetermineGetExpressionForCollection(p, inProp, inPropertyGet, inType);

                }
                else if (outPropertyType.BaseType() == typeof(Enum))
                {
                    getExpression = DetermineGetExpressionForEnum(p, inProp, inPropertyGet);
                }
                else if (typeof(IConvertible).IsAssignableFrom(inType))
                {
                    var outTypeConst = Expression.Constant(outPropertyType, typeof(Type));
                    var convert = Expression.Call(null, typeof(Convert).GetMethod("ChangeType", new[] { typeof(object), typeof(Type) }),
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
            if (p.PropertyType.IsValueType() || p.PropertyType == p.DeclaringType || p.PropertyType.GetConstructor(new Type[] { }) == null)
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

        private static MemberInitExpression BuildMemberAssignments(Type inType, Type outType, Expression inVariable)
        {
            var ctor = Expression.New(outType);
            var props = outType.GetProperties();
            var memberAssignments = new List<MemberAssignment>();
            var config = FindConfig(inType, outType);
            var baseConfigs = FindBaseConfigs(inType.BaseType(), outType);

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
                    var inProp = inType.GetProperty(p.Name);

                    if (inProp == null)
                        continue;

                    var inPropertyGet = Expression.Call(inVariable, inProp.GetGetMethod());
                    outValueAssign = DetermineMemberAssignmentForProperties(p, inProp, inPropertyGet, inType);
                }

                memberAssignments.Add(outValueAssign);
            }

            return Expression.MemberInit(ctor, memberAssignments);
        }

        private static Expression DetermineGetExpressionForCollection(PropertyInfo p, PropertyInfo inProp, MethodCallExpression inPropertyGet, Type inType)
        {
            var inElementType = inProp.PropertyType.GetElementType() ?? inProp.PropertyType.GetGenericArguments().First();
            var outElementType = p.PropertyType.GetElementType() ?? p.PropertyType.GetGenericArguments().First();
            var loopVar = Expression.Variable(typeof(int), "idx");
            var collectionLength = Expression.Variable(typeof(int), "len");
            var sourceCollection = Expression.Variable(inElementType.MakeArrayType(), "src");
            var arrVar = Expression.Variable(outElementType.MakeArrayType(), "arr");
            var breakLabel = Expression.Label("EndLoop");
            var initAssign = Expression.Assign(loopVar, Expression.Constant(0, typeof(int)));
            var increment = Expression.PostIncrementAssign(loopVar);

            var getLen = (inProp.PropertyType.GetProperty("Length") != null)
                ? Expression.Call(inPropertyGet, inProp.PropertyType.GetProperty("Length").GetGetMethod())
                : Expression.Call(inPropertyGet, inProp.PropertyType.GetProperty("Count").GetGetMethod());

            var newArr = Expression.NewArrayBounds(outElementType, getLen);

            var arr = Expression.Assign(arrVar, newArr);
            var lenAssign = Expression.Assign(collectionLength, Expression.ArrayLength(arrVar));
            var srcAssign = (inProp.PropertyType.BaseType() == typeof(Array))
                ? Expression.Assign(sourceCollection, inPropertyGet)
                : Expression.Assign(sourceCollection, Expression.Call(null, typeof(Enumerable).GetMethod("ToArray").MakeGenericMethod(inElementType), inPropertyGet));

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

            var convertExpression = p.PropertyType.BaseType() == typeof(Array)
                ? loop
                : p.PropertyType.GetInterfaces().Any(i => i.Name.Contains("IList"))
                    ? Expression.Call(null, typeof(Enumerable).GetMethod("ToList").MakeGenericMethod(outElementType), loop)
                    : Expression.Convert(loop, p.PropertyType) as Expression;

            var nullCheck = Expression.NotEqual(inPropertyGet, Expression.Constant(null, inProp.PropertyType));
            return Expression.Condition(nullCheck, convertExpression, Expression.Constant(null, p.PropertyType));
        }

        private static Expression DetermineGetExpressionForEnum(PropertyInfo p, PropertyInfo inProp, MethodCallExpression inPropertyGet)
        {
            var outTypeConst = Expression.Constant(p.PropertyType, typeof(Type));

            if (inProp.PropertyType == typeof(string))
            {
                var parse = Expression.Call(null, typeof(Enum).GetMethod("Parse", new[] { typeof(Type), typeof(string), typeof(bool) }),
                    outTypeConst, inPropertyGet, Expression.Constant(true, typeof(bool)));
                var castAfterParse = Expression.Convert(parse, p.PropertyType);
                var defaultValue = Expression.Default(p.PropertyType);
                var nullOrEmpty = Expression.Call(null, typeof(string).GetMethod("IsNullOrEmpty"), inPropertyGet);
                return Expression.Condition(Expression.NotEqual(nullOrEmpty, Expression.Constant(true, typeof(bool))), castAfterParse, defaultValue);
            }
            else
            {
                var objCast = Expression.Convert(inPropertyGet, typeof(object));
                var enumConvert = Expression.Call(null, typeof(Enum).GetMethod("ToObject", new[] { typeof(Type), typeof(object) }), outTypeConst, objCast);
                return Expression.Convert(enumConvert, p.PropertyType);
            }
        }
    }
}