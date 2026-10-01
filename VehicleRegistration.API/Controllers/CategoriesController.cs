using Microsoft.AspNetCore.Mvc;
using VehicleRegistration.Application.DTOs;
using VehicleRegistration.Application.Exceptions;
using VehicleRegistration.Application.Services;

namespace VehicleRegistration.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;
    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService
            ?? throw new ArgumentNullException(nameof(categoryService));
    }

    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryResponse>>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> GetCategories()
    {
        var categories = await _categoryService.GetAllCategoriesAsync();
        return Ok(ApiResponse<List<CategoryResponse>>.Ok(categories));
    }

    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> GetCategory(int id, CancellationToken ct)
    {
        var category = await _categoryService.GetCategoryByIdAsync(id, ct);
        if (category == null)
            throw new NotFoundException("Category", id);

        return Ok(ApiResponse<CategoryResponse>.Ok(category));
    }

    [HttpPut]
    [ProducesResponseType(typeof(ApiResponse<List<CategoryResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<List<CategoryResponse>>>> ReplaceCategories(
        [FromBody] CategorySetRequest request)
    {
        var categories = await _categoryService.ReplaceCategoriesAsync(request);
        return Ok(ApiResponse<List<CategoryResponse>>.Ok(categories, "Categories updated successfully."));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> CreateCategory([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        var category = await _categoryService.CreateCategoryAsync(request, ct);
        return CreatedAtAction(
            nameof(GetCategory),
            new { id = category.Id },
            ApiResponse<CategoryResponse>.Ok(category, "Category created successfully."));
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ApiResponse<CategoryResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<CategoryResponse>>> UpdateCategory(
        int id,
        [FromBody] UpdateCategoryRequest request,
        CancellationToken ct)
    {
        var category = await _categoryService.UpdateCategoryAsync(id, request, ct);
        if (category == null)
            throw new NotFoundException("Category", id);

        return Ok(ApiResponse<CategoryResponse>.Ok(category, "Category updated successfully."));
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteCategory(int id, [FromQuery] AbsorbDirection? absorbInto, CancellationToken ct)
    {
        if (absorbInto is null)
        {
            await _categoryService.DeleteCategoryAsync(id, ct); // always throws explaining the issue
            return NoContent(); // never reached
        }
        var result = await _categoryService.DeleteAndAbsorbAsync(id, absorbInto.Value, ct);
        return Ok(ApiResponse<List<CategoryResponse>>.Ok(result, "Category deleted."));
    }

}