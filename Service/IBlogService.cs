using BasicApiEndpoints.Models;
namespace BasicApiEndpoints.Service;

public interface IBlogService
{
    IReadOnlyList<Blog> GetAllBlogs();
    Blog? GetBlog(int id);
    Blog AddBlog(string title, string content);
    bool DeleteBlog(int id);
    Blog? UpdateBlog(int id, string title, string content);
}





