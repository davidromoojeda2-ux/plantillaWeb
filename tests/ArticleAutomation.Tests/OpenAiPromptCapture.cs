using System.Net;
using System.Text;
using System.Text.Json;

namespace ArticleAutomation.Tests;

/// <summary>
/// Ejecuta una llamada a un servicio de IA sobre un HTTP simulado y devuelve el
/// prompt enviado (mensaje de sistema) para poder inspeccionarlo en las pruebas.
/// </summary>
internal static class OpenAiPromptCapture
{
    public static async Task<string> CaptureAsync(Func<HttpClient, Task> call)
    {
        string? requestBody = null;

        var content = JsonSerializer.Serialize(new
        {
            title = "Título de prueba",
            summary = "Resumen de prueba",
            metaDescription = "Meta descripción",
            tags = new[] { "uno", "dos", "tres" },
            blocks = new object[] { new { type = "paragraph", text = "Contenido de prueba." } }
        });
        var chat = JsonSerializer.Serialize(new
        {
            choices = new[] { new { message = new { content } } },
            usage = new { total_tokens = 1 }
        });

        var handler = new StubHttpMessageHandler(request =>
        {
            requestBody = request.Content!.ReadAsStringAsync().GetAwaiter().GetResult();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(chat, Encoding.UTF8, "application/json")
            };
        });

        await call(new HttpClient(handler));

        using var document = JsonDocument.Parse(
            requestBody ?? throw new InvalidOperationException("No se capturó la petición."));

        return document.RootElement
            .GetProperty("messages")[1]
            .GetProperty("content")
            .GetString()!;
    }
}
