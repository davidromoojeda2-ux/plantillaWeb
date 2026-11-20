using Aplication.DTOs.ContentBlocks;

namespace Aplication.Services;

/// <summary>Contexto que reciben las reglas: los bloques completos y el perfil activo.</summary>
public sealed record ParagraphContext(
    IReadOnlyList<ContentBlockDto> Blocks,
    ParagraphStyleProfile Profile);
