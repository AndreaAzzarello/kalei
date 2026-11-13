using System.Reflection;

namespace Kalei.Tests.Logging;

/// <summary>
/// S10: a log can carry no path and no file name. The strongest guarantee is by construction: no public member of the
/// logging types can accept or expose free text or a path-like object, so a path cannot be handed to the log at all.
/// </summary>
internal static class LogShapeRules
{
    public static IReadOnlyList<string> Violations(IEnumerable<Type> loggingTypes)
    {
        var violations = new List<string>();
        foreach (var type in loggingTypes)
        {
            const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var ctor in type.GetConstructors(Flags))
            {
                CheckParameters(type, ".ctor", ctor.GetParameters(), violations);
            }

            foreach (var method in type.GetMethods(Flags).Where(m => !IsInheritedFromObject(m)))
            {
                CheckParameters(type, method.Name, method.GetParameters(), violations);
            }

            foreach (var property in type.GetProperties(Flags))
            {
                CheckType(type, property.Name, property.PropertyType, violations);
            }

            foreach (var field in type.GetFields(Flags))
            {
                CheckType(type, field.Name, field.FieldType, violations);
            }
        }

        return violations;
    }

    private static bool IsInheritedFromObject(MethodInfo method)
    {
        var root = method.GetBaseDefinition().DeclaringType;
        return root == typeof(object) || root == typeof(ValueType);
    }

    private static void CheckParameters(Type owner, string member, ParameterInfo[] parameters, List<string> violations)
    {
        foreach (var parameter in parameters)
        {
            CheckType(owner, $"{member}({parameter.Name})", parameter.ParameterType, violations);
        }
    }

    private static void CheckType(Type owner, string member, Type type, List<string> violations)
    {
        if (type.HasElementType)
        {
            type = type.GetElementType()!;
        }

        foreach (var argument in type.IsGenericType ? type.GetGenericArguments() : [])
        {
            CheckType(owner, member, argument, violations);
        }

        if (IsFreeText(type))
        {
            violations.Add($"{owner.Name}.{member} uses {type.Name}");
        }
    }

    private static bool IsFreeText(Type type) =>
        type == typeof(string)
        || type == typeof(char)
        || type == typeof(object)
        || type == typeof(Uri)
        || type == typeof(ReadOnlySpan<char>)
        || type == typeof(ReadOnlyMemory<char>)
        || type.IsAssignableTo(typeof(FileSystemInfo))
        || type.IsAssignableTo(typeof(Exception));
}
