using System.ComponentModel.DataAnnotations;
using Swashbuckle.AspNetCore.JsonMultipartFormDataSupport.Attributes;

namespace Demo.Models;

public class SampleRequestWrapper
{
    [Required]
    public IFormFile FileRequired { get; set; } = null!;

    public IFormFile? FileOptional { get; set; }

    [Required]
    public IFormFileCollection MultiplyFilesRequired { get; set; } = null!;

    public IFormFileCollection? MultiplyFilesOptional { get; set; }

    [Required, FromJson]
    public SampleModel JsonBodyRequired { get; set; } = null!;

    [FromJson]
    public SampleModel? JsonBodyOptional { get; set; }
}