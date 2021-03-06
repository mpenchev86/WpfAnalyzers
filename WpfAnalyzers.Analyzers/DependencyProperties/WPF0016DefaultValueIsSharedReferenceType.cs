﻿namespace WpfAnalyzers.DependencyProperties
{
    using System.Collections.Immutable;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Diagnostics;

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    internal class WPF0016DefaultValueIsSharedReferenceType : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "WPF0016";
        private const string Title = "Default value is shared reference type.";
        private const string MessageFormat = "Default value for '{0}' is a reference type that will be shared among all instances.";
        private const string Description = "When registering a new instance of a reference type as default value the value is shared for all instances of the control.";
        private static readonly string HelpLink = WpfAnalyzers.HelpLink.ForId(DiagnosticId);

        private static readonly DiagnosticDescriptor Descriptor = new DiagnosticDescriptor(
                                                                      DiagnosticId,
                                                                      Title,
                                                                      MessageFormat,
                                                                      AnalyzerCategory.DependencyProperties,
                                                                      DiagnosticSeverity.Warning,
                                                                      AnalyzerConstants.EnabledByDefault,
                                                                      Description,
                                                                      HelpLink);

        /// <inheritdoc/>
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(Descriptor);

        /// <inheritdoc/>
        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(HandleObjectCreation, SyntaxKind.ObjectCreationExpression);
        }

        private static void HandleObjectCreation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsExcludedFromAnalysis())
            {
                return;
            }

            var objectCreation = context.Node as ObjectCreationExpressionSyntax;
            if (objectCreation == null ||
                objectCreation.IsMissing)
            {
                return;
            }

            ArgumentSyntax defaultValueArg;
            if (!PropertyMetaData.TryGetDefaultValue(
                    objectCreation,
                    context.SemanticModel,
                    context.CancellationToken,
                    out defaultValueArg))
            {
                return;
            }

            var defaultValue = defaultValueArg.Expression;
            if (IsNonEmptyArrayCreation(defaultValue as ArrayCreationExpressionSyntax, context) ||
                IsReferenceTypeCreation(defaultValue as ObjectCreationExpressionSyntax, context))
            {
                var type = context.SemanticModel.GetSymbolSafe(defaultValue, context.CancellationToken)?.ContainingType;
                if (type == KnownSymbol.FontFamily)
                {
                    return;
                }

                IFieldSymbol dp;
                if (!PropertyMetaData.TryGetDependencyProperty(objectCreation, context.SemanticModel, context.CancellationToken, out dp))
                {
                    return;
                }

                context.ReportDiagnostic(Diagnostic.Create(Descriptor, defaultValueArg.GetLocation(), dp));
            }
        }

        private static bool IsNonEmptyArrayCreation(ArrayCreationExpressionSyntax arrayCreation, SyntaxNodeAnalysisContext context)
        {
            if (arrayCreation == null)
            {
                return false;
            }

            foreach (var rank in arrayCreation.Type.RankSpecifiers)
            {
                foreach (var size in rank.Sizes)
                {
                    var constantValue = context.SemanticModel.GetConstantValueSafe(size, context.CancellationToken);
                    if (!constantValue.HasValue)
                    {
                        return true;
                    }

                    return !Equals(constantValue.Value, 0);
                }
            }

            return false;
        }

        private static bool IsReferenceTypeCreation(ObjectCreationExpressionSyntax objectCreation, SyntaxNodeAnalysisContext context)
        {
            if (objectCreation == null)
            {
                return false;
            }

            var type = context.SemanticModel.GetTypeInfoSafe(objectCreation, context.CancellationToken)
                              .Type;
            if (type.IsValueType)
            {
                return false;
            }

            return true;
        }
    }
}