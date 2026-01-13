namespace Platform.Contracts.Application;

// ============================================================================
// Application Case
// ============================================================================

public record CreateApplicationRequest
{
    public required string ApplicationTypeCode { get; init; }
    public string? Description { get; init; }
    public IDictionary<string, object>? Metadata { get; init; }
}

public record ApplicationResponse
{
    public required Guid Id { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string ApplicationTypeCode { get; init; }
    public required string Status { get; init; }
    public string? Description { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? SubmittedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public DateTime? SlaDeadline { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public IReadOnlyList<ApplicationTaskResponse> Tasks { get; init; } = Array.Empty<ApplicationTaskResponse>();
    public IReadOnlyList<AttachmentResponse> Attachments { get; init; } = Array.Empty<AttachmentResponse>();
}

public record ApplicationListResponse
{
    public required Guid Id { get; init; }
    public required string ReferenceNumber { get; init; }
    public required string ApplicationTypeCode { get; init; }
    public required string Status { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? SlaDeadline { get; init; }
}

public record SubmitApplicationRequest
{
    public Guid ApplicationId { get; init; }
}

// ============================================================================
// Tasks
// ============================================================================

public record ApplicationTaskResponse
{
    public required Guid Id { get; init; }
    public required string TaskType { get; init; }
    public required string Status { get; init; }
    public required string Title { get; init; }
    public string? Description { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public required DateTime CreatedAt { get; init; }
    public DateTime? DueAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}

public record CompleteTaskRequest
{
    public required string Decision { get; init; }
    public string? Comments { get; init; }
    public required string OtptToken { get; init; }
    public required string Nonce { get; init; }
}

public record AssignTaskRequest
{
    public required Guid UserId { get; init; }
}

// ============================================================================
// Attachments
// ============================================================================

public record AttachmentResponse
{
    public required Guid Id { get; init; }
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required long SizeBytes { get; init; }
    public required DateTime UploadedAt { get; init; }
    public required string Hash { get; init; }
}

public record UploadAttachmentRequest
{
    public required string FileName { get; init; }
    public required string ContentType { get; init; }
    public required Stream FileStream { get; init; }
}

// ============================================================================
// Pagination
// ============================================================================

public record PagedRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; }
}

public record PagedResponse<T>
{
    public required IReadOnlyList<T> Items { get; init; }
    public required int TotalCount { get; init; }
    public required int Page { get; init; }
    public required int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasNextPage => Page < TotalPages;
    public bool HasPreviousPage => Page > 1;
}
