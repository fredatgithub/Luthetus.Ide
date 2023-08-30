﻿namespace Luthetus.Ide.ClassLib.WebsiteProjectTemplates.BlazorServerEmptyCase;

public static partial class BlazorServerEmptyFacts
{
    public const string MAIN_LAYOUT_RAZOR_RELATIVE_FILE_PATH = @"MainLayout.razor";

    public static string GetMainLayoutRazorContents(string projectName) => @$"@inherits LayoutComponentBase

<main> @Body </main>
";
}
