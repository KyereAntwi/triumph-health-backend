namespace Triumph.HealthMS.Application.Features.Patients.ProcessConsultation;

public record ProcessConsultationRequest(string ConsultationId, string Notes);
public record ProcessConsultationCommand(Guid ConsultationId, string Notes) : ICommand<BaseResponse<Unit>>;