using System;
using System.Reflection;

namespace ValheimSessionChronicle.Utility
{
    internal static class ValheimReflection
    {
        private const BindingFlags AnyInstance =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        public static object GetMemberValue(object target, string memberName)
        {
            if (target == null || string.IsNullOrWhiteSpace(memberName))
            {
                return null;
            }

            Type type = target as Type ?? target.GetType();
            object instance = target is Type ? null : target;

            FieldInfo field = type.GetField(memberName, AnyInstance | BindingFlags.Static);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            PropertyInfo property = type.GetProperty(memberName, AnyInstance | BindingFlags.Static);
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
