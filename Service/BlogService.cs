using BasicApiEndpoints.Models;
namespace BasicApiEndpoints.Service;

public class BlogService : IBlogService
{
    private readonly List<Blog> _blogs;
    private int _nextId;

    public BlogService()
    {
        _blogs = new List<Blog>
        {
            new Blog { Id = 1, Title = "First Blog", Content = "This is the first blog post." },
            new Blog { Id = 2, Title = "Second Blog", Content = "This is the second blog post." },
            new Blog { Id = 3, Title = "Third Blog", Content = "This is the third blog post." }
        };
        // At the end of the constructor, after initializing _blogs:
        _nextId = _blogs.Max(blog => blog.Id) + 1;
    }

    public IReadOnlyList<Blog> GetAllBlogs()
    {
        return _blogs.ToList();
    }
    public Blog? GetBlog(int id)
    {
        return _blogs.FirstOrDefault(b => b.Id == id);
    }

    public Blog AddBlog(string title, string content)
    {
        var newBlog = new Blog
        {
            Id = _nextId++,
            Title = title,
            Content = content
        };
        _blogs.Add(newBlog);
        return newBlog;
    }

    public bool DeleteBlog(int id)
        {
            var blog = GetBlog(id);
            return blog is not null && _blogs.Remove(blog);
        }

    public Blog? UpdateBlog(int id, string title, string content)
        {
            var blog = GetBlog(id);
            if (blog is null)
            {
                return null;
            }

            blog.Title = title;
            blog.Content = content;
            return blog;
        }
}