namespace Integration.Tests.Infrastructure;

public static class TestConstants
{
    public const string RootTenantId = "root";
    public const string RootAdminEmail = "admin@root.com";
    public const string DefaultPassword = "123Pa$$word!";

    public const string JwtIssuer = "fsh.local";
    public const string JwtAudience = "fsh.clients";
    public const string JwtSigningKey = "integration-test-signing-key-that-is-at-least-32-chars-long!!";

    public const string IdentityBasePath = "/api/v1/identity";
    public const string TenantsBasePath = "/api/v1/tenants";
    public const string AuditsBasePath = "/api/v1/audits";
    public const string WebhooksBasePath = "/api/v1/webhooks";
    public const string CatalogBasePath = "/api/v1/catalog";
    public const string TicketsBasePath = "/api/v1";

    /// <summary>People uses flat resource routing (no "/people" segment) — see
    /// docs/02 Модули/People.md and PeopleModule.MapEndpoints. Only /people/me/scope
    /// carries the "/people" segment.</summary>
    public const string PeopleBasePath = "/api/v1";

    /// <summary>Curriculum uses flat resource routing (no "/curriculum" segment), same
    /// convention as People — see docs/02 Модули/Curriculum.md and CurriculumModule.MapEndpoints.</summary>
    public const string CurriculumBasePath = "/api/v1";

    /// <summary>StudyGroups uses flat resource routing (no "/study-groups" segment prefix beyond
    /// the resource name itself), same convention as People/Curriculum — see
    /// docs/02 Модули/StudyGroups.md and StudyGroupsModule.MapEndpoints.</summary>
    public const string StudyGroupsBasePath = "/api/v1";

    /// <summary>Scheduling uses flat resource routing, same convention as People/Curriculum/
    /// StudyGroups — see docs/02 Модули/Scheduling.md and SchedulingModule.MapEndpoints.</summary>
    public const string SchedulingBasePath = "/api/v1";

    /// <summary>Payments uses flat resource routing, same convention as the other new-module
    /// APIs — see docs/02 Модули/Payments.md and PaymentsModule.MapEndpoints.</summary>
    public const string PaymentsBasePath = "/api/v1";
}
