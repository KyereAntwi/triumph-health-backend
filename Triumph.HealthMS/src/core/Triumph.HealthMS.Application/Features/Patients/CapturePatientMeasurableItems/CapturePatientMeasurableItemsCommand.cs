namespace Triumph.HealthMS.Application.Features.Patients.CapturePatientMeasurableItems;

public record CapturePatientMeasurableItemsRequest( string VisitId, string PatientId, IEnumerable<OpdItemDto> OpdItems);

public record OpdItemDto(string FacilityOpdCaptureItemId, string ValueCaptured, string? Notes);
public record OpdItemCommandDto(Guid FacilityOpdCaptureItemId, string ValueCaptured, string? Notes);

public record CapturePatientMeasurableItemsCommand(
    Guid VisitId,
    Guid  PatientId,
    IEnumerable<OpdItemCommandDto> OpdItems) : ICommand<BaseResponse<Unit>>;