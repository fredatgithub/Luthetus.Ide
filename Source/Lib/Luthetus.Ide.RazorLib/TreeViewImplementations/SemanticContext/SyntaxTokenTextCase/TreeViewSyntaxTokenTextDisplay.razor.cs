using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Luthetus.Common.RazorLib.CustomEvents;
using Luthetus.Common.RazorLib.Icons;
using Luthetus.Common.RazorLib.Icons.Codicon;
using Luthetus.Ide.RazorLib.ContextCase;
using Luthetus.Ide.RazorLib.Button;
using Luthetus.Ide.ClassLib.CompilerServices.Common.BinderCase.BoundNodes.Statements;
using Luthetus.Ide.ClassLib.CompilerServices.Common.Syntax;

namespace Luthetus.Ide.RazorLib.TreeViewImplementations.SemanticContext.SyntaxTokenTextCase;

public partial class TreeViewSyntaxTokenTextDisplay : ComponentBase
{
    [Parameter, EditorRequired]
    public ISyntaxToken SyntaxToken { get; set; } = null!;
}