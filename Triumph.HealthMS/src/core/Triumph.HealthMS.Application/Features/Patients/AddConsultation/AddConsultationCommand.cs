namespace Triumph.HealthMS.Application.Features.Patients.AddConsultation;

public record AddConsultationRequest(
string PatientId,
string DoctorId,
string? VisitId);

public record AddConsultationResult(Guid ConsultationId);

public record AddConsultationCommand(
    Guid PatientId,
    Guid DoctorId,
    Guid? VisitId) : ICommand<BaseResponse<AddConsultationResult>>;