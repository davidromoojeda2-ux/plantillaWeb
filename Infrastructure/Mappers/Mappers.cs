// Todos los mappers del proyecto residen en Aplication.Mappers (capa de aplicación).
// Infrastructure los consume con: using Aplication.Mappers;
//
// Mappers disponibles en Aplication.Mappers:
//   • ArticleMapper  – Article          <-> ArticleDto / ArticleResponseDto
//   • CategoryMapper – Category         <-> CategoryDto / CategoryResponseDto
//   • CommentMapper  – Comment          <-> CommentSubmissionDto / CommentResponseDto
//   • PageMapper     – InformativePage  <-> PageDto / PageResponseDto
//   • ImageMapper    – ImageMetadata    ->  ImageMetadataResponseDto
//   • AuthMapper     – AdminUser        ->  AdminUserDto
// ──────────────────────────────────────────────────────────────────────────────
// Este directorio se mantiene vacío intencionalmente para posibles mappers
// específicos de infraestructura en el futuro (ej: mapeo de documentos MongoDB).
