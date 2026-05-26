// Add this to: Backend/FormBuilder.API/Models/DomainModels.cs

namespace FormBuilder.API.Models;

public class FormRecord
{
    public int Id { get; set; }
    public int FormId { get; set; }
    public string RecordData { get; set; } = "{}";   // JSON
    public string Status { get; set; } = "Active";
    public string? CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public Form? Form { get; set; }
}
