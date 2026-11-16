using System.Collections.Generic;

namespace Zyara.Services.Features.Shared.Dtos;

public sealed class LandingIndexDto
{
    public IReadOnlyList<ServiceOptionDto> Services { get; init; } = [];
}

