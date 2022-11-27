using Demo.Models;
using Microsoft.AspNetCore.Mvc;

namespace Demo.Controllers;

[ApiController]
[Route("api/sample")]
public class SampleController : ControllerBase
{
    [HttpPost("1")]
    public IActionResult Example1([FromForm] SampleRequestWrapper request)
    {
        return this.Ok();
    }
}