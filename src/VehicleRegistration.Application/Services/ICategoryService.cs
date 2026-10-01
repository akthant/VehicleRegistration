using System;
using System.Collections.Generic;
using System.Text;
using VehicleRegistration.Application.DTOs;
namespace VehicleRegistration.Application.Services;

public interface ICategoryService
{
    Task<List<CategoryResponse>> GetAllCategoriesAsync();
    Task<CategoryResponse?> GetCategoryByIdAsync(int id, CancellationToken ct = default);
    Task<CategoryResponse> CreateCategoryAsync(CreateCategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse?> UpdateCategoryAsync(int id, UpdateCategoryRequest request, CancellationToken ct = default);
    Task<bool> DeleteCategoryAsync(int id, CancellationToken ct = default);
    Task<List<CategoryResponse>> ReplaceCategoriesAsync(CategorySetRequest request);
    Task<CategoryResponse?> GetCategoryForWeightAsync(decimal weight, CancellationToken ct = default);
    Task<List<CategoryResponse>> DeleteAndAbsorbAsync(int id, AbsorbDirection absorbInto, CancellationToken ct = default);
}