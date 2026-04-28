namespace Triumph.HealthMS.Application.Features.Patients.AddPatientMedications;

public record AddPatientMedicationsRequest(
    string PatientId,
    string? AssociatedVisitId,
    IEnumerable<MedicationDto> Medications);

public record MedicationDto(
    string DrugId,
    string? AdditionalPrescription,
    bool IsCurrentlyBeenTaken,
    string? PrescribedAt,
    int Quantity);

public record MedicationCommandDto(
    Guid DrugId,
    string? AdditionalPrescription,
    bool IsCurrentlyBeenTaken,
    DateTime? PrescribedAt,
    int Quantity);

public record AddPatientMedicationsCommand(
    Guid PatientId,
    Guid? AssociatedVisitId,
    IEnumerable<MedicationCommandDto> Medications) : ICommand<BaseResponse<Unit>>;