namespace paginaWeb.Areas.Admin.Models;

/// <summary>
/// Datos del editor de bloques reutilizable. Lo comparten el formulario de
/// artículos y el de secciones institucionales.
/// </summary>
public sealed record ContentBlocksEditorViewModel(string? Id, string BlocksJson);
