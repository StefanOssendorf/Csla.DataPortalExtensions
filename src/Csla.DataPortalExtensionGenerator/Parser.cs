using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Ossendorf.Csla.DataPortalExtensionGenerator.Diagnostics;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;
internal static class Parser {
    #region Extension class

    public static Result<ClassForExtensions> GetExtensionClass(GeneratorAttributeSyntaxContext ctx, CancellationToken ct) {
        _ = ct;

        var hasPartialModifier = false;
        var classSyntax = (ClassDeclarationSyntax)ctx.TargetNode;
        for (var i = 0; i < classSyntax.Modifiers.Count; i++) {
            if (classSyntax.Modifiers[i].IsKind(SyntaxKind.PartialKeyword)) {
                hasPartialModifier = true;
                break;
            }
        }

        EquatableArray<DiagnosticInfo> errors;
        if (!hasPartialModifier) {
            errors = new EquatableArray<DiagnosticInfo>([NotPartialDiagnostic.Create(classSyntax)]);
        } else {
            errors = default;
        }

        var classSymbol = (INamedTypeSymbol)ctx.TargetSymbol;
        var @namespace = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : classSymbol.ContainingNamespace.ToString();
        var name = classSymbol.Name;

        return new Result<ClassForExtensions>(new ClassForExtensions(name, @namespace, hasPartialModifier), errors);
    }

    #endregion

    #region Csla methods

    public static bool CouldBeCslaDataPortalAttribute(SyntaxNode node, CancellationToken _) {

        if (node is not AttributeSyntax attribute) {
            return false;
        }

        var name = GeneratorHelper.ExtractAttributeName(attribute.Name);

        return name is not null && GeneratorHelper.RecognizedCslaDataPortalAttributes.Keys.Contains(name);
    }

    #endregion
}
