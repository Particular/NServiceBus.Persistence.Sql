namespace NServiceBus.Persistence.Sql.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

[Generator]
public class SagaMetadataGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx => ctx.AddEmbeddedAttributeDefinition());

        var sagaDetails = context.SyntaxProvider.CreateSyntaxProvider(SyntaxLooksLikeConfigureMethod, TransformToSagaDetails)
            .Where(static d => d is not null)
            .Select(static (d, _) => d!)
            .WithTrackingName("SagaDetails");

        var collected = sagaDetails.Collect()
            .WithTrackingName("Collected");

        context.RegisterSourceOutput(collected, GenerateMetadataCode);
    }

    static bool SyntaxLooksLikeConfigureMethod(SyntaxNode node, CancellationToken cancellationToken) =>
        node is MethodDeclarationSyntax
        {
            Identifier.Text: "ConfigureHowToFindSaga",
            ParameterList.Parameters.Count: 1,
            ReturnType: PredefinedTypeSyntax predefinedType
        } methodSyntax
        && predefinedType.Keyword.IsKind(SyntaxKind.VoidKeyword)
        && methodSyntax.ParameterList.Parameters[0].Type is NameSyntax and (QualifiedNameSyntax { Right.Identifier.Text: "SagaPropertyMapper" } or SimpleNameSyntax { Identifier.Text: "SagaPropertyMapper" });

    static SagaDetails? TransformToSagaDetails(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        var methodSyntax = (MethodDeclarationSyntax)context.Node;

        var mapSagaInvocation = methodSyntax
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(invocation => invocation is
            {
                Expression: MemberAccessExpressionSyntax
                {
                    Name: IdentifierNameSyntax
                    {
                        Identifier.Text: "MapSaga"
                    },

                },
                ArgumentList.Arguments: [{ Expression: LambdaExpressionSyntax { ExpressionBody: MemberAccessExpressionSyntax } }]
            });

        var configureMethod = context.SemanticModel.GetDeclaredSymbol(methodSyntax, cancellationToken);
        var sagaType = configureMethod?.ContainingType;
        if (configureMethod is null || sagaType is null)
        {
            return null;
        }

        string tableSuffix = sagaType.Name;
        CorrelationDetails? correlation = null, transitionalCorrelation = null;

        if (mapSagaInvocation?.ArgumentList.Arguments[0].Expression is LambdaExpressionSyntax { ExpressionBody: MemberAccessExpressionSyntax correlationIdSyntax })
        {
            if (context.SemanticModel.GetOperation(correlationIdSyntax, cancellationToken) is IPropertyReferenceOperation propertyReference)
            {
                if (SagaAnalysisHelpers.TryGetCorrelationSqlPropertyType(propertyReference.Property.Type, out var correlationPropType))
                {
                    correlation = new CorrelationDetails(correlationPropType, propertyReference.Property.Name);
                }
            }
        }

        var sqlSagaAttribute = sagaType.GetAttributes()
            .FirstOrDefault(att => att.AttributeClass?.Name == "SqlSagaAttribute");

        if (sqlSagaAttribute is not null && sqlSagaAttribute.ConstructorArguments.Length == 3)
        {
            var attCorrelation = sqlSagaAttribute.ConstructorArguments[0].Value as string;
            var attTransitional = sqlSagaAttribute.ConstructorArguments[1].Value as string;

            tableSuffix = sqlSagaAttribute.ConstructorArguments[2].Value as string ?? tableSuffix;

            if ((attCorrelation is not null || attTransitional is not null) && SagaAnalysisHelpers.GetSagaDataType(sagaType) is { } dataType)
            {
                correlation = GetCorrelationData(dataType, attCorrelation) ?? correlation;
                transitionalCorrelation = GetCorrelationData(dataType, attTransitional);
            }
        }

        return new SagaDetails(sagaType.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat), correlation, transitionalCorrelation, tableSuffix);
    }

    static CorrelationDetails? GetCorrelationData(INamedTypeSymbol sagaDataType, string? propertyName)
    {
        var propSymbol = SagaAnalysisHelpers.GetCorrelationProperty(sagaDataType, propertyName);
        if (propSymbol is null || !SagaAnalysisHelpers.TryGetCorrelationSqlPropertyType(propSymbol.Type, out var sqlPropType))
        {
            return null;
        }

        return new CorrelationDetails(sqlPropType, propSymbol.Name);
    }

    static void GenerateMetadataCode(SourceProductionContext context, ImmutableArray<SagaDetails> sagas)
    {
        if (sagas.Length == 0)
        {
            return;
        }

        var b = new StringBuilder("""
                                  // <auto-generated/>

                                  #nullable enable annotations
                                  #nullable disable warnings

                                  // Suppress warnings about [Obsolete] member usage in generated code.
                                  #pragma warning disable CS0612, CS0618


                                  """);

        foreach (var saga in sagas)
        {
            _ = b.Append("[assembly:NServiceBusGeneratedSqlSagaMetadataAttribute(");
            WriteProperty(b, "SagaType", saga.SagaType, skipComma: true);
            WriteProperty(b, "CorrelationPropertyName", saga.CorrelationProperty?.Name);
            WriteProperty(b, "CorrelationPropertyType", saga.CorrelationProperty?.Type);
            WriteProperty(b, "TransitionalCorrelationPropertyName", saga.TransitionalProperty?.Name);
            WriteProperty(b, "TransitionalCorrelationPropertyType", saga.TransitionalProperty?.Type);
            WriteProperty(b, "TableSuffix", saga.TableSuffix);
            _ = b.AppendLine(")]");
        }

        _ = b.AppendLine();
        _ = b.Append("""
                     [Microsoft.CodeAnalysis.EmbeddedAttribute]
                     [System.AttributeUsage(System.AttributeTargets.Assembly, AllowMultiple = true)]
                     internal sealed class NServiceBusGeneratedSqlSagaMetadataAttribute : System.Attribute
                     {
                         public string? SagaType { get; set; }
                         public string? CorrelationPropertyName { get; set; }
                         public string? CorrelationPropertyType { get; set; }
                         public string? TransitionalCorrelationPropertyName { get; set; }
                         public string? TransitionalCorrelationPropertyType { get; set; }
                         public string? TableSuffix { get; set; }
                     }
                     """);

        context.AddSource("GeneratedSqlSagaMetadata.g.cs", b.ToString());
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    static void WriteProperty(StringBuilder b, string property, string? value, bool skipComma = false)
    {
        if (value is null)
        {
            return;
        }

        if (!skipComma)
        {
            b.Append(", ");
        }
        b.Append(property);
        b.Append(" = \"");
        b.Append(value);
        b.Append("\"");
    }

    record SagaDetails(string SagaType, CorrelationDetails? CorrelationProperty, CorrelationDetails? TransitionalProperty, string? TableSuffix);
    readonly record struct CorrelationDetails(string Type, string Name);
}