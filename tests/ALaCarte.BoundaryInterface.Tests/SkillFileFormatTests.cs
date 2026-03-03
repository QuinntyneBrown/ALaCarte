using ALaCarte.Core.Commands;

namespace ALaCarte.BoundaryInterface.Tests;

public class SkillFileFormatTests
{
    [Fact]
    public void SkillContent_BeginsWithYamlFrontmatter()
    {
        // The file must begin with YAML frontmatter delimited by ---
        Assert.StartsWith("---", InstallSkillCommandHandler.SkillContent.TrimStart());
    }

    [Fact]
    public void SkillContent_FrontmatterContainsNameField()
    {
        var frontmatter = ExtractFrontmatter();
        Assert.Contains("name: a-la-carte", frontmatter);
    }

    [Fact]
    public void SkillContent_FrontmatterContainsDescription()
    {
        var frontmatter = ExtractFrontmatter();
        Assert.Contains("description:", frontmatter);
    }

    [Fact]
    public void SkillContent_FrontmatterContainsUserInvocableTrue()
    {
        var frontmatter = ExtractFrontmatter();
        Assert.Contains("user-invocable: true", frontmatter);
    }

    [Fact]
    public void SkillContent_BodyIsValidMarkdownWithHeadings()
    {
        var body = ExtractBody();
        // Must contain at least one Markdown heading
        Assert.Contains("# ", body);
    }

    [Fact]
    public void SkillContent_BodyDocumentsALaCarteApi()
    {
        var body = ExtractBody();
        // Must document the ALaCarte.Core public API
        Assert.Contains("ALaCarte", body);
    }

    [Fact]
    public void SkillContent_IsSelfContainedNoExternalUrls()
    {
        var content = InstallSkillCommandHandler.SkillContent;
        // Should not reference external URLs (http/https) for its core content
        // Code examples with URLs as string literals are acceptable
        var lines = content.Split('\n');
        foreach (var line in lines)
        {
            // Skip lines that are inside code blocks (example URLs in code are OK)
            if (line.TrimStart().StartsWith("```") || line.TrimStart().StartsWith("//"))
                continue;

            // Non-code lines should not have URLs
            if (!IsInsideCodeBlock(content, line))
            {
                Assert.DoesNotContain("http://", line);
                Assert.DoesNotContain("https://", line);
            }
        }
    }

    private static string ExtractFrontmatter()
    {
        var content = InstallSkillCommandHandler.SkillContent.TrimStart();
        var firstDelimiter = content.IndexOf("---");
        var secondDelimiter = content.IndexOf("---", firstDelimiter + 3);
        return content.Substring(firstDelimiter, secondDelimiter + 3 - firstDelimiter);
    }

    private static string ExtractBody()
    {
        var content = InstallSkillCommandHandler.SkillContent.TrimStart();
        var firstDelimiter = content.IndexOf("---");
        var secondDelimiter = content.IndexOf("---", firstDelimiter + 3);
        return content.Substring(secondDelimiter + 3).Trim();
    }

    private static bool IsInsideCodeBlock(string content, string line)
    {
        // Simple check: count ``` occurrences before this line
        var lineIndex = content.IndexOf(line);
        if (lineIndex < 0) return false;

        var beforeLine = content.Substring(0, lineIndex);
        var codeBlockCount = 0;
        var idx = 0;
        while ((idx = beforeLine.IndexOf("```", idx)) >= 0)
        {
            codeBlockCount++;
            idx += 3;
        }
        // If odd number of ``` before this line, we're inside a code block
        return codeBlockCount % 2 == 1;
    }
}
