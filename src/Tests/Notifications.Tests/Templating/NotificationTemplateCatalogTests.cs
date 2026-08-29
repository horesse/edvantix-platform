using System.Reflection;
using FSH.Modules.Notifications.Templating;
using Microsoft.Extensions.Logging.Abstractions;

namespace Notifications.Tests.Templating;

public sealed class NotificationTemplateCatalogTests
{
    private static readonly string[] AllTypeKeys = typeof(NotificationTypes)
        .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy)
        .Where(f => f is { IsLiteral: true, IsInitOnly: false } && f.FieldType == typeof(string))
        .Select(f => (string)f.GetRawConstantValue()!)
        .ToArray();

    [Fact]
    public void Every_NotificationType_Has_A_Template()
    {
        var catalog = new NotificationTemplateCatalog();

        foreach (var key in AllTypeKeys)
        {
            catalog.TryGetTemplate(key, out _).ShouldBeTrue($"missing template for '{key}'");
        }
    }

    [Fact]
    public void Catalog_Has_No_Template_Without_A_NotificationType()
    {
        foreach (var key in NotificationTemplateCatalog.Keys)
        {
            AllTypeKeys.ShouldContain(key);
        }
    }

    [Fact]
    public void Emailed_Templates_Have_Both_Subject_And_Body()
    {
        var catalog = new NotificationTemplateCatalog();

        foreach (var key in NotificationTemplateCatalog.Keys)
        {
            var template = catalog.GetTemplate(key);
            (template.EmailSubjectTemplate is null).ShouldBe(
                template.EmailHtmlBodyTemplate is null,
                $"'{key}' must set both e-mail fields or neither");
        }
    }

    [Fact]
    public void Every_Template_Renders_With_Placeholder_Tokens()
    {
        var catalog = new NotificationTemplateCatalog();
        var renderer = new NotificationTemplateRenderer(catalog, NullLogger<NotificationTemplateRenderer>.Instance);
        var permissive = new AlwaysTokens();

        foreach (var key in NotificationTemplateCatalog.Keys)
        {
            var rendered = renderer.Render(key, permissive);
            rendered.Title.ShouldNotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public void ChatMention_Link_Keeps_Its_Query_Shape()
    {
        var renderer = new NotificationTemplateRenderer(
            new NotificationTemplateCatalog(), NullLogger<NotificationTemplateRenderer>.Instance);

        var rendered = renderer.Render(NotificationTypes.ChatMention, new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["channel"] = "#general",
            ["preview"] = "hey @you",
            ["channelId"] = "c1",
            ["messageId"] = "m1",
        });

        rendered.Title.ShouldBe("You were mentioned in #general");
        rendered.Body.ShouldBe("hey @you");
        rendered.Link.ShouldBe("/chat/c1?messageId=m1");
    }

    /// <summary>Returns a value for any token asked for, so a template can be smoke-rendered.</summary>
    private sealed class AlwaysTokens : IReadOnlyDictionary<string, string?>
    {
        public string? this[string key] => "x";
        public IEnumerable<string> Keys => [];
        public IEnumerable<string?> Values => [];
        public int Count => 0;
        public bool ContainsKey(string key) => true;
        public bool TryGetValue(string key, out string? value) { value = "x"; return true; }
        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() =>
            Enumerable.Empty<KeyValuePair<string, string?>>().GetEnumerator();
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
