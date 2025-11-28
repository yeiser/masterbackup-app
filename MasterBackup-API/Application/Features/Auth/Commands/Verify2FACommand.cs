using MasterBackup_API.Application.Common.DTOs;
using MediatR;

namespace MasterBackup_API.Application.Features.Auth.Commands;

public record Verify2FACommand(Verify2FADto Dto) : IRequest<AuthResponseDto>;
