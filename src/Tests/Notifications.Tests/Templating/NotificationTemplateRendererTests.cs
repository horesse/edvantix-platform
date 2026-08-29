using FSH.Modules.Notifications.Templating;
using Microsoft.Extensions.Logging.Abstractions;

namespace Notifications.Tests.Templating;

public sealed class NotificationTemplateRendererTests
{
    private static NotificationTemplateRenderer Renderer(params NotificationTemplate[] templates) =>
        new(new FakeCatalog(templates), NullLogger<NotificationTemplateRenderer>.Instance);

    private static Dictionary<string, string?> Tokens(params (string Key, string? Value)[] pairs) =>
        pairs.ToDictionary(p => p.Key, p => p.Value, StringComparer.Ordinal);

    [Fact]
    public void Render_Substitutes_Tokens_In_Every_Field()
    {
        var renderer = Renderer(new NotificationTemplate(
            "t",
            TitleTemplate: "Hi {{name}}",
            BodyTemplate: "Body {{name}} / {{place}}",
            LinkTemplate: "/x/{{id}}",
            EmailSubjectTemplate: "Subj {{name}}",
            EmailHtmlBodyTemplate: "<p>{{name}}</p>"));

        var result = renderer.Render("t", Tokens(("name", "Ann"), ("place", "Room 2"), ("id", "42")));

        result.Title.ShouldBe("Hi Ann");
        result.Body.ShouldBe("Body Ann / Room 2");
        result.Link.ShouldBe("/x/42");
        result.EmailSubject.ShouldBe("Subj Ann");
        result.EmailHtmlBody.ShouldBe("<p>Ann</p>");
        result.HasEmail.ShouldBeTrue();
    }

    [Fact]
    public void Render_Tolerates_Whitespace_Inside_Braces()
    {
        var renderer = Renderer(new NotificationTemplate("t", TitleTemplate: "{{  name  }}"));

        renderer.Render("t", Tokens(("name", "Bob"))).Title.ShouldBe("Bob");
    }

    [Fact]
    public void Render_Missing_Token_Becomes_Empty_And_Does_Not_Throw()
    {
        var renderer = Renderer(new NotificationTemplate("t", TitleTemplate: "A{{gap}}B"));

        renderer.Render("t", Tokens()).Title.ShouldBe("AB");
    }

    [Fact]
    public void Render_Null_Token_Value_Becomes_Empty()
    {
        var renderer = Renderer(new NotificationTemplate("t", TitleTemplate: "A{{gap}}B"));

        renderer.Render("t", Tokens(("gap", null))).Title.ShouldBe("AB");
    }

    [Fact]
    public void Render_Escapes_Values_Only_In_Email_Html_Body()
    {
        var renderer = Renderer(new NotificationTemplate(
            "t",
            TitleTemplate: "{{v}}",
            BodyTemplate: "{{v}}",
            EmailSubjectTemplate: "{{v}}",
            EmailHtmlBodyTemplate: "<p>{{v}}</p>"));

        var result = renderer.Render("t", Tokens(("v", "<b>&\"'")));

        result.Title.ShouldBe("<b>&\"'");
        result.Body.ShouldBe("<b>&\"'");
        result.EmailSubject.ShouldBe("<b>&\"'");
        result.EmailHtmlBody.ShouldBe("<p>&lt;b&gt;&amp;&quot;&#39;</p>");
    }

    [Fact]
    public void Render_Unknown_Template_Key_Throws()
    {
        Should.Throw<KeyNotFoundException>(() => Renderer().Render("missing", Tokens()));
    }

    [Fact]
    public void Render_Without_Email_Fields_Reports_No_Email()
    {
        var renderer = Renderer(new NotificationTemplate("t", TitleTemplate: "x"));

        var result = renderer.Render("t", Tokens());

        result.HasEmail.ShouldBeFalse();
        result.EmailSubject.ShouldBeNull();
        result.EmailHtmlBody.ShouldBeNull();
    }

    private sealed class FakeCatalog(IEnumerable<NotificationTemplate> templates) : INotificationTemplateCatalog
    {
        private readonly Dictionary<string, NotificationTemplate> _templates =
            templates.ToDictionary(t => t.Key, StringComparer.Ordinal);

        public NotificationTemplate GetTemplate(string key) =>
            _templates.TryGetValue(key, out var t) ? t : throw new KeyNotFoundException(key);

        public bool TryGetTemplate(string key, out NotificationTemplate result)
        {
            var found = _templates.TryGetValue(key, out var t);
            result = t!;
            return found;
        }
    }
}
