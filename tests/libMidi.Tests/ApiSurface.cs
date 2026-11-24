using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;

namespace LibMidi.Tests;

internal static class ApiSurface
{
    private const BindingFlags Declared = BindingFlags.Public | BindingFlags.NonPublic |
        BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

    internal static string[] Describe(Assembly assembly) => assembly.GetExportedTypes()
        .SelectMany(DescribeType).Order(StringComparer.Ordinal).ToArray();

    private static IEnumerable<string> DescribeType(Type type)
    {
        string name = TypeName(type);
        string kind = type.IsEnum ? "enum" : type.IsInterface ? "interface" : type.IsValueType ? "struct" : "class";
        yield return $"{name} | {kind} abstract={type.IsAbstract} sealed={type.IsSealed} " +
            $"base={TypeName(type.BaseType)} interfaces=[{string.Join(", ", type.GetInterfaces().Select(TypeName).Order(StringComparer.Ordinal))}] " +
            $"{Constraints(type.GetGenericArguments())}{Obsolete(type)}";

        foreach (MemberInfo member in type.GetMembers(Declared))
        {
            string? signature = member switch
            {
                MethodInfo m when Visible(m) => $"{Method(m)} {ParameterType(m.ReturnParameter)} {m.Name}" +
                    $"({Parameters(m)}) {Constraints(m.GetGenericArguments())}",
                ConstructorInfo c when Visible(c) => $"{Method(c)} {c.Name}({Parameters(c)})",
                FieldInfo f when f.IsPublic || f.IsFamily || f.IsFamilyOrAssembly =>
                    $"{f.Attributes} {TypeName(f.FieldType)} {f.Name}" +
                    (f.IsLiteral ? $" = {Constant(f.GetRawConstantValue())}" : ""),
                PropertyInfo p when Visible(p.GetMethod) || Visible(p.SetMethod) =>
                    $"property {TypeName(p.PropertyType)} {p.Name}[{string.Join(", ", p.GetIndexParameters().Select(Parameter))}] " +
                    $"{{{Accessor(p.GetMethod)}{Accessor(p.SetMethod)}}}",
                EventInfo e when Visible(e.AddMethod) || Visible(e.RemoveMethod) =>
                    $"event {TypeName(e.EventHandlerType)} {e.Name} {{{Accessor(e.AddMethod)}{Accessor(e.RemoveMethod)}}}",
                _ => null
            };

            if (signature != null)
            {
                yield return $"{name} | {signature.TrimEnd()}{Obsolete(member)}";
            }
        }
    }

    private static bool Visible([NotNullWhen(true)] MethodBase? method) =>
        method != null && (method.IsPublic || method.IsFamily || method.IsFamilyOrAssembly);

    private static string Method(MethodBase method) =>
        $"{method.Attributes}";

    private static string Accessor(MethodInfo? method) => Visible(method) ? $"{Method(method)} {method.Name}; " : "";

    private static string Parameters(MethodBase method) => string.Join(", ", method.GetParameters().Select(Parameter));

    private static string Parameter(ParameterInfo parameter) =>
        $"{ParameterType(parameter)} {parameter.Name} ({parameter.Attributes})" +
        (parameter.HasDefaultValue ? $" = {Constant(parameter.RawDefaultValue)}" : "") +
        (parameter.IsDefined(typeof(ParamArrayAttribute)) ? " params" : "");

    private static string ParameterType(ParameterInfo parameter) => TypeName(parameter.ParameterType) +
        string.Concat(parameter.GetRequiredCustomModifiers().Select(t => $" modreq({TypeName(t)})")) +
        string.Concat(parameter.GetOptionalCustomModifiers().Select(t => $" modopt({TypeName(t)})"));

    private static string Constraints(IEnumerable<Type> arguments) => string.Join(" ", arguments
        .Where(t => t.IsGenericParameter).Select(t =>
            $"<{t.Name} {t.GenericParameterAttributes}: {string.Join(", ", t.GetGenericParameterConstraints().Select(TypeName).Order(StringComparer.Ordinal))}>"));

    private static string TypeName(Type? type)
    {
        if (type == null)
        {
            return "-";
        }

        if (type.IsGenericParameter)
        {
            return (type.DeclaringMethod == null ? "!" : "!!") + type.GenericParameterPosition + ":" + type.Name;
        }

        if (type.HasElementType)
        {
            string suffix = type.IsByRef ? "&" : type.IsPointer ? "*" : "[" + new string(',', type.GetArrayRank() - 1) + "]";
            return TypeName(type.GetElementType()) + suffix;
        }

        return type.IsGenericType
            ? type.GetGenericTypeDefinition().FullName + "<" + string.Join(", ", type.GetGenericArguments().Select(TypeName)) + ">"
            : type.FullName ?? type.Name;
    }

    private static string Constant(object? value) => value switch
    {
        null => "null",
        string text => System.Text.Json.JsonSerializer.Serialize(text),
        _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
    };

    private static string Obsolete(MemberInfo member) =>
        member.GetCustomAttribute<ObsoleteAttribute>() is { } attribute
            ? $" [Obsolete({Constant(attribute.Message)}, {attribute.IsError}, {Constant(attribute.DiagnosticId)}, {Constant(attribute.UrlFormat)})]"
            : "";
}
