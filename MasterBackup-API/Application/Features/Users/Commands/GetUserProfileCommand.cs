using MediatR;
using MasterBackup_API.Application.Common.DTOs;

namespace MasterBackup_API.Application.Features.Users.Commands;

public record GetUserProfileCommand(string UserId) : IRequest<UserProfileDto?>;
