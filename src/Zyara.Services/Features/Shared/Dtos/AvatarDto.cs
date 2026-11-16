namespace Zyara.Services.Features.Shared.Dtos
{
    public sealed class AvatarDto
    {
        public string? FullName { get; init; }
        public string? PhotoUrl { get; init; }
        public int Id { get; init; }
        public int Size { get; init; } = 88;
    }
}

