using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleRegistration.Application.DTOs;

public record CreateCategoryRequest(string Name, decimal MinWeight, decimal? MaxWeight, string IconUrl);

public record UpdateCategoryRequest(string Name, string IconUrl);

public record CategoryResponse(int Id, string Name, decimal MinWeight, decimal? MaxWeight, string IconUrl);

public record CategorySetRequest(List<CategoryItemRequest> Categories);

public record CategoryItemRequest(int? Id, string Name, decimal MinWeight, decimal? MaxWeight, string IconUrl);

public enum AbsorbDirection { Previous, Next }