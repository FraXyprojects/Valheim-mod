using System;
using System.Collections.Generic;
using System.Reflection;

namespace ValheimSessionChronicle.Utility
{
    internal static class ValheimReflection
    {
        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        private static readonly Dictionary<string, FieldInfo> _fieldCache = new Dictionary<string, FieldInfo>();
        private static readonly Dictionary<string, PropertyInfo> _propertyCache = new Dictionary<string, PropertyInfo>();
        private static readonly Dictionary<string, MethodInfo> _methodCache = new Dictionary<string, MethodInfo>();

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

            string key = type.FullName + "." + memberName;

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
            string key = type.FullName + "." + methodName + "." + argCount;

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
