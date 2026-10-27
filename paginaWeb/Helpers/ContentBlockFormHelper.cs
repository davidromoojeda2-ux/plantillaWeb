using System.Text.Json;
using Aplication.DTOs.ContentBlocks;
using Aplication.Serialization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace paginaWeb.Helpers;

/// <summary>
/// Convierte y valida el JSON del editor de bloques para los formularios del área
/// administrativa, preservando el JSON original cuando hay errores.
/// </summary>
public static class ContentBlockFormHelper
{
    public static bool TryParse(
        string? json,
        ModelStateDictionary modelState,
        out List<ContentBlockDto> blocks,
        string field = "BlocksJson")
    {
        try
        {
            blocks = ContentBlockJson.Deserialize(json);
            return true;
        }
        catch (JsonException ex)
        {
            blocks = [];
            modelState.AddModelError(field, ex.Message);
            return false;
        }
    }
}
