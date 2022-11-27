using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Attributes;

namespace Demo.Models;

public class SampleRequestWrapper
{
    [Required]
    public IFormFile File { get; set; } = null!;

    [Required, FromJson]
    public SampleModel JsonBody { get; set; } = null!;

    [Required, FromJson]
    public SampleModel JsonBody2 { get; set; } = null!;
}