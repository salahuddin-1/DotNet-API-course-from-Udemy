namespace DotnetAPI.Models;

public partial class Post
{
    public required int PostId { get; set; }
    public required int UserId { get; set; }
    public required string? PostTitle { get; set; }
    public required string? PostContent { get; set; }
    public required DateTime? PostCreated { get; set; }
    public required DateTime? PostUpdate { get; set; }
}