namespace Variable.Enrollment;

public sealed record CohortStaffView(Guid UserId, string Name, string Email, string Capability, bool Effective);
public sealed record CohortSummary(Guid Id, Guid CourseId, string CourseTitle, string Title, DateTimeOffset StartAt,
    DateTimeOffset EndAt, string Lifecycle, long Revision, DateTimeOffset CreatedAt, CohortStaffView[] Staff);
public sealed record CreateCohort(Guid Id, Guid CourseId, string Title, DateTimeOffset StartAt, DateTimeOffset EndAt,
    Guid CoordinatorId, Guid? FacilitatorId = null);
public sealed record ChangeCohortStaff(long ExpectedRevision, Guid UserId, string Capability, bool Assigned, string Reason);
