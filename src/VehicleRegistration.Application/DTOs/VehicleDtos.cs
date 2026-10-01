using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleRegistration.Application.DTOs;

public record CreateVehicleRequest(int OwnerId, int ManufacturerId, int YearOfManufacture, decimal Weight);

public record UpdateVehicleRequest(int OwnerId, int ManufacturerId, int YearOfManufacture, decimal Weight);

public record VehicleResponse(int Id, string OwnerName, string ManufacturerName, int YearOfManufacture, decimal Weight, string CategoryName, string CategoryIconUrl);

public record OwnerResponse(int Id, string Name);
public record ManufacturerResponse(int Id, string Name);
