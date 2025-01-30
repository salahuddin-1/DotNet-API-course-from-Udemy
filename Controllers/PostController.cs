using DotnetAPI.Data;
using DotnetAPI.Dtos;
using DotnetAPI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DotnetAPI.Controllers;

[Authorize]
[ApiController]
[Route("[controller]")]
public class PostController(IConfiguration config) : ControllerBase
{

    private readonly DataContextDapper _dapper = new(config);


    [HttpGet("GetPost")]
    public Post GetPost(int postId)
    {
        string sql = @$"SELECT [PostId],
                        [UserId],
                        [PostTitle],
                        [PostContent],
                        [PostCreated],
                        [PostUpdate] 
                    FROM TutorialAppSchema.Posts
                    WHERE PostId = '{postId}'";
        return _dapper.LoadDataSingle<Post>(sql);
    }

    [HttpGet("GetPostsByUser")]
    public IEnumerable<Post> GetPostsByUser(int userId)
    {
        string sql = @$"SELECT [PostId],
                        [UserId],
                        [PostTitle],
                        [PostContent],
                        [PostCreated],
                        [PostUpdate] 
                    FROM TutorialAppSchema.Posts
                    WHERE UserId = '{userId}'";
        return _dapper.LoadData<Post>(sql);
    }


    [HttpGet("GetMyPosts")]
    public IEnumerable<Post> GetMyPosts()
    {
        string userId = this.User.FindFirst("userId")?.Value ?? "";
        string sql = @$"SELECT [PostId],
                        [UserId],
                        [PostTitle],
                        [PostContent],
                        [PostCreated],
                        [PostUpdate] 
                    FROM TutorialAppSchema.Posts
                    WHERE UserId = '{userId}'";
        return _dapper.LoadData<Post>(sql);
    }

    [HttpPost("AddPost")]
    public IActionResult AddPost(PostToAddDto post)
    {
        string userId = this.User.FindFirst("userId")?.Value ?? "";
        string sql = @$"
            INSERT INTO TutorialAppSchema.Posts(
                [UserId],
                [PostTitle],
                [PostContent],
                [PostCreated],
                [PostUpdate]
                ) VALUES (
                    '{userId}',
                    '{post.PostTitle}',
                    '{post.PostContent}',
                    GETDATE(),
                    GETDATE()
                )";
        if (_dapper.ExecuteSql(sql))
        {
            return Ok();
        }
        throw new Exception("Failed to Add Post");
    }

    [HttpPut("EditPost")]
    public IActionResult EditPost(PostToEditDto post)
    {
        string userId = this.User.FindFirst("userId")?.Value ?? "";
        string sql = @$"
            UPDATE TutorialAppSchema.Posts
                SET PostContent = '{post.PostContent}',
                    PostTitle = '{post.PostTitle}',
                    PostUpdate = GETDATE()
                WHERE PostId = {post.PostId}
                AND UserId = {userId}";
        if (_dapper.ExecuteSql(sql))
        {
            return Ok();
        }
        throw new Exception("Failed to Edit Post");
    }


    [HttpDelete("DeletePost")]
    public IActionResult DeletePost(int postId)
    {
        string userId = this.User.FindFirst("userId")?.Value ?? "";
        string sql = @$"
            DELETE FROM TutorialAppSchema.Posts
                WHERE PostId = {postId}
                AND UserId = {userId}";
        if (_dapper.ExecuteSql(sql))
        {
            return Ok();
        }
        throw new Exception("Failed to Delete Post");
    }


    [HttpGet("SearchByPost")]
    public IEnumerable<Post> SearchByPost(string searchQuery)
    {
        string sql = @$"
            SELECT * FROM TutorialAppSchema.Posts
                WHERE PostContent LIKE '%{searchQuery}%'
                OR PostTitle LIKE '%{searchQuery}%'";
        return _dapper.LoadData<Post>(sql);
    }

}