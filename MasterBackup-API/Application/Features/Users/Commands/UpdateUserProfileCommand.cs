using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Users.Commands;

public record UpdateUserProfileCommand(
    string UserId,
    UpdateProfileDto ProfileDto
) : IRequest<UserProfileDto?>;
