// Add these to: Backend/FormBuilder.API/DTOs/FormBuilderDTOs.cs

namespace FormBuilder.API.DTOs;

public record FormRecordDto(
    int Id,
    int FormId,
    string RecordData,
    string Status,
    string? CreatedBy,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateFormRecordDto(
    int FormId,
    string RecordData,
    string? CreatedBy
);

public record UpdateFormRecordDto(
    string RecordData,
    string? CreatedBy
);

public record FormRecordPagedResult(
    List<FormRecordDto> Items,
    int TotalCount,
    int Page,
    int PageSize
);
