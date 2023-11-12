﻿using Luthetus.Common.RazorLib.FileSystems.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Luthetus.Common.Tests.Basis.FileSystems.Models;

/// <summary>
/// <see cref="PathHelper"/>
/// </summary>
public class PathHelperTests
{
    /// <summary>
    /// <see cref="PathHelper.GetAbsoluteFromAbsoluteAndRelative(IAbsolutePath, string, IEnvironmentProvider)"/>
    /// ---------------------------------------------<br/>
    /// Root<br/>
    /// ∙└───Homework<br/>
    /// ∙∙∙∙∙∙├───Math<br/>
    /// ∙∙∙∙∙∙│∙∙∙∙├───addition.txt<br/>
    /// ∙∙∙∙∙∙│∙∙∙∙└───subtraction.txt<br/>
    /// ∙∙∙∙∙∙│<br/>
    /// ∙∙∙∙∙∙└───Biology<br/>
    /// ∙∙∙∙∙∙∙∙∙∙∙├───nervousSystem.txt<br/>
    /// ∙∙∙∙∙∙∙∙∙∙∙└───skeletalSystem.txt<br/>
    /// </summary>
    [Fact]
    public void GetAbsoluteFromAbsoluteAndRelative()
    {
        FileSystemsTestsHelper.InitializeFileSystemsTests(
            out InMemoryEnvironmentProvider environmentProvider,
            out InMemoryFileSystemProvider fileSystemProvider,
            out ServiceProvider serviceProvider);

        // UpDir directive performs differently when starting on a file vs a directory
        {
            // If one starts on a directory, then the immediate parent is the answer.
            //
            // BUT if one starts on a file, the file's parent is the upper directory.
            // Instead it would be one ancestor further.
        }

        // File to file with UpDir directives.
        {
            var startPathString = "/Homework/Biology/nervousSystem.txt";
            var relativePathString = "../Math/addition.txt";
            var expectedOutputPathString = "/Homework/Math/addition.txt";

            var startAbsolutePath = new AbsolutePath(startPathString, false, environmentProvider);

            var outputAbsolutePathString = PathHelper.GetAbsoluteFromAbsoluteAndRelative(
                startAbsolutePath,
                relativePathString,
                environmentProvider);

            Assert.Equal(expectedOutputPathString, outputAbsolutePathString);
        }

        // File to file NOT-USING any UpDir directives.
        {
            var startPathString = "/Homework/Biology/nervousSystem.txt";
            var relativePathString = "./skeletalSystem.txt";
            var expectedOutputPathString = "/Homework/Biology/skeletalSystem.txt";

            var startAbsolutePath = new AbsolutePath(startPathString, false, environmentProvider);

            var outputAbsolutePathString = PathHelper.GetAbsoluteFromAbsoluteAndRelative(
                startAbsolutePath,
                relativePathString,
                environmentProvider);

            Assert.Equal(expectedOutputPathString, outputAbsolutePathString);
        }

        // A single UpDir directive from a file.
        {
            var startPathString = "/Homework/Biology/nervousSystem.txt";
            var relativePathString = "../";
            var expectedOutputPathString = "/Homework/";

            var startAbsolutePath = new AbsolutePath(startPathString, false, environmentProvider);

            var outputAbsolutePathString = PathHelper.GetAbsoluteFromAbsoluteAndRelative(
                startAbsolutePath,
                relativePathString,
                environmentProvider);

            Assert.Equal(expectedOutputPathString, outputAbsolutePathString);
        }

        // A single UpDir directive from a directory.
        {
            var startPathString = "/Homework/Biology/";
            var relativePathString = "../";
            var expectedOutputPathString = "/Homework/";

            var startAbsolutePath = new AbsolutePath(startPathString, false, environmentProvider);

            var outputAbsolutePathString = PathHelper.GetAbsoluteFromAbsoluteAndRelative(
                startAbsolutePath,
                relativePathString,
                environmentProvider);

            Assert.Equal(expectedOutputPathString, outputAbsolutePathString);
        }
    }

    /// <summary>
    /// <see cref="PathHelper.GetRelativeFromTwoAbsolutes(IAbsolutePath, IAbsolutePath, IEnvironmentProvider)"/>
    /// </summary>
    [Fact]
    public void GetRelativeFromTwoAbsolutes()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// <see cref="PathHelper.CalculateNameWithExtension(string, string, bool)"/>
    /// </summary>
    [Fact]
    public void CalculateNameWithExtension()
    {
        throw new NotImplementedException();
    }
}