using System.Globalization;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Constants;
using FSH.Framework.Shared.Identity.Claims;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Billing.Contracts;
using FSH.Modules.Billing.Data;
using FSH.Modules.Billing.Domain;
using FSH.Modules.Chat.Data;
using FSH.Modules.Chat.Domain;
using FSH.Modules.Curriculum.Contracts.Authorization;
using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using FSH.Modules.Identity.Contracts.Authorization;
using FSH.Modules.Identity.Data;
using FSH.Modules.Identity.Domain;
using FSH.Modules.Multitenancy.Contracts;
using FSH.Modules.Multitenancy.Data;
using FSH.Modules.Multitenancy.Provisioning;
using FSH.Modules.Payments.Contracts.Dtos;
using FSH.Modules.Payments.Data;
using FSH.Modules.Payments.Domain;
using FSH.Modules.People.Contracts.Authorization;
using FSH.Modules.People.Contracts.Dtos;
using FSH.Modules.People.Data;
using FSH.Modules.People.Domain;
using FSH.Modules.Scheduling.Contracts.Dtos;
using FSH.Modules.Scheduling.Data;
using FSH.Modules.Scheduling.Domain;
using FSH.Modules.StudyGroups.Contracts.Authorization;
using FSH.Modules.StudyGroups.Contracts.Dtos;
using FSH.Modules.StudyGroups.Data;
using FSH.Modules.StudyGroups.Domain;
using FSH.Modules.Tickets.Contracts.Authorization;
using FSH.Modules.Tickets.Contracts.Dtos;
using FSH.Modules.Tickets.Data;
using FSH.Modules.Tickets.Domain;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Edvantix.DbMigrator.DemoSeed;

/// <summary>
/// Owns the "rich demo content" that the dev environment needs to feel lived-in:
/// the <c>acme</c> and <c>globex</c> tenants, their demo users, custom roles,
/// a school (courses, teachers, students, study groups, schedule, invoices),
/// tickets, and chat. Invoked by the migrator's <c>seed-demo</c> verb — never
/// by the API runtime.
///
/// Idempotent: every step checks before writing, so re-running the verb
/// against an already-seeded database is a no-op.
///
/// Naming: pre-2026-05-17 this lived in the API as <c>DevDataSeeder</c>
/// (a hosted service) — moved here so the API no longer mutates data on
/// startup, matching the same principle that pulled migrations out into
/// this project. See <c>docs/superpowers/specs/2026-05-14-remove-api-auto-migration-design.md</c>.
/// </summary>
internal sealed class DemoSeeder
{
    private readonly IServiceProvider _services;
    private readonly IConfiguration _config;
    private readonly ILogger<DemoSeeder> _logger;
    private string _sharedPassword = string.Empty;

    public static readonly DemoTenant Acme = new(
        Id: "acme",
        Name: "Acme Corp",
        AdminEmail: "admin@acme.com",
        Issuer: "fsh.demo.acme",
        PlanKey: "pro-annual");

    public static readonly DemoTenant Globex = new(
        Id: "globex",
        Name: "Globex",
        AdminEmail: "admin@globex.com",
        Issuer: "fsh.demo.globex",
        PlanKey: "free");

    public DemoSeeder(IServiceProvider services, IConfiguration config, ILogger<DemoSeeder> logger)
    {
        _services = services;
        _config = config;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken)
    {
        // Sourced from configuration so the demo credential isn't hard-coded.
        _sharedPassword = _config["Seed:DemoPassword"]
            ?? throw new InvalidOperationException(
                "Seed:DemoPassword must be configured (see appsettings.Development.json).");

        await EnsureDemoTenantsExistAsync(cancellationToken).ConfigureAwait(false);
        await SeedRootSuperAdminAsync(cancellationToken).ConfigureAwait(false);

        foreach (var demo in new[] { Acme, Globex })
        {
            await SeedTenantSubscriptionAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantUsersAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantSchoolAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantTicketsAsync(demo, cancellationToken).ConfigureAwait(false);
            await SeedTenantChatAsync(demo, cancellationToken).ConfigureAwait(false);
        }

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[demo-seed] complete · root superadmin + {Acme} + {Globex} populated with users / school / tickets / chat",
                Acme.Id, Globex.Id);
        }
    }

    // ─── Tenant provisioning ────────────────────────────────────────────

    /// <summary>
    /// Adds the demo tenants to the catalog if missing, then walks them through
    /// the same <see cref="ITenantService"/> migrate + seed path the runtime
    /// uses. The provisioning service inside the migrator falls back to inline
    /// execution because Hangfire isn't running here — we get a synchronous
    /// "tenant is ready" before this method returns.
    /// </summary>
    private async Task EnsureDemoTenantsExistAsync(CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenantService = scope.ServiceProvider.GetRequiredService<ITenantService>();
        var tenantDb = scope.ServiceProvider.GetRequiredService<TenantDbContext>();

        foreach (var demo in new[] { Acme, Globex })
        {
            var existing = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
            if (existing is null)
            {
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("[demo-seed] creating tenant '{TenantId}'", demo.Id);
                }
                var tenant = new AppTenantInfo(demo.Id, demo.Name, connectionString: string.Empty, demo.AdminEmail, demo.Issuer);
                tenant.SetValidity(DateTime.UtcNow.AddYears(1));
                await tenantDb.TenantInfo.AddAsync(tenant, cancellationToken).ConfigureAwait(false);
                await tenantDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
                existing = tenant;
            }

            // Same per-tenant path the migrator's apply verb uses. The Identity initializer creates
            // the tenant admin, while the school modules'/Tickets/Chat initializers are no-ops today.
            await tenantService.MigrateTenantAsync(existing, cancellationToken).ConfigureAwait(false);
            await tenantService.SeedTenantAsync(existing, cancellationToken).ConfigureAwait(false);

            await EnsureProvisioningRecordAsync(tenantDb, demo.Id, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Demo tenants are migrated + seeded inline above, bypassing the provisioning
    /// pipeline — so no <see cref="TenantProvisioning"/> row exists and the admin
    /// Provisioning panel would 404. Record a completed run (all steps done) so the
    /// panel shows a real "Completed" history instead. Idempotent: skips if a row
    /// already exists for the tenant.
    /// </summary>
    private static async Task EnsureProvisioningRecordAsync(TenantDbContext tenantDb, string tenantId, CancellationToken cancellationToken)
    {
        var alreadyTracked = await tenantDb.Set<TenantProvisioning>()
            .AnyAsync(p => p.TenantId == tenantId, cancellationToken)
            .ConfigureAwait(false);
        if (alreadyTracked)
        {
            return;
        }

        var provisioning = new TenantProvisioning(tenantId, Guid.NewGuid().ToString());
        foreach (var step in Enum.GetValues<TenantProvisioningStepName>())
        {
            var stepEntity = new TenantProvisioningStep(provisioning.Id, step);
            stepEntity.MarkRunning();
            stepEntity.MarkCompleted();
            provisioning.Steps.Add(stepEntity);
        }
        provisioning.MarkCompleted();

        tenantDb.Add(provisioning);
        await tenantDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ─── Subscription ──────────────────────────────────────────────────

    /// <summary>
    /// Attaches an active billing <see cref="Subscription"/> to the demo tenant so the dashboard's
    /// PLAN / subscription cards are populated out of the box. The real tenant-create path drives this
    /// via <c>TenantSubscribedIntegrationEvent</c>, but demo tenants are provisioned inline (see
    /// <see cref="EnsureDemoTenantsExistAsync"/>) and never publish it — so we write the row directly.
    ///
    /// Paid plans also get an issued term invoice, matching the real flow. It's written directly
    /// rather than via <c>IBillingService</c> so we don't publish <c>InvoiceIssuedIntegrationEvent</c>
    /// — the one-shot migrator has no outbox dispatcher and demo seeding shouldn't fire
    /// notifications/emails. The subscription's term is aligned to the tenant's <c>ValidUpto</c> so the
    /// dashboard's term matches the enforced validity window.
    ///
    /// Idempotent: skips when the tenant already has an active subscription.
    /// </summary>
    private async Task SeedTenantSubscriptionAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        // BillingDbContext is NOT tenant-filtered (TenantId is an explicit column), so no Finbuckle
        // context juggling is required — we scope by TenantId directly.
        var billingDb = scope.ServiceProvider.GetRequiredService<BillingDbContext>();

        var plan = await billingDb.Plans
            .FirstOrDefaultAsync(p => p.Key == demo.PlanKey && p.IsActive, cancellationToken)
            .ConfigureAwait(false);
        if (plan is null)
        {
            if (_logger.IsEnabled(LogLevel.Warning))
            {
                _logger.LogWarning(
                    "[demo-seed] [{Tenant}] plan '{PlanKey}' not found — skipping subscription", demo.Id, demo.PlanKey);
            }
            return;
        }

        // Reuse the existing active subscription's term if present so re-runs don't re-subscribe but
        // still backfill a missing invoice, otherwise start fresh aligned to the tenant's ValidUpto.
        var existing = await billingDb.Subscriptions
            .FirstOrDefaultAsync(s => s.TenantId == demo.Id && s.Status == SubscriptionStatus.Active, cancellationToken)
            .ConfigureAwait(false);

        var startUtc = existing?.StartUtc ?? DateTime.UtcNow;
        var endUtc = existing?.EndUtc ?? DateTime.SpecifyKind(tenant.ValidUpto, DateTimeKind.Utc);

        if (existing is null)
        {
            billingDb.Subscriptions.Add(Subscription.Create(demo.Id, plan.Id, startUtc, endUtc));
            if (_logger.IsEnabled(LogLevel.Information))
            {
                _logger.LogInformation(
                    "[demo-seed] [{Tenant}] subscribed to plan '{PlanKey}' (term ends {End:o})",
                    demo.Id, plan.Key, endUtc);
            }
        }

        // Paid plans get an issued term invoice (like real CreateTenant), written directly so no InvoiceIssuedIntegrationEvent fires
        // (no outbox dispatcher; seeding mustn't email). Idempotent on invoice number; free plans (term price 0) get none, as in production.
        if (plan.TermPrice > 0m)
        {
            var invoiceNumber = string.Create(
                CultureInfo.InvariantCulture, $"SUB-{startUtc:yyyyMM}-{demo.Id.ToUpperInvariant()}");
            var invoiceExists = await billingDb.Invoices
                .AnyAsync(i => i.TenantId == demo.Id && i.InvoiceNumber == invoiceNumber, cancellationToken)
                .ConfigureAwait(false);
            if (!invoiceExists)
            {
                var invoice = Invoice.CreateDraft(
                    demo.Id, invoiceNumber, startUtc.Year, startUtc.Month, plan.Currency,
                    InvoicePurpose.Subscription, startUtc, endUtc);
                invoice.AddLineItem(
                    InvoiceLineItemKind.BaseFee,
                    string.Create(
                        CultureInfo.InvariantCulture,
                        $"{plan.Name} — {plan.Interval} subscription ({startUtc:yyyy-MM-dd} to {endUtc:yyyy-MM-dd})"),
                    1m,
                    plan.TermPrice);
                invoice.Issue();
                billingDb.Invoices.Add(invoice);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation(
                        "[demo-seed] [{Tenant}] issued term invoice {InvoiceNumber} ({Amount} {Currency})",
                        demo.Id, invoiceNumber, plan.TermPrice, plan.Currency);
                }
            }
        }

        await billingDb.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ─── Users + roles ─────────────────────────────────────────────────

    private async Task SeedRootSuperAdminAsync(CancellationToken cancellationToken)
    {
        var rootTenant = new AppTenantInfo(
            id: MultitenancyConstants.Root.Id,
            name: MultitenancyConstants.Root.Name,
            connectionString: string.Empty,
            adminEmail: MultitenancyConstants.Root.EmailAddress,
            issuer: MultitenancyConstants.Root.Issuer);

        await SeedUsersInTenantAsync(rootTenant, BuildRootUsers(), [], cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedTenantUsersAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        var users = demo.Id == Acme.Id ? BuildAcmeUsers() : BuildGlobexUsers();
        var customRoles = demo.Id == Acme.Id ? BuildAcmeCustomRoles() : Array.Empty<DemoRole>();
        await SeedUsersInTenantAsync(tenant, users, customRoles, cancellationToken).ConfigureAwait(false);
    }

    private async Task SeedUsersInTenantAsync(
        AppTenantInfo tenant,
        IReadOnlyList<DemoUser> users,
        IReadOnlyList<DemoRole> customRoles,
        CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<FshRole>>();
        var context = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var hasher = new PasswordHasher<FshUser>();

        foreach (var demoRole in customRoles)
        {
            var role = await roleManager.FindByNameAsync(demoRole.Name).ConfigureAwait(false);
            if (role is null)
            {
                role = new FshRole(demoRole.Name, demoRole.Description);
                await roleManager.CreateAsync(role).ConfigureAwait(false);
                if (_logger.IsEnabled(LogLevel.Information))
                {
                    _logger.LogInformation("[demo-seed] [{Tenant}] created custom role '{Role}'", tenant.Id, demoRole.Name);
                }
            }

            var existingClaims = await roleManager.GetClaimsAsync(role).ConfigureAwait(false);
            foreach (var permission in demoRole.Permissions)
            {
                if (existingClaims.Any(c => c.Type == ClaimConstants.Permission && c.Value == permission))
                {
                    continue;
                }
                context.RoleClaims.Add(new FshRoleClaim
                {
                    RoleId = role.Id,
                    ClaimType = ClaimConstants.Permission,
                    ClaimValue = permission,
                    CreatedBy = "DemoSeeder",
                    CreatedOn = DateTimeOffset.UtcNow,
                });
            }
            await context.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        foreach (var demoUser in users)
        {
            var existing = await userManager.FindByEmailAsync(demoUser.Email).ConfigureAwait(false);
            if (existing is null)
            {
                var user = new FshUser
                {
                    UserName = demoUser.UserName,
                    Email = demoUser.Email,
                    EmailConfirmed = true,
                    FirstName = demoUser.FirstName,
                    LastName = demoUser.LastName,
                    IsActive = true,
                    NormalizedEmail = demoUser.Email.ToUpperInvariant(),
                    NormalizedUserName = demoUser.UserName.ToUpperInvariant(),
                };
                user.PasswordHash = hasher.HashPassword(user, _sharedPassword);
                var created = await userManager.CreateAsync(user).ConfigureAwait(false);
                if (!created.Succeeded)
                {
                    if (_logger.IsEnabled(LogLevel.Warning))
                    {
                        _logger.LogWarning(
                            "[demo-seed] [{Tenant}] failed to create '{Email}': {Errors}",
                            tenant.Id, demoUser.Email,
                            string.Join("; ", created.Errors.Select(e => e.Description)));
                    }
                    continue;
                }
                existing = user;
            }
            else
            {
                await EnsureSharedPasswordAsync(userManager, hasher, existing).ConfigureAwait(false);
            }

            foreach (var role in demoUser.Roles)
            {
                if (!await userManager.IsInRoleAsync(existing, role).ConfigureAwait(false))
                {
                    var roleEntity = await roleManager.FindByNameAsync(role).ConfigureAwait(false);
                    if (roleEntity is null) continue;
                    await userManager.AddToRoleAsync(existing, role).ConfigureAwait(false);
                }
            }
        }

        // Tenant admin (admin@<tenant>.com) was created by IdentityDbInitializer with the framework default password.
        // Realign it to the shared password so the dev login panel's advertised credential is truthful.
        if (!string.IsNullOrWhiteSpace(tenant.AdminEmail))
        {
            var admin = await userManager.FindByEmailAsync(tenant.AdminEmail).ConfigureAwait(false);
            if (admin is not null)
            {
                await EnsureSharedPasswordAsync(userManager, hasher, admin).ConfigureAwait(false);
            }
        }
    }

    private async Task EnsureSharedPasswordAsync(
        UserManager<FshUser> userManager,
        PasswordHasher<FshUser> hasher,
        FshUser user)
    {
        if (await userManager.CheckPasswordAsync(user, _sharedPassword).ConfigureAwait(false))
        {
            return;
        }
        user.PasswordHash = hasher.HashPassword(user, _sharedPassword);
        var result = await userManager.UpdateAsync(user).ConfigureAwait(false);
        if (!result.Succeeded && _logger.IsEnabled(LogLevel.Warning))
        {
            _logger.LogWarning(
                "[demo-seed] failed to reset password for '{Email}': {Errors}",
                user.Email,
                string.Join("; ", result.Errors.Select(e => e.Description)));
        }
    }

    // ─── School (Curriculum / People / StudyGroups / Scheduling / Payments) ──────────

    /// <summary>
    /// Idempotently seeds a lived-in school into the demo tenant. Acme (the rich, paid-plan
    /// tenant) gets the full footprint: 3 courses (with sections and lessons), 4 teachers,
    /// 30 students each linked to a guardian, 5 study groups (6 students each), a month of
    /// scheduled sessions with attendance for the ones already held, and issued tuition
    /// invoices for the current period. Globex (the minimal free-plan tenant — same asymmetry
    /// as its users/tickets/chat elsewhere in this class) gets a single course/group instead.
    /// Finbuckle's <c>IsMultiTenant()</c> (see <c>BaseDbContext.OnModelCreating</c>) folds
    /// TenantId into every unique index it manages, so Acme and Globex could safely reuse the
    /// same names — the split here is purely to keep Globex's footprint minimal, not to dodge
    /// a collision. Replaces the old product-catalog demo data — see docs/05 Решения (ADR)/
    /// ADR-002 Catalog заменяется на Curriculum.md and docs/04 Задачи/Открытые вопросы.md →
    /// «Демо-данные». Bails per sub-step when that step's rows already exist for the tenant.
    /// </summary>
    private async Task SeedTenantSchoolAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        // Records created here are attributed to the tenant admin (ManagerUserId) — the
        // Identity initializer guarantees this user exists before any per-tenant seed runs.
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var manager = string.IsNullOrWhiteSpace(tenant.AdminEmail)
            ? null
            : await userManager.FindByEmailAsync(tenant.AdminEmail).ConfigureAwait(false);
        if (manager is null) return;

        bool isAcme = demo.Id == Acme.Id;

        var courses = await SeedTenantCurriculumAsync(scope.ServiceProvider, isAcme, cancellationToken).ConfigureAwait(false);
        var (teachers, students, primaryGuardianByStudent) = await SeedTenantPeopleAsync(
            scope.ServiceProvider, manager.Id, isAcme, cancellationToken).ConfigureAwait(false);
        if (courses.Count == 0 || teachers.Count == 0 || students.Count == 0) return;

        var groups = await SeedTenantStudyGroupsAsync(
            scope.ServiceProvider, courses, teachers, students, isAcme, cancellationToken).ConfigureAwait(false);
        await SeedTenantSchedulingAsync(scope.ServiceProvider, groups, cancellationToken).ConfigureAwait(false);
        await SeedTenantPaymentsAsync(
            scope.ServiceProvider, courses, groups, primaryGuardianByStudent, cancellationToken).ConfigureAwait(false);

        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[demo-seed] [{Tenant}] seeded {CourseCount} courses, {TeacherCount} teachers, " +
                "{StudentCount} students, {GroupCount} study groups",
                tenant.Id, courses.Count, teachers.Count, students.Count, groups.Count);
        }
    }

    /// <summary>3 published courses for Acme (one per subject, each with 2 sections of 2
    /// lessons); 1 for Globex. Reuses a subject <c>CurriculumDbInitializer.SeedAsync</c> already
    /// created for this tenant (it seeds "Английский язык" for every new school) by name instead
    /// of inserting a second row — that class's own <c>Subject.Slug</c> uniqueness would reject
    /// the duplicate.</summary>
    private static async Task<IReadOnlyList<Course>> SeedTenantCurriculumAsync(
        IServiceProvider services, bool isAcme, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<CurriculumDbContext>();

        if (!await dbContext.Courses.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var courseSpecs = isAcme
                ? new (string SubjectName, string Title, CourseLevel Level, int Hours)[]
                  {
                      ("Английский язык", "Английский язык — групповой курс", CourseLevel.Intermediate, 64),
                      ("Немецкий язык", "Немецкий язык — групповой курс", CourseLevel.Beginner, 48),
                      ("Испанский язык", "Испанский язык — групповой курс", CourseLevel.Beginner, 48),
                  }
                : [("Французский язык", "Французский язык — групповой курс", CourseLevel.Beginner, 40)];

            var existingSubjectsByName = await dbContext.Subjects
                .ToDictionaryAsync(s => s.Name, cancellationToken).ConfigureAwait(false);
            int nextSortOrder = existingSubjectsByName.Count;

            Subject GetOrCreateSubject(string name)
            {
                if (existingSubjectsByName.TryGetValue(name, out var existing))
                {
                    return existing;
                }
                var created = Subject.Create(name, null, nextSortOrder++);
                dbContext.Subjects.Add(created);
                existingSubjectsByName[name] = created;
                return created;
            }

            var subjects = courseSpecs.Select(spec => GetOrCreateSubject(spec.SubjectName)).ToList();

            foreach (var ((_, title, level, hours), subject) in courseSpecs.Zip(subjects))
            {
                var course = Course.Create(
                    subject.Id, title, "Демо-курс, сгенерирован seed-demo.", level, hours, coverFileId: null);
                dbContext.Courses.Add(course);

                for (int m = 0; m < 2; m++)
                {
                    var module = CourseModule.Create(course.Id, $"Раздел {m + 1}", null, m);
                    dbContext.CourseModules.Add(module);

                    for (int l = 0; l < 2; l++)
                    {
                        dbContext.Lessons.Add(Lesson.Create(module.Id, $"Урок {l + 1}", null, null, 60, l));
                    }
                }

                course.Publish();
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await dbContext.Courses.OrderBy(c => c.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Acme: 4 teachers and 30 students. Globex: 1 teacher and 6 students (enough for
    /// its single study group). Each student is linked to one guardian marked as primary payer.
    /// Teacher/student emails are reused across tenants on purpose — People has no cross-tenant
    /// uniqueness on Email, so this is safe (unlike Curriculum/StudyGroups' slugs/codes).</summary>
    private static async Task<(
        IReadOnlyList<Teacher> Teachers,
        IReadOnlyList<Student> Students,
        IReadOnlyDictionary<Guid, Guid> PrimaryGuardianByStudent)> SeedTenantPeopleAsync(
        IServiceProvider services, string managerUserId, bool isAcme, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<PeopleDbContext>();

        if (!await dbContext.Students.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var teacherSpecs = isAcme
                ? new (string Last, string First, string[] Specializations)[]
                  {
                      ("Волкова", "Мария", ["Английский"]),
                      ("Соколов", "Дмитрий", ["Немецкий"]),
                      ("Морозова", "Анна", ["Испанский"]),
                      ("Кузнецов", "Иван", ["Английский", "Испанский"]),
                  }
                : [("Дюбуа", "Клэр", ["Французский"])];
            for (int i = 0; i < teacherSpecs.Length; i++)
            {
                var (last, first, specializations) = teacherSpecs[i];
                dbContext.Teachers.Add(Teacher.Create(
                    last, first, null,
                    phone: $"+7 900 000-00-{10 + i:00}",
                    email: $"teacher{i + 1}@demo.local",
                    bio: null,
                    specializations: specializations,
                    hourlyRate: 1500m));
            }

            string[] firstNames =
            [
                "Александр", "Мария", "Дмитрий", "Елена", "Иван", "Ольга", "Сергей", "Наталья", "Андрей", "Татьяна",
                "Максим", "Юлия", "Артём", "Виктория", "Кирилл", "Полина", "Никита", "Дарья", "Роман", "Ксения",
                "Егор", "Софья", "Владимир", "Алиса", "Павел", "Вероника", "Тимофей", "Милана", "Глеб", "Есения",
            ];
            string[] lastNames =
                ["Иванов", "Петров", "Сидоров", "Смирнов", "Кузнецов", "Попов", "Васильев", "Соколов", "Михайлов", "Новиков"];
            string[] guardianFirstNames =
                ["Елена", "Сергей", "Наталья", "Андрей", "Марина", "Алексей", "Ирина", "Виктор", "Светлана", "Павел"];

            int birthYearStart = DateTime.UtcNow.Year - 16;
            int studentCount = isAcme ? 30 : 6;
            for (int i = 0; i < studentCount; i++)
            {
                var birthDate = new DateOnly(birthYearStart + (i % 10), 1 + (i % 12), 1 + (i % 28));
                var student = Student.Create(
                    lastNames[i % lastNames.Length], firstNames[i], null, birthDate,
                    phone: $"+7 900 000-{10 + i:00}-00",
                    email: $"student{i + 1}@demo.local",
                    managerUserId: managerUserId,
                    source: "seed-demo");
                student.ChangeStatus(StudentStatus.Active);

                var guardian = Guardian.Create(
                    lastNames[i % lastNames.Length], guardianFirstNames[i % guardianFirstNames.Length],
                    phone: $"+7 900 100-{10 + i:00}-00",
                    email: $"guardian{i + 1}@demo.local");
                dbContext.Guardians.Add(guardian);

                student.AddGuardianLink(guardian.Id, "Родитель", isPrimaryPayer: true);
                dbContext.Students.Add(student);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        var teachers = await dbContext.Teachers.OrderBy(t => t.CreatedAtUtc).ToListAsync(cancellationToken).ConfigureAwait(false);
        var students = await dbContext.Students
            .Include(s => s.GuardianLinks)
            .OrderBy(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
        var primaryGuardianByStudent = students.ToDictionary(
            s => s.Id,
            s => s.GuardianLinks.First(g => g.IsPrimaryPayer).GuardianId);

        return (teachers, students, primaryGuardianByStudent);
    }

    /// <summary>Acme: 5 study groups (one primary teacher + 6 students each) across its 3
    /// courses. Globex: 1 study group covering its single course/teacher/6 students. All
    /// activated.</summary>
    private static async Task<IReadOnlyList<StudyGroup>> SeedTenantStudyGroupsAsync(
        IServiceProvider services,
        IReadOnlyList<Course> courses,
        IReadOnlyList<Teacher> teachers,
        IReadOnlyList<Student> students,
        bool isAcme,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<StudyGroupsDbContext>();

        if (!await dbContext.StudyGroups.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            var startDate = DateOnly.FromDateTime(DateTime.UtcNow).AddDays(-14);
            var groupSpecs = isAcme
                ? new (string Code, string Name, int CourseIndex, int TeacherIndex, GroupFormat Format)[]
                  {
                      ("ENG-A1", "Английский, группа A1", 0, 0, GroupFormat.Offline),
                      ("ENG-A2", "Английский, группа A2", 0, 3, GroupFormat.Online),
                      ("DEU-A1", "Немецкий, группа A1", 1, 1, GroupFormat.Offline),
                      ("SPA-A1", "Испанский, группа A1", 2, 2, GroupFormat.Offline),
                      ("SPA-A2", "Испанский, группа A2", 2, 3, GroupFormat.Hybrid),
                  }
                : [("FRA-A1", "Французский, группа A1", 0, 0, GroupFormat.Offline)];

            int studentCursor = 0;
            foreach (var spec in groupSpecs)
            {
                var teacher = teachers[spec.TeacherIndex];
                var group = StudyGroup.Create(
                    spec.Code, spec.Name, courses[spec.CourseIndex].Id, teacher.Id,
                    spec.Format, capacity: 8, startDate: startDate, endDate: null,
#pragma warning disable CA1308 // demo slug is canonical lowercase, not security-sensitive
                    meetingUrl: spec.Format == GroupFormat.Offline ? null : $"https://meet.demo.local/{spec.Code.ToLowerInvariant()}",
#pragma warning restore CA1308
                    roomId: null, notes: null);
                group.AddTeacher(teacher.Id, TeacherRole.Primary);

                for (int i = 0; i < 6; i++)
                {
                    group.Enroll(students[studentCursor].Id, startDate, tariffId: null, discountPercent: 0);
                    studentCursor++;
                }
                group.Activate();

                dbContext.StudyGroups.Add(group);
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return await dbContext.StudyGroups
            .Include(g => g.Enrollments)
            .Include(g => g.Teachers)
            .OrderBy(g => g.CreatedAtUtc)
            .ToListAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Two weekly slots per group for ±2 weeks around today (≈ a month of schedule). Past
    /// sessions are held with attendance seeded (one in eleven marked absent for variety); future
    /// sessions stay planned.</summary>
    private static async Task SeedTenantSchedulingAsync(
        IServiceProvider services, IReadOnlyList<StudyGroup> groups, CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<SchedulingDbContext>();
        if (await dbContext.Sessions.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var room = Room.Create("Кабинет 1", 10, "2 этаж", isVirtual: false);
        var virtualRoom = Room.Create("Онлайн-класс", 20, null, isVirtual: true);
        dbContext.Rooms.AddRange(room, virtualRoom);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var horizonStart = today.AddDays(-14);
        var horizonEnd = today.AddDays(14);

        // Two weekday slots per group — enough occurrences to fill ±2 weeks of history/upcoming.
        (DayOfWeek Day, TimeOnly Time)[][] slotsByGroup =
        [
            [(DayOfWeek.Monday, new TimeOnly(18, 0)), (DayOfWeek.Thursday, new TimeOnly(18, 0))],
            [(DayOfWeek.Tuesday, new TimeOnly(19, 0)), (DayOfWeek.Friday, new TimeOnly(19, 0))],
            [(DayOfWeek.Monday, new TimeOnly(17, 0)), (DayOfWeek.Wednesday, new TimeOnly(17, 0))],
            [(DayOfWeek.Tuesday, new TimeOnly(16, 0)), (DayOfWeek.Thursday, new TimeOnly(16, 0))],
            [(DayOfWeek.Wednesday, new TimeOnly(18, 30)), (DayOfWeek.Saturday, new TimeOnly(11, 0))],
        ];

        for (int g = 0; g < groups.Count; g++)
        {
            var group = groups[g];
            var groupRoom = group.Format == GroupFormat.Online ? virtualRoom : room;
            var teacherId = group.Teachers[0].TeacherId;

            foreach (var (day, time) in slotsByGroup[g % slotsByGroup.Length])
            {
                dbContext.ScheduleTemplates.Add(ScheduleTemplate.Create(
                    group.Id, day, time, durationMinutes: 60, groupRoom.Id, teacherId, horizonStart, validTo: null));

                for (var date = horizonStart; date <= horizonEnd; date = date.AddDays(1))
                {
                    if (date.DayOfWeek != day || date < group.StartDate)
                    {
                        continue;
                    }

                    var startUtc = new DateTimeOffset(date.ToDateTime(time, DateTimeKind.Utc));
                    var session = Session.Create(
                        group.Id, lessonId: null, teacherId, groupRoom.Id,
                        startUtc: startUtc, endUtc: startUtc.AddMinutes(60),
                        topic: null, meetingUrl: group.MeetingUrl);

                    if (date < today)
                    {
                        session.Hold();
                        var activeStudentIds = group.Enrollments
                            .Where(e => e.Status is EnrollmentStatus.Active)
                            .Select(e => e.StudentId);
                        foreach (var studentId in activeStudentIds)
                        {
                            var attendance = Attendance.CreateDefault(session.Id, studentId);
                            if ((date.DayNumber + studentId.GetHashCode()) % 11 == 0)
                            {
                                attendance.Mark(AttendanceStatus.Absent, "Заболел(а)", markedByUserId: null);
                            }
                            dbContext.Attendances.Add(attendance);
                        }
                    }

                    dbContext.Sessions.Add(session);
                }
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <summary>One PerMonth tariff per course; every active enrollment gets an issued invoice for
    /// the current period, billed to its student's primary-payer guardian. Every 3rd invoice is paid
    /// in full, every 3rd+1 partially paid, the rest left open — a realistic status mix.</summary>
    private static async Task SeedTenantPaymentsAsync(
        IServiceProvider services,
        IReadOnlyList<Course> courses,
        IReadOnlyList<StudyGroup> groups,
        IReadOnlyDictionary<Guid, Guid> primaryGuardianByStudent,
        CancellationToken cancellationToken)
    {
        var dbContext = services.GetRequiredService<PaymentsDbContext>();
        if (await dbContext.StudentInvoices.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        decimal[] tariffAmounts = [120m, 100m, 100m];
        var tariffs = new List<Tariff>();
        for (int i = 0; i < courses.Count; i++)
        {
            tariffs.Add(Tariff.Create(
                $"{courses[i].Title} — абонемент (месяц)", courses[i].Id, TariffKind.PerMonth,
                amount: tariffAmounts[i % tariffAmounts.Length], currency: "USD",
                lessonsCount: 8, validDays: 30, chargeOnExcusedAbsence: false));
        }
        dbContext.Tariffs.AddRange(tariffs);
        var tariffByCourse = tariffs.ToDictionary(t => t.CourseId!.Value, t => t);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var periodFrom = new DateOnly(today.Year, today.Month, 1);
        var periodTo = periodFrom.AddMonths(1).AddDays(-1);
        var dueDate = periodFrom.AddDays(10);

        int invoiceIndex = 0;
        foreach (var group in groups)
        {
            var tariff = tariffByCourse[group.CourseId];
            var activeStudentIds = group.Enrollments
                .Where(e => e.Status is EnrollmentStatus.Active)
                .Select(e => e.StudentId);
            foreach (var studentId in activeStudentIds)
            {
                if (!primaryGuardianByStudent.TryGetValue(studentId, out var guardianId))
                {
                    continue;
                }

                var invoice = StudentInvoice.Create(
                    studentId, guardianId, group.Id, periodFrom, periodTo, dueDate,
                    currency: tariff.Currency, comment: null);
                invoice.ReplaceLines([(tariff.Name, tariff.Id, 1m, tariff.Amount)]);
                invoice.Issue(periodFrom);

                switch (invoiceIndex % 3)
                {
                    case 0:
                        invoice.ConfirmPayment(
                            invoice.Total, periodFrom.AddDays(2), PaymentMethod.BankTransfer,
                            reference: null, proofFileId: null, confirmedByUserId: "seed-demo", note: null);
                        break;
                    case 1:
                        invoice.ConfirmPayment(
                            decimal.Round(invoice.Total / 2, 2), periodFrom.AddDays(3), PaymentMethod.Cash,
                            reference: null, proofFileId: null, confirmedByUserId: "seed-demo", note: null);
                        break;
                }
                invoiceIndex++;

                dbContext.StudentInvoices.Add(invoice);
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    // ─── Tickets ────────────────────────────────────────────────────────

    private async Task SeedTenantTicketsAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<TicketsDbContext>();
        if (await dbContext.Tickets.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var usersByEmail = await userManager.Users
            .ToDictionaryAsync(u => u.Email!, u => Guid.Parse(u.Id), cancellationToken)
            .ConfigureAwait(false);

        Guid? UserId(string email) =>
            usersByEmail.TryGetValue(email, out var id) ? id : null;

        IReadOnlyList<TicketScenario> scenarios;
        if (demo.Id == Acme.Id) scenarios = AcmeTicketScenarios(UserId);
        else if (demo.Id == Globex.Id) scenarios = GlobexTicketScenarios(UserId);
        else scenarios = [];

        int number = 1;
        foreach (var scenario in scenarios)
        {
            if (scenario.ReporterUserId is null) continue;

            var ticket = Ticket.Create(
                number: $"TK-{number.ToString(CultureInfo.InvariantCulture)}",
                title: scenario.Title,
                description: scenario.Description,
                priority: scenario.Priority,
                reporterUserId: scenario.ReporterUserId.Value,
                assignedToUserId: scenario.AssignedToUserId);

            foreach (var (authorUserId, body) in scenario.Comments)
            {
                if (authorUserId is null) continue;
                ticket.AddComment(authorUserId.Value, body);
            }

            if (scenario.Resolve)
            {
                ticket.Resolve(scenario.ResolutionNote);
            }

            dbContext.Tickets.Add(ticket);
            number++;
        }

        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        if (_logger.IsEnabled(LogLevel.Information))
        {
            _logger.LogInformation(
                "[demo-seed] [{Tenant}] seeded {Count} demo ticket(s)",
                tenant.Id, number - 1);
        }
    }

    // ─── Chat ───────────────────────────────────────────────────────────

    private async Task SeedTenantChatAsync(DemoTenant demo, CancellationToken cancellationToken)
    {
        using var scope = _services.CreateScope();
        var tenantStore = scope.ServiceProvider.GetRequiredService<IMultiTenantStore<AppTenantInfo>>();
        var tenant = await tenantStore.GetAsync(demo.Id).ConfigureAwait(false);
        if (tenant is null) return;

        scope.ServiceProvider.GetRequiredService<IMultiTenantContextSetter>()
            .MultiTenantContext = new MultiTenantContext<AppTenantInfo>(tenant);

        var dbContext = scope.ServiceProvider.GetRequiredService<ChatDbContext>();
        if (await dbContext.Channels.AnyAsync(cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<FshUser>>();
        var usersByEmail = await userManager.Users
            .ToDictionaryAsync(u => u.Email!, u => u.Id, cancellationToken)
            .ConfigureAwait(false);

        string? UserId(string email) =>
            usersByEmail.TryGetValue(email, out var id) ? id : null;

        int channelCount = 0;
        int messageCount = 0;

        if (demo.Id == Acme.Id)
        {
            var general = await SeedChannelAsync(
                dbContext,
                creator: UserId("admin@acme.com"),
                name: "general",
                description: "Company-wide announcements and watercooler chatter.",
                isPrivate: false,
                additionalMembers: usersByEmail.Values
                    .Where(id => id != UserId("admin@acme.com"))
                    .ToList(),
                messages:
                [
                    (UserId("admin@acme.com"), "Welcome to Acme! 👋 This channel is for company-wide announcements."),
                    (UserId("manager@acme.com"), "Glad to have everyone here. Standups Mondays 10am sharp."),
                    (UserId("alice@acme.com"), "👋"),
                    (UserId("bob@acme.com"), "Coffee chat in 10?"),
                ],
                cancellationToken);
            if (general is not null) { channelCount++; messageCount += 4; }

            var engineering = await SeedChannelAsync(
                dbContext,
                creator: UserId("manager@acme.com"),
                name: "engineering",
                description: "Eng-only. Tickets, deploys, post-mortems.",
                isPrivate: true,
                additionalMembers: [UserId("alice@acme.com"), UserId("bob@acme.com"), UserId("carol@acme.com")],
                messages:
                [
                    (UserId("manager@acme.com"), "What's everyone shipping this week?"),
                    (UserId("alice@acme.com"), "Login redesign — code review out tomorrow."),
                    (UserId("bob@acme.com"), "Mobile hydration fix, then a perf pass on /reports."),
                ],
                cancellationToken);
            if (engineering is not null) { channelCount++; messageCount += 3; }

            var random = await SeedChannelAsync(
                dbContext,
                creator: UserId("admin@acme.com"),
                name: "random",
                description: "Off-topic. Memes, weekend plans, dog photos.",
                isPrivate: false,
                additionalMembers: usersByEmail.Values
                    .Where(id => id != UserId("admin@acme.com"))
                    .ToList(),
                messages:
                [
                    (UserId("gina@acme.com"), "Anyone tried the new ramen place on 5th?"),
                    (UserId("henry@acme.com"), "Two thumbs up. The tonkotsu is worth the wait."),
                ],
                cancellationToken);
            if (random is not null) { channelCount++; messageCount += 2; }

            // DM between alice + bob
            var aliceId = UserId("alice@acme.com");
            var bobId = UserId("bob@acme.com");
            if (aliceId is not null && bobId is not null)
            {
                var dm = ChatChannel.CreateDirect(aliceId, bobId);
                dbContext.Channels.Add(dm);
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                dbContext.Messages.Add(Message.Create(dm.Id, aliceId, "hey, got a sec for the hydration thing?"));
                dbContext.Messages.Add(Message.Create(dm.Id, bobId,   "yeah, throw me a repro and i'll look in the morning"));
                await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

                channelCount++; messageCount += 2;
            }
        }
        else if (demo.Id == Globex.Id)
        {
            var general = await SeedChannelAsync(
                dbContext,
                creator: UserId("admin@globex.com"),
                name: "general",
                description: "Company-wide channel.",
                isPrivate: false,
                additionalMembers: [UserId("dave@globex.com")],
                messages:
                [
                    (UserId("admin@globex.com"), "Welcome to Globex. Ping me here if you need anything."),
                ],
                cancellationToken);
            if (general is not null) { channelCount++; messageCount += 1; }
        }

        if (_logger.IsEnabled(LogLevel.Information) && channelCount > 0)
        {
            _logger.LogInformation(
                "[demo-seed] [{Tenant}] seeded {Channels} chat channel(s) and {Messages} message(s)",
                tenant.Id, channelCount, messageCount);
        }
    }

    private static async Task<ChatChannel?> SeedChannelAsync(
        ChatDbContext dbContext,
        string? creator,
        string name,
        string description,
        bool isPrivate,
        IReadOnlyList<string?> additionalMembers,
        IReadOnlyList<(string? AuthorUserId, string Body)> messages,
        CancellationToken cancellationToken)
    {
        if (creator is null) return null;

        var channel = ChatChannel.CreateChannel(name, description, isPrivate, creator);
        foreach (var memberId in additionalMembers.Where(m => m is not null && m != creator).Distinct())
        {
            try
            {
                channel.AddMember(memberId!, addedByUserId: creator);
            }
            catch (InvalidOperationException)
            {
                // "User already a member" — defensive against duplicate ids in the list.
            }
        }
        dbContext.Channels.Add(channel);
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        foreach (var (authorUserId, body) in messages)
        {
            if (authorUserId is null) continue;
            dbContext.Messages.Add(Message.Create(channel.Id, authorUserId, body));
        }
        await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);

        return channel;
    }

    // ─── Demo content shapes ───────────────────────────────────────────

    internal sealed record DemoTenant(string Id, string Name, string AdminEmail, string Issuer, string PlanKey);
    internal sealed record DemoUser(
        string UserName,
        string Email,
        string FirstName,
        string LastName,
        IReadOnlyList<string> Roles);
    internal sealed record DemoRole(string Name, string Description, IReadOnlyList<string> Permissions);
    private sealed record TicketScenario(
        string Title,
        string? Description,
        TicketPriority Priority,
        Guid? ReporterUserId,
        Guid? AssignedToUserId,
        IReadOnlyList<(Guid? AuthorUserId, string Body)> Comments,
        bool Resolve,
        string? ResolutionNote);

    private static IReadOnlyList<DemoUser> BuildRootUsers() =>
    [
        new("superadmin", "superadmin@root.com", "Super", "Admin", [RoleConstants.Admin]),
    ];

    private static IReadOnlyList<DemoUser> BuildAcmeUsers() =>
    [
        new("acme.manager",  "manager@acme.com",  "Maya",   "Lin",      ["Manager"]),
        new("acme.support",  "support@acme.com",  "Sam",    "Rivera",   ["Support"]),
        new("acme.alice",    "alice@acme.com",    "Alice",  "Nguyen",   [RoleConstants.Basic]),
        new("acme.bob",      "bob@acme.com",      "Bob",    "Patel",    [RoleConstants.Basic]),
        new("acme.carol",    "carol@acme.com",    "Carol",  "Smith",    [RoleConstants.Basic]),
        new("acme.dan",      "dan@acme.com",      "Dan",    "Mueller",  [RoleConstants.Basic]),
        new("acme.erin",     "erin@acme.com",     "Erin",   "Okafor",   [RoleConstants.Basic]),
        new("acme.frank",    "frank@acme.com",    "Frank",  "Tanaka",   [RoleConstants.Basic]),
        new("acme.gina",     "gina@acme.com",     "Gina",   "Kowalski", [RoleConstants.Basic]),
        new("acme.henry",    "henry@acme.com",    "Henry",  "Park",     [RoleConstants.Basic]),
    ];

    private static IReadOnlyList<DemoUser> BuildGlobexUsers() =>
    [
        new("globex.dave",   "dave@globex.com",   "Dave",   "Hartwell", [RoleConstants.Basic]),
    ];

    // Permission claims reference the module contracts constants — never raw strings.
    // A hand-typed name that doesn't match a registry entry (e.g. the old
    // "Permissions.Brands.View" vs the real "Permissions.Catalog.Brands.View")
    // is a claim that grants nothing, silently.
    private static IReadOnlyList<DemoRole> BuildAcmeCustomRoles() =>
    [
        new(
            "Manager",
            "Operations manager — full students/courses/groups + tickets + read-only users.",
            [
                IdentityPermissions.Users.View,
                IdentityPermissions.Users.Update,
                IdentityPermissions.UserRoles.View,
                IdentityPermissions.Roles.View,
                IdentityPermissions.Sessions.View,
                IdentityPermissions.Sessions.Revoke,
                IdentityPermissions.Groups.View,
                PeoplePermissions.Students.View,
                PeoplePermissions.Students.Create,
                PeoplePermissions.Students.Update,
                PeoplePermissions.Students.Delete,
                PeoplePermissions.Teachers.View,
                PeoplePermissions.Teachers.Create,
                PeoplePermissions.Teachers.Update,
                PeoplePermissions.Guardians.View,
                PeoplePermissions.Guardians.Create,
                PeoplePermissions.Guardians.Update,
                CurriculumPermissions.Courses.View,
                CurriculumPermissions.Courses.Create,
                CurriculumPermissions.Courses.Update,
                CurriculumPermissions.Courses.Publish,
                CurriculumPermissions.Subjects.View,
                StudyGroupsPermissions.StudyGroups.View,
                StudyGroupsPermissions.StudyGroups.Create,
                StudyGroupsPermissions.StudyGroups.Update,
                StudyGroupsPermissions.Enrollments.View,
                StudyGroupsPermissions.Enrollments.Create,
                StudyGroupsPermissions.Enrollments.Update,
                TicketsPermissions.Tickets.View,
                TicketsPermissions.Tickets.Create,
                TicketsPermissions.Tickets.Update,
                TicketsPermissions.Tickets.Delete,
            ]),

        new(
            "Support",
            "Support agent — full tickets + read-only users.",
            [
                IdentityPermissions.Users.View,
                IdentityPermissions.UserRoles.View,
                IdentityPermissions.Sessions.View,
                IdentityPermissions.Sessions.Revoke,
                TicketsPermissions.Tickets.View,
                TicketsPermissions.Tickets.Create,
                TicketsPermissions.Tickets.Update,
            ]),
    ];

    private static IReadOnlyList<TicketScenario> AcmeTicketScenarios(Func<string, Guid?> uid) =>
    [
        new("Login button broken on mobile",
            "Tapping login on iOS Safari does nothing on first tap. Have to double-tap.",
            TicketPriority.High,
            uid("alice@acme.com"),
            uid("support@acme.com"),
            [
                (uid("support@acme.com"),
                 "Confirmed on iPhone 15 Safari. Looks like a hydration race on the auth button. Looking now."),
            ],
            Resolve: false,
            ResolutionNote: null),

        new("Add dark mode to the dashboard",
            "Several customers have asked. Let's match the system preference by default.",
            TicketPriority.Medium,
            uid("bob@acme.com"),
            uid("manager@acme.com"),
            [],
            Resolve: false,
            ResolutionNote: null),

        new("Slow page load on /reports",
            "Initial render takes 6-8s with the full quarter view. Need to chunk the query or add a loader.",
            TicketPriority.High,
            uid("carol@acme.com"),
            uid("manager@acme.com"),
            [
                (uid("manager@acme.com"), "Profiling now — the join against audits is the culprit."),
                (uid("carol@acme.com"),   "Thanks. Let me know if you need a repro account."),
            ],
            Resolve: false,
            ResolutionNote: null),

        new("Update copyright year in footer",
            "Footer still says © 2024. Tiny fix, just flagging.",
            TicketPriority.Low,
            uid("dan@acme.com"),
            uid("support@acme.com"),
            [],
            Resolve: true,
            ResolutionNote: "Bumped to 2026 and added a year() helper so we don't have to chase it again."),

        new("Email notifications missing tenant logo",
            "Notification emails render the default placeholder instead of the tenant brand mark.",
            TicketPriority.Medium,
            uid("erin@acme.com"),
            uid("manager@acme.com"),
            [
                (uid("manager@acme.com"),
                 "Template was hardcoded to /assets/default.png — switched to tenant.theme.logoUrl."),
            ],
            Resolve: true,
            ResolutionNote: "Released in 1.4.2. Verified across acme and globex."),

        new("Onboarding survey wording feels stiff",
            "Step 3 copy is robotic. Could we soften it?",
            TicketPriority.Low,
            uid("frank@acme.com"),
            null,
            [],
            Resolve: false,
            ResolutionNote: null),
    ];

    private static IReadOnlyList<TicketScenario> GlobexTicketScenarios(Func<string, Guid?> uid) =>
    [
        new("Need help wiring our Salesforce integration",
            "We're trying to set up the inbound webhook but it keeps returning 401. Maybe a tenant header thing?",
            TicketPriority.Medium,
            uid("dave@globex.com"),
            null,
            [],
            Resolve: false,
            ResolutionNote: null),

        new("Export to CSV truncates long descriptions",
            "Product descriptions over ~500 chars get cut off in the CSV download.",
            TicketPriority.Low,
            uid("dave@globex.com"),
            null,
            [],
            Resolve: false,
            ResolutionNote: null),
    ];
}
