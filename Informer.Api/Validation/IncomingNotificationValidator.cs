using System.Text.Json;
using Informer.Core.Dto;

namespace Informer.Api.Validation;

public static class IncomingNotificationValidator
{
    private const int MaxHeaderLength = 256;
    private const int MaxDescriptionLength = 2000;
    private const int MaxBodySizeBytes = 512 * 1024; // 512 KB, guards against oversized "garbage" payloads

    /// <summary>
    /// Structural + size validation performed before anything is written to the database.
    /// Returns false with an error message if the payload should be rejected as garbage.
    /// </summary>
    public static bool TryValidate(IncomingNotificationDto dto, string rawBody, out string? error)
    {
        if (string.IsNullOrWhiteSpace(dto.Header))
        {
            error = "\"header\" is required and cannot be empty.";
            return false;
        }

        if (dto.Header.Length > MaxHeaderLength)
        {
            error = $"\"header\" exceeds max length of {MaxHeaderLength} characters.";
            return false;
        }

        if (dto.Description?.Length > MaxDescriptionLength)
        {
            error = $"\"description\" exceeds max length of {MaxDescriptionLength} characters.";
            return false;
        }

        if (dto.ResponseBody.ValueKind == JsonValueKind.Undefined)
        {
            error = "\"ResponseBody\" is required.";
            return false;
        }

        if (System.Text.Encoding.UTF8.GetByteCount(rawBody) > MaxBodySizeBytes)
        {
            error = $"Payload exceeds max size of {MaxBodySizeBytes / 1024} KB.";
            return false;
        }

        error = null;
        return true;
    }
}
