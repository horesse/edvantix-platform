using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using FSH.Framework.Shared.Multitenancy;
using FSH.Modules.Curriculum.Data;
using FSH.Modules.Curriculum.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Curriculum.Tests.Data;

/// <summary>Covers the provisioning default seeded by <see cref="CurriculumDbInitializer.SeedAsync"/> —
/// see docs/04 Задачи/Задачи · Доработки каркаса.md → Multitenancy → "Шаги провижининга под новые
/// модули". Idempotency (no duplicate <see cref="Subject"/> rows on re-run) mirrors
/// <c>IdentityDbInitializer</c>'s "check before insert" tests.</summary>
public sealed class CurriculumDbInitializerTests
{
    [Fact]
    public async Task SeedAsync_On_EmptyDatabase_Creates_Default_Subjects()
    {
        await using var db = TestCurriculumDbContextFactory.Create();
        var initializer = CreateInitializer(db);

        await initializer.SeedAsync(CancellationToken.None);

        var subjects = await db.Subjects.ToListAsync();
        subjects.Count.ShouldBe(2);
        subjects.ShouldContain(s => s.Name == "Английский язык");
        subjects.ShouldContain(s => s.Name == "Математика");
        subjects.ShouldAllBe(s => s.ParentId == null);
    }

    [Fact]
    public async Task SeedAsync_Called_Twice_Does_Not_Duplicate_Subjects()
    {
        await using var db = TestCurriculumDbContextFactory.Create();
        var initializer = CreateInitializer(db);

        await initializer.SeedAsync(CancellationToken.None);
        await initializer.SeedAsync(CancellationToken.None);

        var subjects = await db.Subjects.ToListAsync();
        subjects.Count.ShouldBe(2);
    }

    [Fact]
    public async Task SeedAsync_Does_Not_Recreate_Subject_Already_Seeded_By_Slug()
    {
        // A subject with the same Slug already exists (e.g. the school renamed it, or a retried
        // provisioning run partially completed) — SeedAsync must recognize it by Slug, not add a
        // second row, per the idempotency contract in the doc comment.
        await using var db = TestCurriculumDbContextFactory.Create();
        db.Subjects.Add(Subject.Create("Английский язык", parentId: null, sortOrder: 0));
        await db.SaveChangesAsync();

        var initializer = CreateInitializer(db);
        await initializer.SeedAsync(CancellationToken.None);

        var subjects = await db.Subjects.ToListAsync();
        subjects.Count.ShouldBe(2); // "Английский язык" (pre-existing) + "Математика" (newly seeded)
    }

    private static CurriculumDbInitializer CreateInitializer(CurriculumDbContext db)
    {
        var tenantAccessor = Substitute.For<IMultiTenantContextAccessor<AppTenantInfo>>();
        tenantAccessor.MultiTenantContext.Returns(
            new MultiTenantContext<AppTenantInfo>(new AppTenantInfo("tenant-acme", "tenant-acme")));

        return new CurriculumDbInitializer(db, tenantAccessor, NullLogger<CurriculumDbInitializer>.Instance);
    }
}
