using System.Linq;
using Mono.Cecil;

public static class CecilExtentions
{
    public static string GetFileName(this TypeDefinition type)
    {
        foreach (var method in type.Methods)
        {
            var body = method.Body;
            if (body?.Instructions == null)
            {
                continue;
            }
            foreach (var instruction in body.Instructions)
            {
                var point = instruction.SequencePoint;
                if (point?.Document?.Url == null)
                {
                    continue;
                }
                return point.Document.Url;
            }
        }
        return null;
    }

    public static bool ContainsAttribute(this ICustomAttributeProvider property, string attributeName)
    {
        return property.CustomAttributes.Any(attribute => attribute.AttributeType.FullName == attributeName);
    }

}