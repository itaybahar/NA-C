using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace Blazor_WebAssembly.Services.Api
{
    public class ApiErrorHandler
    {
        private readonly ILogger<ApiErrorHandler> _logger;
        private readonly JsonSerializerOptions _jsonOptions;

        public ApiErrorHandler(ILogger<ApiErrorHandler> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
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
                string userMessage = GetUserFriendlyErrorMessage(ex.StatusCode);

                if (ex.Data.Contains("ResponseContent"))
                {
                    try
                    {
                        var responseContent = ex.Data["ResponseContent"] as string;
                        if (!string.IsNullOrEmpty(responseContent))
                        {
                            var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(responseContent, _jsonOptions);
                            if (!string.IsNullOrEmpty(errorResponse?.Message))
                            {
                                userMessage = errorResponse.Message;
                            }
                        }
                    }
                    catch
                    {
                        // Ignore parsing errors for error responses
                    }
                }

                return ApiResponse<T>.Failure(userMessage, (int?)ex.StatusCode ?? 500);
            }
            catch (Exception ex)
            {
                return HandleGenericException<T>(ex, operationName);
            }
        }

        public async Task<ApiResponse<T>> HandleResponseAsync<T>(HttpResponseMessage response, string operationName)
        {
            try
            {
                _logger.LogInformation($"Processing response for operation: {operationName}, Status: {response.StatusCode}");

                if (response.IsSuccessStatusCode)
                {
                    return await HandleSuccessResponse<T>(response);
                }

                return await HandleErrorResponse<T>(response, operationName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error handling response for {operationName}");
                return ApiResponse<T>.Failure("An error occurred while processing the server response.", 500);
            }
        }

        private async Task<ApiResponse<T>> HandleSuccessResponse<T>(HttpResponseMessage response)
        {
            if (typeof(T) == typeof(bool))
            {
                return ApiResponse<T>.Success((T)(object)true);
            }

            var content = await response.Content.ReadAsStringAsync();

            if (string.IsNullOrEmpty(content))
            {
                return HandleEmptyContent<T>();
            }

            try
            {
                var result = JsonSerializer.Deserialize<T>(content, _jsonOptions);
                return ApiResponse<T>.Success(result!);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "JSON parsing error");
                return ApiResponse<T>.Failure("Error processing data from the server.", 422);
            }
        }

        private async Task<ApiResponse<T>> HandleErrorResponse<T>(HttpResponseMessage response, string operationName)
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning($"API error in {operationName}: {response.StatusCode}, Content: {errorContent}");

            string userMessage = GetUserFriendlyErrorMessage(response.StatusCode);

            if (!string.IsNullOrEmpty(errorContent))
            {
                try
                {
                    var errorResponse = JsonSerializer.Deserialize<ErrorResponse>(errorContent, _jsonOptions);
                    if (!string.IsNullOrEmpty(errorResponse?.Message))
                    {
                        userMessage = errorResponse.Message;
                    }
                }
                catch
                {
                    // Ignore parsing errors for error responses
                }
            }

            return ApiResponse<T>.Failure(userMessage, (int)response.StatusCode);
        }

        private ApiResponse<T> HandleEmptyContent<T>()
        {
            if (typeof(T) == typeof(string))
            {
                return ApiResponse<T>.Success((T)(object)string.Empty);
            }

            if (typeof(T).IsValueType)
            {
                return ApiResponse<T>.Success(default!);
            }

            try
            {
                var instance = Activator.CreateInstance<T>();
                return ApiResponse<T>.Success(instance);
            }
            catch
            {
                return ApiResponse<T>.Success(default!);
            }
        }

        private ApiResponse<T> HandleGenericException<T>(Exception ex, string operationName)
        {
            var (message, statusCode) = ex switch
            {
                JsonException _ => ("Error processing data from the server.", 422),
                TaskCanceledException _ => ("The request timed out. Please check your connection and try again.", 408),
                InvalidOperationException when ex.Message.Contains("JSON") => ("Error processing data from the server.", 422),
                _ => ("An unexpected error occurred. Please try again later.", 500)
            };

            _logger.LogError(ex, $"{message} during {operationName}");
            return ApiResponse<T>.Failure(message, statusCode);
        }

        private static string GetUserFriendlyErrorMessage(HttpStatusCode? statusCode)
        {
            return statusCode switch
            {
                HttpStatusCode.NotFound => "The requested resource was not found.",
                HttpStatusCode.Unauthorized => "Your session has expired. Please sign in again.",
                HttpStatusCode.Forbidden => "You don't have permission to access this resource.",
                HttpStatusCode.BadRequest => "The request contained invalid data.",
                HttpStatusCode.InternalServerError => "A server error occurred. The team has been notified.",
                HttpStatusCode.BadGateway => "The server is temporarily unavailable. Please try again.",
                HttpStatusCode.ServiceUnavailable => "The server is currently unavailable. Please try again later.",
                HttpStatusCode.GatewayTimeout => "The request timed out. Please check your connection and try again.",
                HttpStatusCode.RequestTimeout => "The request timed out. Please try again.",
                _ => "A connection error occurred. Please check your network and try again."
            };
        }

        private class ErrorResponse
        {
            public string? Message { get; set; }
            public string? Error { get; set; }
            public int? StatusCode { get; set; }

            [JsonPropertyName("error_description")]
            public string? ErrorDescription { get; set; }

            [JsonPropertyName("error_message")]
            public string? ErrorMessage { get; set; }
        }
    }

    public class ApiResponse<T>
    {
        public bool IsSuccess { get; private set; }
        public T? Data { get; private set; }
        public string? ErrorMessage { get; private set; }
        public int? StatusCode { get; private set; }

        public static ApiResponse<T> Success(T data) =>
            new ApiResponse<T> { IsSuccess = true, Data = data };

        public static ApiResponse<T> Failure(string errorMessage, int statusCode) =>
            new ApiResponse<T> { IsSuccess = false, ErrorMessage = errorMessage, StatusCode = statusCode };
    }
} 