namespace NServiceBus.Persistence.Sql.Analyzer;

using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class SqlSagaAttributeAnalyzer : DiagnosticAnalyzer
{
    static readonly DiagnosticDescriptor CorrelationPropertyNotFound = new(
        id: DiagnosticIds.CorrelationPropertyNotFound,
        title: "A saga data property named in SqlSaga could not be found",
        messageFormat: "Saga data property '{0}' was not found",
        category: "Code",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [CorrelationPropertyNotFound];

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterCompilationStartAction(StartAnalysis);
    }

    static void StartAnalysis(CompilationStartAnalysisContext context)
    {
        var sqlSagaAttribute = context.Compilation.GetTypeByMetadataName("NServiceBus.Persistence.Sql.SqlSagaAttribute");
        if (sqlSagaAttribute is null)
        {
            return;
        }

        context.RegisterSyntaxNodeAction(syntaxContext => AnalyzeAttribute(syntaxContext, sqlSagaAttribute), Microsoft.CodeAnalysis.CSharp.SyntaxKind.Attribute);
    }

    static void AnalyzeAttribute(SyntaxNodeAnalysisContext context, INamedTypeSymbol sqlSagaAttribute)
    {
        if (context.Node is not AttributeSyntax attributeSyntax || !IsSqlSagaAttributeName(attributeSyntax.Name))
        {
            return;
        }

        var attributeSymbol = context.SemanticModel.GetSymbolInfo(attributeSyntax, context.CancellationToken).Symbol as IMethodSymbol;
        if (attributeSymbol is not { ContainingType: var containingType } || !SymbolEqualityComparer.Default.Equals(containingType, sqlSagaAttribute))
        {
            return;
        }

        if (attributeSyntax.Parent?.Parent is not TypeDeclarationSyntax typeDeclaration)
        {
            return;
        }

        if (context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        if (SagaAnalysisHelpers.GetSagaDataType(typeSymbol) is not { } sagaDataType)
        {
            return;
        }

        AnalyzeProperty(context, attributeSyntax, sagaDataType, parameterName: "correlationProperty", constructorIndex: 0);
        AnalyzeProperty(context, attributeSyntax, sagaDataType, parameterName: "transitionalCorrelationProperty", constructorIndex: 1);
    }

    static void AnalyzeProperty(SyntaxNodeAnalysisContext context, AttributeSyntax attributeSyntax, INamedTypeSymbol sagaDataType, string parameterName, int constructorIndex)
    {
        var argumentSyntax = GetArgumentSyntax(attributeSyntax, parameterName, constructorIndex);
        if (argumentSyntax is null)
        {
            return;
        }

        var constantValue = context.SemanticModel.GetConstantValue(argumentSyntax.Expression, context.CancellationToken);
        if (!constantValue.HasValue || constantValue.Value is not string propertyName)
        {
            return;
        }

        if (SagaAnalysisHelpers.GetCorrelationProperty(sagaDataType, propertyName) is not null)
        {
            return;
        }

        var diagnostic = Diagnostic.Create(CorrelationPropertyNotFound, argumentSyntax.Expression.GetLocation(), propertyName);
        context.ReportDiagnostic(diagnostic);
    }

    static bool IsSqlSagaAttributeName(NameSyntax nameSyntax) =>
        nameSyntax switch
        {
            IdentifierNameSyntax { Identifier.Text: "SqlSaga" or "SqlSagaAttribute" } => true,
            QualifiedNameSyntax { Right: var right } => IsSqlSagaAttributeName(right),
            AliasQualifiedNameSyntax { Name: var name } => IsSqlSagaAttributeName(name),
            _ => false
        };

    static AttributeArgumentSyntax? GetArgumentSyntax(AttributeSyntax attributeSyntax, string parameterName, int constructorIndex)
    {
        var arguments = attributeSyntax.ArgumentList?.Arguments;
        if (arguments is null || arguments.Value.Count == 0)
        {
            return null;
        }

        var namedArgument = arguments.Value.FirstOrDefault(argument =>
            argument.NameColon?.Name.Identifier.Text == parameterName ||
            argument.NameEquals?.Name.Identifier.Text == parameterName);
        if (namedArgument is not null)
        {
            return namedArgument;
        }

        if (constructorIndex < arguments.Value.Count)
        {
            return arguments.Value[constructorIndex];
        }

        return null;
    }
}
