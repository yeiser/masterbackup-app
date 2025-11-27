using MediatR;
using MasterBackup_API.Domain.Enums;

namespace MasterBackup_API.Application.Features.Users.Commands;

public record InviteUserCommand(
    string Email,
    UserRole Role,
    string InvitedByUserId,
    Guid TenantId
) : IRequest<bool>;
