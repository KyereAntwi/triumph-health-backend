namespace Triumph.HealthMS.Application.Features.Patients.StartAConsultation;

public record StartAConsultationRequest(string ConsultationId);
public record StartAConsultationCommand(Guid ConsultationId) : ICommand<BaseResponse<Unit>>;