namespace Triumph.HealthMS.Application.Features.Patients.LinkPatientToCreatedAccount;

public record LinkPatientToCreatedAccountRequest(string PatientId);

public record LinkPatientToCreatedAccountCommand(
    Guid PatientId)
    : ICommand<BaseResponse<Unit>>;