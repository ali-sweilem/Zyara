namespace Zyara.Web.ViewModels
{
    public sealed class AvatarViewModel
    {
        public string? FullName { get; init; }
        public string? PhotoUrl { get; init; }
        public int Id { get; init; }
        public int Size { get; init; } = 88;
    }
}