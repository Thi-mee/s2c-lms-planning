namespace Variable.Authoring;

public sealed record CourseSummary(Guid Id, string Title, string Description, string Status, Guid OwnerId,
    long Revision, long RequirementsVersion, DateTimeOffset CreatedAt);
public sealed record CourseDetail(CourseSummary Course, ModuleDraft[] Modules);
public sealed record ModuleDraft(Guid Id, string Title, LessonDraft[] Lessons);
public sealed record LessonDraft(Guid Id, string Title, string Notes, bool Required);
public sealed record CreateCourse(Guid Id, string Title, string Description, Guid OwnerId);
public sealed record SaveCourse(long ExpectedRevision, string Title, string Description, ModuleDraft[] Modules);
public sealed record PublishCourse(long ExpectedRevision);
public sealed record ChangeOwner(long ExpectedRevision, Guid OwnerId, string Reason);
