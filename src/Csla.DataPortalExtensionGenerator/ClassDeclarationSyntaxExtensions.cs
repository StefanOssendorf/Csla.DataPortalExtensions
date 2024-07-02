using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Runtime.CompilerServices;

namespace Ossendorf.Csla.DataPortalExtensionGenerator;
internal static class ClassDeclarationSyntaxExtensions {
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool HasModifier(this ClassDeclarationSyntax source, SyntaxKind modifier) {
        for (var i = 0; i < source.Modifiers.Count; i++) {
            if (source.Modifiers[i].IsKind(modifier)) {
                return true;
            }
        }

        return false;
    }
}
