using FSH.Modules.Curriculum.Contracts.Dtos;
using FSH.Modules.Curriculum.Domain;

namespace Curriculum.Tests.Domain;

public sealed class CourseTests
{
    private static Course CreateValidCourse() => Course.Create(
        subjectId: Guid.NewGuid(),
        title: " English A1 ",
        description: "Beginner course",
        level: CourseLevel.Beginner,
        durationHours: 40,
        coverFileId: null);

    #region Create

    [Fact]
    public void Create_Should_SetDraftStatus_When_Created()
    {
        Course course = CreateValidCourse();

        course.Status.ShouldBe(CourseStatus.Draft);
    }

    [Fact]
    public void Create_Should_TrimTitle_When_Created()
    {
        Course course = CreateValidCourse();

        course.Title.ShouldBe("English A1");
    }

    [Fact]
    public void Create_Should_Throw_When_DurationIsNegative()
    {
        Should.Throw<ArgumentOutOfRangeException>(() =>
            Course.Create(Guid.NewGuid(), "Title", null, CourseLevel.Beginner, -1, null));
    }

    #endregion

    #region Publish / Archive

    [Fact]
    public void Publish_Should_SetPublishedStatus_And_PublishedAtUtc_When_Draft()
    {
        Course course = CreateValidCourse();

        course.Publish();

        course.Status.ShouldBe(CourseStatus.Published);
        course.PublishedAtUtc.ShouldNotBeNull();
    }

    [Fact]
    public void Publish_Should_BeNoOp_When_AlreadyPublished()
    {
        Course course = CreateValidCourse();
        course.Publish();
        DateTimeOffset? firstPublishedAt = course.PublishedAtUtc;

        course.Publish();

        course.PublishedAtUtc.ShouldBe(firstPublishedAt);
    }

    [Fact]
    public void Publish_Should_Throw_When_Archived()
    {
        Course course = CreateValidCourse();
        course.Archive();

        Should.Throw<FSH.Framework.Core.Exceptions.CustomException>(() => course.Publish());
    }

    [Fact]
    public void Archive_Should_SetArchivedStatus_When_Draft()
    {
        Course course = CreateValidCourse();

        course.Archive();

        course.Status.ShouldBe(CourseStatus.Archived);
    }

    [Fact]
    public void Archive_Should_SetArchivedStatus_When_Published()
    {
        Course course = CreateValidCourse();
        course.Publish();

        course.Archive();

        course.Status.ShouldBe(CourseStatus.Archived);
    }

    #endregion

    #region Soft delete

    [Fact]
    public void Restore_Should_ClearIsDeleted_When_Deleted()
    {
        Course course = CreateValidCourse();

        course.Restore();

        course.IsDeleted.ShouldBeFalse();
    }

    #endregion
}
