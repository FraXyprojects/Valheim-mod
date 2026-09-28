using System;
using System.Collections.Generic;
using System.Reflection;

namespace ValheimSessionChronicle.Utility
{
    internal static class ValheimReflection
    {
        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private struct MemberKey : IEquatable<MemberKey>
        {
            public readonly Type TargetType;
            public readonly string MemberName;

            public MemberKey(Type type, string name)
            {
                TargetType = type;
                MemberName = name;
            }

            public bool Equals(MemberKey other)
            {
                return TargetType == other.TargetType && MemberName == other.MemberName;
            }

            public override bool Equals(object obj)
            {
                return obj is MemberKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((TargetType != null ? TargetType.GetHashCode() : 0) * 397) ^ (MemberName != null ? MemberName.GetHashCode() : 0);
                }
            }
        }

        private struct MethodKey : IEquatable<MethodKey>
        {
            public readonly Type TargetType;
            public readonly string MethodName;
            public readonly int ArgumentCount;

            public MethodKey(Type type, string name, int argCount)
            {
                TargetType = type;
                MethodName = name;
                ArgumentCount = argCount;
            }

            public bool Equals(MethodKey other)
            {
                return TargetType == other.TargetType && MethodName == other.MethodName && ArgumentCount == other.ArgumentCount;
            }

            public override bool Equals(object obj)
            {
                return obj is MethodKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = (TargetType != null ? TargetType.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (MethodName != null ? MethodName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ ArgumentCount;
                    return hashCode;
                }
            }
        }

        private static readonly Dictionary<MemberKey, FieldInfo> _fieldCache = new Dictionary<MemberKey, FieldInfo>();
        private static readonly Dictionary<MemberKey, PropertyInfo> _propertyCache = new Dictionary<MemberKey, PropertyInfo>();
        private static readonly Dictionary<MethodKey, MethodInfo> _methodCache = new Dictionary<MethodKey, MethodInfo>();

        // ⚡ Bolt: Caching reflection lookups significantly reduces CPU overhead,
        // especially during frequent Update loop evaluations in SessionWatcher.
        public static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;

            var key = new MemberKey(type, memberName);

            if (_fieldCache.TryGetValue(key, out FieldInfo cachedField))
            {
                if (cachedField != null)
                {
                    return cachedField.GetValue(instance);
                }
            }
            else if (_propertyCache.TryGetValue(key, out PropertyInfo cachedProperty))
            {
                if (cachedProperty != null)
                {
                    return cachedProperty.GetValue(instance, null);
                }
            }
            else
            {
                FieldInfo field = type.GetField(memberName, AnyInstance | BindingFlags.Static);
                if (field != null)
                {
                    _fieldCache[key] = field;
                    return field.GetValue(instance);
                }

                PropertyInfo property = type.GetProperty(memberName, AnyInstance | BindingFlags.Static);
                if (property != null && property.GetIndexParameters().Length == 0)
                {
                    _propertyCache[key] = property;
                    return property.GetValue(instance, null);
                }

                _fieldCache[key] = null;
                _propertyCache[key] = null;
            }

            return null;
        }

        public static T GetMemberValue<T>(object target, string memberName, T fallback = default(T))
        {
            object value = GetMemberValue(target, memberName);
            if (value is T typed)
            {
                return typed;
            }

            return fallback;
        }

        public static object Invoke(object target, string methodName, params object[] args)
        {
            if (target == null || string.IsNullOrWhiteSpace(methodName))
            {
                return null;
            }

            Type type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;
            int argCount = args == null ? 0 : args.Length;
            var key = new MethodKey(type, methodName, argCount);

            if (_methodCache.TryGetValue(key, out MethodInfo cachedMethod))
            {
                if (cachedMethod != null)
                {
                    return cachedMethod.Invoke(instance, args);
                }
                return null;
            }

            MethodInfo method = FindMethod(type, methodName, argCount);
            _methodCache[key] = method;

            if (method != null)
            {
                return method.Invoke(instance, args);
            }

            return null;
        }

        public static bool TryGetBool(object target, string memberName, out bool value)
        {
            object raw = GetMemberValue(target, memberName);
            if (raw is bool boolValue)
            {
                value = boolValue;
                return true;
            }

            value = false;
            return false;
        }

        private static MethodInfo FindMethod(Type type, string methodName, int argumentCount)
        {
            while (type != null)
            {
                MethodInfo[] methods = type.GetMethods(AnyInstance | BindingFlags.Static);
                for (int index = 0; index < methods.Length; index++)
                {
                    MethodInfo method = methods[index];
                    if (method.Name == methodName && method.GetParameters().Length == argumentCount)
                    {
                        return method;
                    }
                }

                type = type.BaseType;
            }

            return null;
        }
    }
}
