namespace Triumph.HealthMS.Application.Features.Patients.CreatePatientVisit;

public record CreatePatientVisitRequest(
    string PatientId,
    string VisitReason);

public record CreatePatientVisitResponse(Guid VisitId);

public record CreatePatientVisitCommand(
    Guid PatientId,
    string VisitReason) : ICommand<BaseResponse<CreatePatientVisitResponse>>;