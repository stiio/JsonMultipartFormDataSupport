[![https://www.nuget.org/packages/Stio.JsonMultipartFormDataSupport](https://img.shields.io/nuget/v/Stio.JsonMultipartFormDataSupport)](https://www.nuget.org/packages/Stio.JsonMultipartFormDataSupport/)

# JsonMultipartFormDataSupport
[Morasiu's project](https://github.com/Morasiu/Swashbuckle.AspNetCore.JsonMultipartFormDataSupport) is taken as a basis.  
The main difference from the original source is reading the json body from Request.Form.Files.

# Usage
1. Simple add this to your ConfigureServices
```csharp
services.AddJsonMultipartFormDataSupport(JsonSerializerChoice.SystemText);
```

2. Create your wrapper
```csharp
public class MyWrapper
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [FromJson, Required]
    public MyJson Json { get; set; } = null!;
}
```
and then add to your controller
```csharp
[HttpPost]
public IActionResult Post([FromForm] MyWrapper request)
{
    return this.Ok();
}
```
