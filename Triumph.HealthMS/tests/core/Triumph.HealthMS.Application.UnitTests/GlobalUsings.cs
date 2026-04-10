// Global using directives

global using FluentAssertions;
global using MassTransit;
global using Microsoft.EntityFrameworkCore;
global using Microsoft.Extensions.Logging;
global using NSubstitute;
global using NSubstitute.ExceptionExtensions;
global using Triumph.HealthMS.Application.Features.HealthFacilities.CreateAFacility;
global using Triumph.HealthMS.Application.Features.HealthOrganizations.CreateAnOrganization;
global using Triumph.HealthMS.Application.Interfaces;
global using Triumph.HealthMS.Application.UnitTests.Mocks;
global using Triumph.HealthMS.Domain.Common;
global using Triumph.HealthMS.Domain.Patients;
global using Triumph.HealthMS.Domain.Tenants;
global using Triumph.HealthMS.Domain.Tenants.Employees;
global using Triumph.HealthMS.Shared.Enums;
global using Triumph.HealthMS.Shared.Interfaces;
global using HealthOrganizationEntity = Triumph.HealthMS.Domain.Tenants.HealthOrganization;