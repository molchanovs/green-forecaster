using Microsoft.AspNetCore.Mvc;

namespace Forecaster.ComponentTests.TestControllers;

/// <summary>
/// Controller used only in component tests to trigger exceptions
/// so that the ExceptionMiddleware behaviour can be verified via HTTP.
/// It is registered through ForecasterApiFactory and is never present
/// in the production application.
/// </summary>
[ApiController]
[Route("/test")]
public class ThrowingController : ControllerBase
{
    [HttpGet("throw")]
    public IActionResult ThrowUnhandled()
        => throw new InvalidOperationException("Simulated unhandled exception");

    [HttpGet("throw-argument")]
    public IActionResult ThrowArgument()
        => throw new ArgumentException("Simulated argument exception");
}

