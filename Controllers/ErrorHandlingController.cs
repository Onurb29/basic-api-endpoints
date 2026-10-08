using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

// ...existing code...
[ApiController]
[Route("api/[controller]")]
public class ErrorHandlingController : ControllerBase
{
    private readonly ILogger<ErrorHandlingController> _logger;

    public ErrorHandlingController(ILogger<ErrorHandlingController> logger)
    {
        _logger = logger;
    }

    [HttpGet("division")]
    public IActionResult GetDivisionResult(int numerator, int denominator)
    {
        if (denominator == 0)
        {
            _logger.LogWarning("Division request rejected because the denominator was zero.");
            return BadRequest("Cannot divide by zero.");
        }

        return Ok(new { result = numerator / denominator });
    }

    [HttpGet("global-test")]
    public IActionResult GlobalErrorTest()
    {
        throw new InvalidOperationException(
            "Testing global exception handling.");
    }
}
