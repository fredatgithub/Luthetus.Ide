﻿namespace Luthetus.Ide.ClassLib.CommandCase;

public class Command : ICommand
{
    public Command(
        Func<Task> doAsyncFunc,
        string displayName,
        string internalIdentifier,
        bool shouldBubble)
    {
        DoAsyncFunc = doAsyncFunc;
        DisplayName = displayName;
        InternalIdentifier = internalIdentifier;
        ShouldBubble = shouldBubble;
    }

    public Func<Task> DoAsyncFunc { get; }
    public string DisplayName { get; }
    public string InternalIdentifier { get; }
    /// <summary>
    /// <see cref="ShouldBubble"/> is false by default
    /// </summary>
    public bool ShouldBubble { get; }
}