namespace Triumph.HealthMS.Application.Features.Patients.CreateAPatient;

public record CreateAPatientRequest(
    string FirstName,
    string LastName,
    string OtherNames,
    string? Email,
    string PhoneNumber,
    string Gender,
    string  DateOfBirth,
    string Nationality,
    string? PassportNumber,
    string? HomeAddress,
    string NationalIdNumber,
    string? VisitReason,
    string? VisitTimeStamp,
    bool SendLinkForPatientAccountLinking);

public record CreateAPatientResponse(Guid PatientId);

public record CreateAPatientCommand(
    string FirstName,
    string LastName,
    string OtherNames,
    string? Email,
    string PhoneNumber,
    string Gender,
    DateOnly  DateOfBirth,
    string Nationality,
    string? PassportNumber,
    string? HomeAddress,
    string NationalIdNumber,
    string? VisitReason,
    DateTime? VisitTimeStamp,
    bool SendLinkForPatientAccountLinking) : ICommand<BaseResponse<CreateAPatientResponse>>;