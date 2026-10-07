using Microsoft.AspNetCore.Mvc;
using BasicApiEndpoints.Models;
using BasicApiEndpoints.Service;
using System.Collections.Generic;
using System.Linq;

[Route("api/blogs")]
[ApiController]
public class BlogsController : ControllerBase
{
    private readonly IBlogService _blogService;

    public BlogsController(IBlogService blogService)
    {
        _blogService = blogService;
    }

    [HttpGet]
    public IActionResult GetAllBlogs()
    {
        var blogs = _blogService.GetAllBlogs();
        return Ok(blogs);
    }

    [HttpGet("{id}")]
    public IActionResult GetBlogById(int id)
    {
        var blog = _blogService.GetBlog(id);
        if (blog == null)
        {
            return NotFound($"Blog with ID {id} not found.");
        }
        return Ok(blog);
    }

    [HttpPost]
    public IActionResult CreateBlog([FromBody] CreateBlogRequest request)
    {
        var createdBlog = _blogService.AddBlog(request.Title, request.Content);
        return CreatedAtAction(nameof(GetBlogById), new { id = createdBlog.Id }, createdBlog);
    }

    [HttpPut("{id}")]
    public IActionResult UpdateBlog(int id, [FromBody] CreateBlogRequest request)
    {
        var updatedBlog = _blogService.UpdateBlog(id, request.Title, request.Content);
        if (updatedBlog == null)
        {
            return NotFound($"Blog with ID {id} not found.");
        }
        return Ok(updatedBlog);
    }

    [HttpDelete("{id}")]
    public IActionResult DeleteBlog(int id)
    {
        var success = _blogService.DeleteBlog(id);
        if (!success)
        {
            return NotFound($"Blog with ID {id} not found.");
        }
        return NoContent();
    }
}