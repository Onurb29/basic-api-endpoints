using BasicApiEndpoints.Models;

namespace BasicApiEndpoints.Service;

public class ProductService
{
	private readonly List<Product> _products =
	[
		new Product { Id = 1, Name = "Laptop", Description = "Portable computer", Price = 999.99m },
		new Product { Id = 2, Name = "Mouse", Description = "Wireless mouse", Price = 29.99m },
		new Product { Id = 3, Name = "Keyboard", Description = "Mechanical keyboard", Price = 79.99m }
	];

	private int _nextId = 4;

	public IReadOnlyList<Product> GetAllProducts() => _products.ToList();

	public Product? GetProductById(int id) =>
		_products.FirstOrDefault(product => product.Id == id);

	public Product CreateProduct(Product product)
	{
		var createdProduct = new Product
		{
			Id = _nextId++,
			Name = product.Name,
			Description = product.Description,
			Price = product.Price
		};

		_products.Add(createdProduct);
		return createdProduct;
	}

	public Product? UpdateProduct(int id, Product updatedProduct)
	{
		var product = GetProductById(id);
		if (product is null)
		{
			return null;
		}

		product.Name = updatedProduct.Name;
		product.Description = updatedProduct.Description;
		product.Price = updatedProduct.Price;
		return product;
	}

	public bool DeleteProduct(int id)
	{
		var product = GetProductById(id);
		return product is not null && _products.Remove(product);
	}
}
