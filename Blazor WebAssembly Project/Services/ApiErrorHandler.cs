// Blazor WebAssembly Project/Services/ApiErrorHandler.cs
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services
{
    public class ApiErrorHandler
    {
        private readonly ILogger<ApiErrorHandler> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiErrorHandler(ILogger<ApiErrorHandler> logger)
        {
            _logger = logger;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

        public async Task<ApiResponse<T>> HandleRequestAsync<T>(Func<Task<T>> apiCall, string operationName)
        {
            try
            {
                _logger.LogInformation($"Executing API operation: {operationName}");
                var result = await apiCall();
                return ApiResponse<T>.Success(result);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, $"HTTP request failed during {operationName}: Status={ex.StatusCode}");

                string userMessage = ex.StatusCode switch
                {
                    HttpStatusCode.NotFound => "המשאב המבוקש לא נמצא.",
                    HttpStatusCode.Unauthorized => "נדרשת הרשאה לפעולה זו.",
                    HttpStatusCode.Forbidden => "אין לך הרשאות לפעולה זו.",
                    HttpStatusCode.BadRequest => "הבקשה אינה תקינה.",
                    HttpStatusCode.InternalServerError => "אירעה שגיאה בשרת. נסה שוב מאוחר יותר.",
                    _ => "אירעה שגיאה בתקשורת עם השרת."
                };

                return ApiResponse<T>.Failure(userMessage, (int?)ex.StatusCode ?? 500);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, $"JSON parsing error during {operationName}");
                return ApiResponse<T>.Failure("שגיאה בעיבוד נתונים מהשרת.", 422);
            }
            catch (TaskCanceledException ex)
            {
                _logger.LogWarning(ex, $"Request timeout during {operationName}");
                return ApiResponse<T>.Failure("הבקשה נכשלה עקב זמן המתנה ארוך מדי. נסה שוב.", 408);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Unexpected error during {operationName}");
                return ApiResponse<T>.Failure("אירעה שגיאה לא צפויה. נסה שוב מאוחר יותר.", 500);
            }
        }
    }

    public class ApiResponse<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int? StatusCode { get; private set; }
        public bool IsLoading { get; private set; }

        public static ApiResponse<T> Loading() =>
            new ApiResponse<T> { IsLoading = true };

        public static ApiResponse<T> Success(T data) =>
            new ApiResponse<T> { IsSuccess = true, Data = data, IsLoading = false };

        public static ApiResponse<T> Failure(string errorMessage, int statusCode) =>
            new ApiResponse<T> { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode, IsLoading = false };
    }
}
