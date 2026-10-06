namespace BasicApiEndpoints.Service;
using BasicApiEndpoints.Models;

public class BlogService
{
    private readonly List<Blog> _blogs;

    public BlogService()
    {
        _blogs = new List<Blog>
        {
            new Blog { Id = 1, Title = "First Blog", Content = "This is the first blog post." },
            new Blog { Id = 2, Title = "Second Blog", Content = "This is the second blog post." },
            new Blog { Id = 3, Title = "Third Blog", Content = "This is the third blog post." }
        };
    }

    public Blog? GetBlog(int id)
    {
        return _blogs.FirstOrDefault(b => b.Id == id);
    }
}
