﻿namespace WpfAnalyzers.PropertyChanged.Helpers
{
    using System.Threading;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Property
    {
        internal static bool ShouldNotify(PropertyDeclarationSyntax declaration, IPropertySymbol propertySymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            if (propertySymbol.IsIndexer ||
                propertySymbol.DeclaredAccessibility != Accessibility.Public ||
                propertySymbol.IsStatic ||
                propertySymbol.IsReadOnly ||
                propertySymbol.GetMethod == null ||
                propertySymbol.IsAbstract ||
                propertySymbol.ContainingType.IsValueType ||
                propertySymbol.ContainingType.DeclaredAccessibility != Accessibility.Public)
            {
                return false;
            }

            if (IsMutableAutoProperty(declaration))
            {
                return true;
            }

            AccessorDeclarationSyntax setter;
            if (declaration.TryGetSetAccessorDeclaration(out setter))
            {
                AssignmentExpressionSyntax assignment;
                if (!AssignsValueToBackingField(setter, out assignment))
                {
                    return false;
                }

                if (PropertyChanged.InvokesPropertyChangedFor(assignment, propertySymbol, semanticModel, cancellationToken) != AnalysisResult.No)
                {
                    return false;
                }

                return true;
            }

            return false;
        }

        internal static bool IsMutableAutoProperty(PropertyDeclarationSyntax property)
        {
            var accessors = property?.AccessorList?.Accessors;
            if (accessors?.Count != 2)
            {
                return false;
            }

            foreach (var accessor in accessors.Value)
            {
                if (accessor.Body != null)
                {
                    return false;
                }
            }

            return true;
        }

        internal static bool IsSimplePropertyWithBackingField(PropertyDeclarationSyntax property, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            AccessorDeclarationSyntax getter;
            AccessorDeclarationSyntax setter;
            if (!(property.TryGetGetAccessorDeclaration(out getter) &&
                  property.TryGetSetAccessorDeclaration(out setter)))
            {
                return false;
            }

            if (getter.Body?.Statements.Count != 1 ||
                setter.Body?.Statements.Count != 1)
            {
                return false;
            }

            var returnStatement = getter.Body.Statements[0] as ReturnStatementSyntax;
            var assignment = (setter.Body.Statements[0] as ExpressionStatementSyntax)?.Expression as AssignmentExpressionSyntax;
            if (returnStatement == null ||
                assignment == null)
            {
                return false;
            }

            var returnedField = semanticModel.GetSymbolSafe(returnStatement.Expression, cancellationToken) as IFieldSymbol;
            var assignedField = semanticModel.GetSymbolSafe(assignment.Left, cancellationToken) as IFieldSymbol;
            if (assignedField == null ||
                returnedField == null)
            {
                return false;
            }

            var propertySymbol = semanticModel.GetDeclaredSymbolSafe(property, cancellationToken);
            return assignedField.Equals(returnedField) && assignedField.ContainingType == propertySymbol?.ContainingType;
        }

        internal static bool TryGetBackingField(IPropertySymbol property, SemanticModel semanticModel, CancellationToken cancellationToken, out IFieldSymbol field)
        {
            field = null;
            if (property == null)
            {
                return false;
            }

            foreach (var declaration in property.Declarations(cancellationToken))
            {
                var propertyDeclaration = declaration as PropertyDeclarationSyntax;
                if (propertyDeclaration == null)
                {
                    continue;
                }

                IdentifierNameSyntax fieldIdentifier;
                FieldDeclarationSyntax fieldDeclaration;
                if (TryGetBackingField(propertyDeclaration, out fieldIdentifier, out fieldDeclaration))
                {
                    field = semanticModel.GetSymbolSafe(fieldIdentifier, cancellationToken) as IFieldSymbol;
                    return field != null;
                }
            }

            return false;
        }

        internal static bool TryGetBackingField(PropertyDeclarationSyntax property, out IdentifierNameSyntax field, out FieldDeclarationSyntax fieldDeclaration)
        {
            field = null;
            fieldDeclaration = null;
            if (property == null)
            {
                return false;
            }

            AccessorDeclarationSyntax setter;
            if (property.TryGetSetAccessorDeclaration(out setter) &&
                setter.Body != null)
            {
                using (var pooled = AssignmentWalker.Create(setter))
                {
                    if (pooled.Item.Assignments.Count != 1)
                    {
                        return false;
                    }

                    var left = pooled.Item.Assignments[0].Left;
                    field = left as IdentifierNameSyntax;
                    if (field == null)
                    {
                        var memberAccess = left as MemberAccessExpressionSyntax;
                        if (!(memberAccess?.Expression is ThisExpressionSyntax))
                        {
                            return false;
                        }

                        field = memberAccess.Name as IdentifierNameSyntax;
                    }

                    if (field == null)
                    {
                        return false;
                    }

                    foreach (var member in property.FirstAncestorOrSelf<TypeDeclarationSyntax>().Members)
                    {
                        fieldDeclaration = member as FieldDeclarationSyntax;
                        if (fieldDeclaration != null)
                        {
                            foreach (var variable in fieldDeclaration.Declaration.Variables)
                            {
                                if (variable.Identifier.ValueText == field.Identifier.ValueText)
                                {
                                    return true;
                                }
                            }
                        }
                    }
                }
            }

            return false;
        }

        internal static bool AssignsValueToBackingField(AccessorDeclarationSyntax setter, out AssignmentExpressionSyntax assignment)
        {
            using (var pooled = AssignmentWalker.Create(setter))
            {
                foreach (var a in pooled.Item.Assignments)
                {
                    if ((a.Right as IdentifierNameSyntax)?.Identifier.ValueText != "value")
                    {
                        continue;
                    }

                    if (a.Left is IdentifierNameSyntax)
                    {
                        assignment = a;
                        return true;
                    }

                    var memberAccess = a.Left as MemberAccessExpressionSyntax;
                    if (memberAccess?.Expression is ThisExpressionSyntax &&
                        memberAccess.Name is IdentifierNameSyntax)
                    {
                        assignment = a;
                        return true;
                    }
                }
            }

            assignment = null;
            return false;
        }

        internal static bool TryFindValue(AccessorDeclarationSyntax setter, SemanticModel semanticModel, CancellationToken cancellationToken, out IParameterSymbol value)
        {
            using (var pooled = IdentifierNameWalker.Create(setter))
            {
                foreach (var identifierName in pooled.Item.IdentifierNames)
                {
                    if (identifierName.Identifier.ValueText == "value")
                    {
                        value = semanticModel.GetSymbolSafe(identifierName, cancellationToken) as IParameterSymbol;
                        if (value != null)
                        {
                            return true;
                        }
                    }
                }
            }

            value = null;
            return false;
        }
    }
}