﻿namespace WpfAnalyzers
{
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.CSharp.Syntax;

    internal static class Names
    {
        internal static bool UsesUnderscoreNames(this SyntaxNode node)
        {
            var typeDeclarationSyntax = node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclarationSyntax == null)
            {
                using (var pooled = UsesThisWalker.Create(node))
                {
                    return pooled.Item.UsesThis == false;
                }
            }

            foreach (var member in typeDeclarationSyntax.Members)
            {
                var field = member as FieldDeclarationSyntax;
                if (field == null ||
                    field.Modifiers.Any(SyntaxKind.StaticKeyword) ||
                    !field.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    continue;
                }

                foreach (var variable in field.Declaration.Variables)
                {
                    return variable.Identifier.ValueText.StartsWith("_");
                }
            }

            using (var pooled = UsesThisWalker.Create(typeDeclarationSyntax))
            {
                return pooled.Item.UsesThis == false;
            }
        }

        internal sealed class UsesThisWalker : CSharpSyntaxWalker
        {
            private static readonly Pool<UsesThisWalker> Cache = new Pool<UsesThisWalker>(
                () => new UsesThisWalker(),
                x =>
                {
                    x.usesThis = false;
                    x.useUnderScore = false;
                });

            private bool usesThis;
            private bool useUnderScore;

            private UsesThisWalker()
            {
            }

            public bool? UsesThis
            {
                get
                {
                    if (this.usesThis == this.useUnderScore)
                    {
                        return null;
                    }

                    if (this.usesThis && !this.useUnderScore)
                    {
                        return true;
                    }

                    return false;
                }
            }

            public static Pool<UsesThisWalker>.Pooled Create(SyntaxNode node)
            {
                var pooled = Cache.GetOrCreate();
                pooled.Item.Visit(node);
                return pooled;
            }

            public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
            {
                this.CheckUsesThis(node.Left);
                base.VisitAssignmentExpression(node);
            }

            public override void VisitInvocationExpression(InvocationExpressionSyntax node)
            {
                this.CheckUsesThis(node.Expression);
                base.VisitInvocationExpression(node);
            }

            public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
            {
                if (!node.Modifiers.Any(SyntaxKind.StaticKeyword) &&
                     node.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    foreach (var variable in node.Declaration.Variables)
                    {
                        if (variable.Identifier.ValueText.StartsWith("_"))
                        {
                            this.useUnderScore = true;
                        }
                        else
                        {
                            this.usesThis = true;
                        }
                    }
                }

                base.VisitFieldDeclaration(node);
            }

            public override void VisitMemberAccessExpression(MemberAccessExpressionSyntax node)
            {
                this.CheckUsesThis(node);
                base.VisitMemberAccessExpression(node);
            }

            public override void VisitConditionalAccessExpression(ConditionalAccessExpressionSyntax node)
            {
                this.CheckUsesThis(node.Expression);
                base.VisitConditionalAccessExpression(node);
            }

            private void CheckUsesThis(ExpressionSyntax expression)
            {
                if (expression == null)
                {
                    return;
                }

                if ((expression as MemberAccessExpressionSyntax)?.Expression is ThisExpressionSyntax)
                {
                    this.usesThis = true;
                }

                if (expression is IdentifierNameSyntax)
                {
                    this.useUnderScore = true;
                }
            }
        }
    }
}
