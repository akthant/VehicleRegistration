using System;
using System.Collections.Generic;
using System.Text;

namespace VehicleRegistration.Application.DTOs
{
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }

        public static ApiResponse<T> Ok(T data, string? message = null) => new()
        {
            Success = true,
            Data = data,
            Message = message
        };
    }

    public class ApiErrorResponse
    {
        public bool Success { get; set; } = false;
        public string Message { get; set; } = string.Empty;
        public Dictionary<string, string[]>? Errors { get; set; }

        public static ApiErrorResponse FromMessage(string message) => new()
        {
            Message = message
        };

        public static ApiErrorResponse FromValidation(Dictionary<string, string[]> errors) => new()
        {
            Message = "One or more validation errors occurred.",
            Errors = errors
        };
    }
}
