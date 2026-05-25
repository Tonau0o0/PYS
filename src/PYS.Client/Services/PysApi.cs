using PYS.Client.Models;

namespace PYS.Client.Services;

public sealed class PysApi
{
    private readonly ApiClient _api;

    public PysApi(ApiClient api) => _api = api;

    // Auth
    public Task<AuthResponse> LoginAsync(LoginRequest req, CancellationToken ct = default)
        => _api.PostAsync<AuthResponse>("/api/auth/login", req, ct);

    public Task<AuthResponse> RegisterAsync(RegisterRequest req, CancellationToken ct = default)
        => _api.PostAsync<AuthResponse>("/api/auth/register", req, ct);

    public Task ChangePasswordAsync(ChangePasswordRequest req, CancellationToken ct = default)
        => _api.PostAsync("/api/auth/change-password", req, ct);

    public Task UpdateMyColorAsync(string color, CancellationToken ct = default)
        => _api.PutAsync("/api/auth/me/color", new UpdateColorRequest(color), ct);

    public Task<AuthResponse> UpdateProfileAsync(string fullName, CancellationToken ct = default)
        => _api.PutAsync<AuthResponse>("/api/auth/me/profile", new UpdateProfileRequest(fullName), ct);

    public Task<AuthResponse> UpdateEmailAsync(string email, CancellationToken ct = default)
        => _api.PutAsync<AuthResponse>("/api/auth/me/email", new UpdateEmailRequest(email), ct);

    public Task<AuthResponse> UploadAvatarAsync(Stream content, string fileName, CancellationToken ct = default)
        => _api.PostFileAsync<AuthResponse>("/api/auth/me/avatar", content, fileName, ct: ct);

    // Projects
    public Task<ProjectItem[]> GetProjectsAsync(int? status, CancellationToken ct = default)
        => _api.GetAsync<ProjectItem[]>(status.HasValue ? $"/api/projects?status={status}" : "/api/projects", ct);

    public Task<ProjectItem> GetProjectAsync(int id, CancellationToken ct = default)
        => _api.GetAsync<ProjectItem>($"/api/projects/{id}", ct);

    public Task<ProjectItem> CreateProjectAsync(CreateProjectRequest req, CancellationToken ct = default)
        => _api.PostAsync<ProjectItem>("/api/projects", req, ct);

    public Task<ProjectItem> UpdateProjectAsync(int id, UpdateProjectRequest req, CancellationToken ct = default)
        => _api.PutAsync<ProjectItem>($"/api/projects/{id}", req, ct);

    public Task DeleteProjectAsync(int id, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/projects/{id}", ct);

    // Members & Invitations
    public Task<ProjectMemberItem[]> GetMembersAsync(int projectId, CancellationToken ct = default)
        => _api.GetAsync<ProjectMemberItem[]>($"/api/projects/{projectId}/members", ct);

    public Task<InvitationItem[]> GetInvitationsAsync(int projectId, CancellationToken ct = default)
        => _api.GetAsync<InvitationItem[]>($"/api/projects/{projectId}/invitations", ct);

    public Task InviteAsync(int projectId, InviteRequest req, CancellationToken ct = default)
        => _api.PostAsync($"/api/projects/{projectId}/invitations", req, ct);

    public Task RemoveMemberAsync(int projectId, int userId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/projects/{projectId}/members/{userId}", ct);

    public Task CancelInvitationAsync(int projectId, int invitationId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/projects/{projectId}/invitations/{invitationId}", ct);

    // Tasks
    public Task<TaskItem[]> GetTasksAsync(int? projectId, CancellationToken ct = default)
        => _api.GetAsync<TaskItem[]>(projectId.HasValue ? $"/api/tasks?projectId={projectId}" : "/api/tasks", ct);

    public Task<TaskItem> CreateTaskAsync(CreateTaskRequest req, CancellationToken ct = default)
        => _api.PostAsync<TaskItem>("/api/tasks", req, ct);

    public Task<TaskItem> UpdateTaskAsync(int id, UpdateTaskRequest req, CancellationToken ct = default)
        => _api.PutAsync<TaskItem>($"/api/tasks/{id}", req, ct);

    public Task DeleteTaskAsync(int id, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/tasks/{id}", ct);

    // Project Resources (dosya sistemi)
    public Task<ProjectResourceItem[]> GetResourcesAsync(int projectId, int? folderId = null, CancellationToken ct = default)
        => _api.GetAsync<ProjectResourceItem[]>(
            folderId.HasValue ? $"/api/projects/{projectId}/resources?folderId={folderId}" : $"/api/projects/{projectId}/resources", ct);

    public Task<ProjectResourceItem> CreateFolderAsync(int projectId, string name, int? parentFolderId, CancellationToken ct = default)
        => _api.PostAsync<ProjectResourceItem>($"/api/projects/{projectId}/resources/folder", new CreateFolderRequest(name, parentFolderId), ct);

    public Task<ProjectResourceItem> AddYouTubeAsync(int projectId, string title, string url, int? parentFolderId, CancellationToken ct = default)
        => _api.PostAsync<ProjectResourceItem>($"/api/projects/{projectId}/resources/link", new AddYouTubeRequest(title, url, parentFolderId), ct);

    public Task<ProjectResourceItem> UploadResourceAsync(int projectId, Stream content, string fileName, string title, int? parentFolderId, CancellationToken ct = default)
    {
        var fields = new Dictionary<string, string> { ["title"] = title };
        if (parentFolderId.HasValue) fields["parentFolderId"] = parentFolderId.Value.ToString();
        return _api.PostFileAsync<ProjectResourceItem>($"/api/projects/{projectId}/resources/file", content, fileName, fields, ct: ct);
    }

    public Task MoveResourceAsync(int projectId, int resourceId, int? parentFolderId, CancellationToken ct = default)
        => _api.PutAsync($"/api/projects/{projectId}/resources/{resourceId}/move", new MoveResourceRequest(parentFolderId), ct);

    public Task DeleteResourceAsync(int projectId, int resourceId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/projects/{projectId}/resources/{resourceId}", ct);

    // Görev ↔ kaynak bağları
    public Task<ProjectResourceItem[]> GetTaskResourcesAsync(int taskId, CancellationToken ct = default)
        => _api.GetAsync<ProjectResourceItem[]>($"/api/tasks/{taskId}/resources", ct);

    public Task LinkResourceToTaskAsync(int taskId, int resourceId, CancellationToken ct = default)
        => _api.PostAsync($"/api/tasks/{taskId}/resources/{resourceId}", new { }, ct);

    public Task UnlinkResourceFromTaskAsync(int taskId, int resourceId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/tasks/{taskId}/resources/{resourceId}", ct);
}
