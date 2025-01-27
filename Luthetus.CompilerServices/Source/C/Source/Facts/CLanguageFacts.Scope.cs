﻿// TODO: All C logic is commented out due to breaking changes in the TextEditor API. The...
// ...C# compiler service is the focus while API decisions are being made, lest twice the code...
// ...need be modified for every API change (2023-10-04)
//
//using Luthetus.TextEditor.RazorLib.CompilerServiceCase.Syntax;
//using Luthetus.TextEditor.RazorLib.CompilerServiceCase.Syntax.SyntaxNodes;
//using Luthetus.TextEditor.RazorLib.CompilerServiceCase.Syntax.SyntaxTokens;
//using Luthetus.TextEditor.RazorLib.Lexing;

//namespace Luthetus.CompilerServices.Lang.C.Facts;

//public partial class CLanguageFacts
//{
//    public class Scope
//    {
//        public static BoundScope GetInitialGlobalScope()
//        {
//            var classMap = new Dictionary<string, TypeDefinitionNode>
//        {
//            {
//                Types.Int.TypeIdentifier.TextSpan.GetText(),
//                Types.Int
//            },
//            {
//                Types.String.TypeIdentifier.TextSpan.GetText(),
//                Types.String
//            },
//            {
//                Types.Void.TypeIdentifier.TextSpan.GetText(),
//                Types.Void
//            }
//        };

//            return new BoundScope(
//                null,
//                new TypeClauseNode(new IdentifierToken(Types.Int.TypeIdentifier.TextSpan), null),
//                0,
//                null,
//                new ResourceUri(string.Empty),
//                classMap,
//                new(),
//                new());
//        }
//    }
//}