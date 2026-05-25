namespace PYS.Core.Common;

public enum UserRole
{
    Member = 0,
    Manager = 1,
    Admin = 2
}

public enum ProjectStatus
{
    Planned = 0,
    InProgress = 1,
    OnHold = 2,
    Completed = 3,
    Cancelled = 4
}

public enum TaskStatus
{
    Todo = 0,
    InProgress = 1,
    InReview = 2,
    Done = 3,
    Blocked = 4
}

public enum TaskPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

public enum ProjectRole
{
    Member = 0,
    Manager = 1,
    Owner = 2
}

public enum InvitationStatus
{
    Pending = 0,
    Accepted = 1,
    Cancelled = 2
}
