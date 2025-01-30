namespace DotnetAPI.Dtos
{
    public partial class PostToEditDto
    {
        public required int PostId { get; set; }
        public required string PostTitle { get; set; }
        public required string PostContent { get; set; }
    }
}