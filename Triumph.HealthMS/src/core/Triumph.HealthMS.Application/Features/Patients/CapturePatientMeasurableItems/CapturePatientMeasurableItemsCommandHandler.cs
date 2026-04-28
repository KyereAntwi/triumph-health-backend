namespace Triumph.HealthMS.Application.Features.Patients.CapturePatientMeasurableItems;

public sealed class CapturePatientMeasurableItemsCommandHandler(
    IPermissionsServices permissionsServices,
    IApplicationDbContext dbContext) 
    : IRequestHandler<CapturePatientMeasurableItemsCommand, BaseResponse<Unit>>
{
    public async Task<BaseResponse<Unit>> Handle(CapturePatientMeasurableItemsCommand command, CancellationToken cancellationToken)
    {
        if (!await permissionsServices.HasPermission(PermissionValue.MANAGE_PATIENTS_MEASUREMENTS, cancellationToken))
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Forbidden",
                Errors = ["Forbidden! You do not have permission to use this command."],
                StatusCode = 403
            };
        }

        var validation = new CapturePatientMeasurableItemsCommandValidator();
        var validationResult = await validation.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Validation Failed",
                Errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList(),
                StatusCode = 400
            };
        }
        
        var existingPatient = await dbContext
            .Patients
            .Select(p => new
            {
                p.Id,
                Visit = p.Visits.FirstOrDefault(v => v.Id == command.VisitId)
            })
            .FirstOrDefaultAsync(p => p.Id == command.PatientId, cancellationToken);
        if (existingPatient is null)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Not Found",
                Errors = ["Patient not found."],
                StatusCode = 404,
            };
        }
        if (existingPatient.Visit is null)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Not Found",
                Errors = [$"Registered visit with Id {command.VisitId} was not found or not connected to selected patient."],
                StatusCode = 404
            };
        }

        var opdItemsExist = await dbContext
            .FacilityOpdCaptureItems
            .AnyAsync(i => command.OpdItems.Select(oi => oi.FacilityOpdCaptureItemId).Contains(i.Id), cancellationToken);
        if (!opdItemsExist)
        {
            return new BaseResponse<Unit>
            {
                IsSuccess = false,
                Message = "Not Found",
                Errors = ["One or more of the provided FacilityOpdCaptureItemIds were not found."],
                StatusCode = 404
            };
        }

        var opdItemIds = command.OpdItems.Select(oi => oi.FacilityOpdCaptureItemId).ToList();
        
        var existingMeasurements = await dbContext
            .PatientFacilityOpdCaptureItems
            .AsTracking()
            .Where(i => i.AssociatedVisit == command.VisitId && opdItemIds.Contains(i.FacilityOpdCaptureItemId))
            .ToDictionaryAsync(i => i.FacilityOpdCaptureItemId, cancellationToken);
        
        foreach (var opdItem in command.OpdItems)
        {
            if (existingMeasurements.TryGetValue(opdItem.FacilityOpdCaptureItemId, out var existingMeasurement))
            {
                existingMeasurement.ValueCaptured = opdItem.ValueCaptured;
                existingMeasurement.Notes = opdItem.Notes;
            }
            else
            {
                dbContext.PatientFacilityOpdCaptureItems.Add(new PatientFacilityOpdCaptureItem
                {
                    FacilityOpdCaptureItemId = opdItem.FacilityOpdCaptureItemId,
                    PatientId = command.PatientId,
                    Notes = opdItem.Notes ?? string.Empty,
                    ValueCaptured = opdItem.ValueCaptured,
                });
            }
        }
        
        await dbContext.SaveChangesAsync(cancellationToken);

        return new BaseResponse<Unit>
        {
            IsSuccess = true,
            Message = "Operation Completed",
            Data = Unit.Value,
            StatusCode = 200
        };
    }
}