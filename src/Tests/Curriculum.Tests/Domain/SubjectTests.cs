using FSH.Modules.Curriculum.Domain;

namespace Curriculum.Tests.Domain;

public sealed class SubjectTests
{
    [Fact]
    public void Create_Should_SlugifyName_When_Created()
    {
        var subject = Subject.Create("Английский язык", parentId: null, sortOrder: 0);

        subject.Slug.ShouldNotBeNullOrWhiteSpace();
        subject.Name.ShouldBe("Английский язык");
    }

    [Fact]
    public void Create_Should_TrimName_When_Created()
    {
        var subject = Subject.Create("  Math  ", parentId: null, sortOrder: 0);

        subject.Name.ShouldBe("Math");
    }

    [Fact]
    public void Update_Should_Throw_When_ParentIsSelf()
    {
        var subject = Subject.Create("Math", parentId: null, sortOrder: 0);

        Should.Throw<InvalidOperationException>(() => subject.Update("Math", subject.Id));
    }

    [Fact]
    public void Update_Should_ChangeParent_When_ParentIsDifferent()
    {
        var subject = Subject.Create("Math", parentId: null, sortOrder: 0);
        var newParentId = Guid.NewGuid();

        subject.Update("Math", newParentId);

        subject.ParentId.ShouldBe(newParentId);
    }
}
