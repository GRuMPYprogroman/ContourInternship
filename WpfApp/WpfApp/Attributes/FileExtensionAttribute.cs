using System.ComponentModel.DataAnnotations;
using System.IO;

namespace WpfApp.Attributes;

public class FileExtensionAttribute(string extension) : ValidationAttribute
{
    private string Extension { get; } = extension.StartsWith(".") ? extension : "." + extension;

    protected override ValidationResult? IsValid(object? value, ValidationContext validationContext)
    {
        var path = value?.ToString() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(path))
            return new ValidationResult("Provide path to file");

        if (!string.Equals(Path.GetExtension(path), Extension, StringComparison.OrdinalIgnoreCase))
            return new ValidationResult($"File extension must be {Extension}");
        
        return ValidationResult.Success;
    }
}