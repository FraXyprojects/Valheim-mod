using System;
using System.Collections.Generic;
using System.Reflection;

namespace ValheimSessionChronicle.Utility
{
    internal static class ValheimReflection
    {
        private struct MemberKey : IEquatable<MemberKey>
        {
            public readonly Type Type;
            public readonly string Name;
            public readonly int Arguments;

            public MemberKey(Type type, string name, int arguments = 0)
            {
                Type = type;
                Name = name;
                Arguments = arguments;
            }

            public bool Equals(MemberKey other)
            {
                return Type == other.Type && string.Equals(Name, other.Name) && Arguments == other.Arguments;
            }

            public override bool Equals(object obj)
            {
                return obj is MemberKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = Type != null ? Type.GetHashCode() : 0;
                    hashCode = (hashCode * 397) ^ (Name != null ? Name.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ Arguments;
                    return hashCode;
                }
            }
        }

        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        // ⚡ Bolt: Cache reflection lookups (FieldInfo, PropertyInfo, MethodInfo)
        // to prevent unnecessary allocations array creations and GC pressure,
        // specifically when called repeatedly from Update loops (like SessionWatcher).
        // Uses struct keys to avoid string allocation GC pressure.
        private static readonly Dictionary<MemberKey, FieldInfo> _fieldCache = new Dictionary<MemberKey, FieldInfo>();
        private static readonly Dictionary<MemberKey, PropertyInfo> _propertyCache = new Dictionary<MemberKey, PropertyInfo>();
        private static readonly Dictionary<MemberKey, MethodInfo> _methodCache = new Dictionary<MemberKey, MethodInfo>();

        public static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;
            MemberKey key = new MemberKey(type, memberName);

            if (!_fieldCache.TryGetValue(key, out FieldInfo field))
            {
                field = type.GetField(memberName, AnyInstance | BindingFlags.Static);
                _fieldCache[key] = field;
            }

            if (field != null)
            {
                return field.GetValue(instance);
            }

            if (!_propertyCache.TryGetValue(key, out PropertyInfo property))
            {
                property = type.GetProperty(memberName, AnyInstance | BindingFlags.Static);
                _propertyCache[key] = property;
            }

            if (property != null && property.GetIndexParameters().Length == 0)
            {
                return property.GetValue(instance, null);
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

            MethodInfo method = FindMethod(type, methodName, args == null ? 0 : args.Length);
            if (method == null)
            {
                return null;
            }

            return method.Invoke(instance, args);
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
            MemberKey key = new MemberKey(type, methodName, argumentCount);
            if (_methodCache.TryGetValue(key, out MethodInfo cachedMethod))
            {
                return cachedMethod;
            }

            Type currentType = type;
            while (currentType != null)
            {
                MethodInfo[] methods = currentType.GetMethods(AnyInstance | BindingFlags.Static);
                for (int index = 0; index < methods.Length; index++)
                {
                    MethodInfo method = methods[index];
                    if (method.Name == methodName && method.GetParameters().Length == argumentCount)
                    {
                        _methodCache[key] = method;
                        return method;
                    }
                }

                currentType = currentType.BaseType;
            }

            _methodCache[key] = null;
            return null;
        }
    }
}
