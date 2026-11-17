using System.Net;
using System.Text.RegularExpressions;
using Domine.ContentBlocks;
using Domine.Entities;
using Domine.ValueObjects;
using MongoDB.Driver;

namespace Infrastructure.Persistence;

/// <summary>
/// Plantilla de seed para la sección institucional del portal.
/// </summary>
/// <remarks>
/// No se ejecuta automáticamente al arrancar la aplicación. Está pensada para
/// sembrar una instalación nueva de forma manual o controlada.
///
/// Las partes editables se representan con marcadores entre llaves dobles
/// (por ejemplo <c>{{RAZON_SOCIAL}}</c>). La plantilla puede crearse tal cual,
/// conservando los marcadores, o resolverse con <see cref="CreateTemplate(IReadOnlyDictionary{string, string})"/>
/// pasando los valores reales del titular. Los marcadores también pueden
/// editarse después desde el panel de administración.
///
/// <example>
/// <code>
/// var inserted = await InstitutionalPagesSeed.SeedAsync(
///     context,
///     new Dictionary&lt;string, string&gt;
///     {
///         [InstitutionalPagesSeed.Dominio] = "https://www.ejemplo.es",
///         [InstitutionalPagesSeed.RazonSocial] = "Ejemplo Solar S.L.",
///         [InstitutionalPagesSeed.CifNif] = "B-00000000",
///         [InstitutionalPagesSeed.DomicilioFiscal] = "Calle Ejemplo 1, 28001 Madrid",
///         [InstitutionalPagesSeed.EmailContacto] = "contacto@ejemplo.es",
///         [InstitutionalPagesSeed.Telefono] = "+34 910 000 000",
///         [InstitutionalPagesSeed.RegistroMercantil] = "Registro Mercantil de Madrid, Tomo 1, Folio 1, Hoja M-1",
///         [InstitutionalPagesSeed.DelegadoProteccionDatos] = "No designado",
///         [InstitutionalPagesSeed.FechaActualizacion] = "17 de noviembre de 2026"
///     });
/// </code>
/// </example>
/// </remarks>
public static class InstitutionalPagesSeed
{
    public const string Dominio = "{{DOMINIO}}";
    public const string RazonSocial = "{{RAZON_SOCIAL}}";
    public const string CifNif = "{{CIF_NIF}}";
    public const string DomicilioFiscal = "{{DOMICILIO_FISCAL}}";
    public const string EmailContacto = "{{EMAIL_CONTACTO}}";
    public const string Telefono = "{{TELEFONO}}";
    public const string RegistroMercantil = "{{REGISTRO_MERCANTIL}}";
    public const string DelegadoProteccionDatos = "{{DELEGADO_PROTECCION_DATOS}}";
    public const string FechaActualizacion = "{{FECHA_ACTUALIZACION}}";

    /// <summary>
    /// Marcadores disponibles y qué dato debe introducirse en cada uno.
    /// Es la lista de partes editables de la plantilla.
    /// </summary>
    public static IReadOnlyDictionary<string, string> EditableFields { get; } =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Dominio] = "Dominio público del portal, con protocolo (por ejemplo, https://www.ejemplo.es).",
            [RazonSocial] = "Razón social o nombre del titular del sitio web.",
            [CifNif] = "CIF o NIF del titular.",
            [DomicilioFiscal] = "Domicilio fiscal completo del titular.",
            [EmailContacto] = "Correo electrónico de contacto para consultas y ejercicio de derechos.",
            [Telefono] = "Teléfono de contacto del titular.",
            [RegistroMercantil] = "Datos registrales del titular (registro, tomo, folio y hoja).",
            [DelegadoProteccionDatos] = "Delegado de Protección de Datos, si procede.",
            [FechaActualizacion] = "Fecha de la última revisión de los documentos legales."
        };

    /// <summary>
    /// Crea las cuatro páginas institucionales conservando los marcadores editables.
    /// </summary>
    public static IReadOnlyList<InformativePage> CreateTemplate()
        => Build(new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>
    /// Crea las cuatro páginas institucionales sustituyendo los marcadores indicados.
    /// Los marcadores que no se envíen se conservan tal cual.
    /// </summary>
    public static IReadOnlyList<InformativePage> CreateTemplate(IReadOnlyDictionary<string, string> values)
        => Build(values ?? throw new ArgumentNullException(nameof(values)));

    /// <summary>
    /// Siembra las páginas institucionales que aún no existan, identificadas por slug.
    /// Es una operación manual e idempotente: no modifica páginas ya publicadas.
    /// </summary>
    /// <returns>Número de páginas insertadas.</returns>
    public static async Task<int> SeedAsync(
        MongoDbContext context,
        IReadOnlyDictionary<string, string> values,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(context);

        var resolvedValues = new Dictionary<string, string>(
            values ?? new Dictionary<string, string>(),
            StringComparer.Ordinal);

        if (!resolvedValues.ContainsKey(FechaActualizacion))
            resolvedValues[FechaActualizacion] = DateTime.UtcNow.ToString("d 'de' MMMM 'de' yyyy", new System.Globalization.CultureInfo("es-ES"));

        var inserted = 0;
        foreach (var page in CreateTemplate(resolvedValues))
        {
            var exists = await context.Pages
                .Find(Builders<InformativePage>.Filter.Eq(p => p.Slug, page.Slug))
                .AnyAsync(cancellationToken);

            if (exists) continue;

            page.ValidarInvariantes();
            await context.Pages.InsertOneAsync(page, cancellationToken: cancellationToken);
            inserted++;
        }

        return inserted;
    }

    private static IReadOnlyList<InformativePage> Build(IReadOnlyDictionary<string, string> values)
        =>
        [
            AvisoLegal(values),
            PoliticaPrivacidad(values),
            PoliticaCookies(values),
            TerminosUso(values)
        ];

    private static InformativePage AvisoLegal(IReadOnlyDictionary<string, string> values) => new()
    {
        Title = "Aviso Legal",
        Slug = "aviso-legal",
        Subtitle = "Información sobre el titular del sitio, las condiciones de uso y el carácter informativo de los contenidos sobre energía solar.",
        Order = 1,
        ShowInFooter = true,
        Blocks = ResolveBlocks(values,
        [
            Paragraph($"El presente sitio web, accesible en <strong>{Dominio}</strong>, es un portal de contenido informativo y editorial especializado en energía solar, autoconsumo y eficiencia energética en España. La navegación por el sitio atribuye la condición de usuario e implica la aceptación de este Aviso Legal."),

            Heading("1. Datos identificativos del titular"),
            Paragraph($"<strong>Titular:</strong> {RazonSocial} · <strong>CIF/NIF:</strong> {CifNif} · <strong>Domicilio:</strong> {DomicilioFiscal} · <strong>Correo electrónico:</strong> {EmailContacto} · <strong>Teléfono:</strong> {Telefono} · <strong>Datos registrales:</strong> {RegistroMercantil}."),

            Heading("2. Objeto del sitio web"),
            Paragraph("El sitio tiene como finalidad difundir información, guías y análisis sobre energía solar en España, incluyendo autoconsumo, instalaciones fotovoltaicas, ayudas y marcos regulatorios. El acceso es gratuito y no exige registro, salvo para participar en comentarios o recibir comunicaciones."),
            Bullets(
                "Publicar contenidos editoriales de carácter informativo y divulgativo.",
                "Facilitar la comprensión de conceptos técnicos, económicos y normativos del sector solar.",
                "Ofrecer espacios de participación moderados, como comentarios, cuando estén habilitados."),

            Heading("3. Condiciones de acceso y uso"),
            Paragraph("El usuario se compromete a utilizar el sitio conforme a la ley, la buena fe y el orden público. Queda prohibido cualquier uso que pueda dañar la disponibilidad, integridad o seguridad del portal, así como la reproducción no autorizada de sus contenidos."),
            Bullets(
                "No introducir programas, virus o código malicioso.",
                "No intentar acceder a áreas restringidas sin autorización.",
                "No suplantar la identidad de terceros ni difundir información falsa.",
                "No realizar extracción masiva de contenidos sin consentimiento expreso."),

            Heading("4. Contenidos informativos y ausencia de asesoramiento"),
            Callout(
                "Aviso importante",
                "La información publicada tiene carácter divulgativo y no constituye asesoramiento técnico, jurídico, fiscal o financiero personalizado. Antes de contratar o instalar una solución fotovoltaica, consulte con un profesional cualificado y verifique la normativa vigente.",
                CalloutVariant.Warning),
            Paragraph("Las estimaciones de producción, ahorro o amortización son orientativas y dependen de la ubicación, la orientación, el consumo, las tarifas, la potencia instalada y los cambios normativos. El titular no garantiza resultados concretos ni se responsabiliza de decisiones adoptadas exclusivamente a partir de estos contenidos."),

            Heading("5. Propiedad intelectual e industrial"),
            Paragraph("Los textos, diseños, marcas, logotipos, imágenes y demás elementos del sitio están protegidos por la normativa de propiedad intelectual e industrial. Su reproducción, distribución o transformación requiere autorización previa y por escrito del titular, salvo los usos permitidos legalmente."),

            Heading("6. Enlaces a sitios de terceros"),
            Paragraph("El portal puede incluir enlaces a webs de terceros, organismos públicos o fabricantes. El titular no controla sus contenidos ni sus políticas y no asume responsabilidad por ellos. La inclusión de un enlace no implica recomendación ni relación comercial."),

            Heading("7. Exclusión de responsabilidad"),
            Paragraph("El titular adopta medidas razonables para mantener la información actualizada y el servicio disponible, pero no garantiza la ausencia de errores, interrupciones o inexactitudes. En la medida permitida por la ley, no será responsable de daños derivados del uso del sitio o de la confianza depositada en sus contenidos."),

            Heading("8. Protección de datos y cookies"),
            Paragraph("El tratamiento de datos personales se rige por la <a href=\"/info/politica-de-privacidad\">Política de Privacidad</a> y el uso de cookies por la <a href=\"/info/politica-de-cookies\">Política de Cookies</a>. Ambos documentos forman parte integrante de este Aviso Legal."),

            Heading("9. Legislación aplicable y resolución de conflictos"),
            Paragraph($"Este Aviso Legal se rige por la legislación española. Para consumidores, serán competentes los juzgados y tribunales del domicilio del consumidor y podrán acudir a los organismos de consumo o al sistema arbitral de consumo. Para el resto de usuarios, las partes se someten a los juzgados y tribunales de {DomicilioFiscal}."),

            Heading("10. Modificaciones"),
            Paragraph("El titular podrá modificar este Aviso Legal para adaptarlo a cambios legislativos, técnicos o de negocio. La versión vigente será la publicada en el sitio, con indicación de la fecha de actualización."),
            Paragraph($"<em>Última actualización: {FechaActualizacion}.</em>")
        ])
    };

    private static InformativePage PoliticaPrivacidad(IReadOnlyDictionary<string, string> values) => new()
    {
        Title = "Política de Privacidad",
        Slug = "politica-de-privacidad",
        Subtitle = "Cómo tratamos tus datos personales cuando visitas el portal, te suscribes o participas en comentarios.",
        Order = 2,
        ShowInFooter = true,
        Blocks = ResolveBlocks(values,
        [
            Paragraph($"Esta política explica qué datos personales tratamos, con qué finalidad, durante cuánto tiempo y cómo puedes ejercer tus derechos. Se aplica a la navegación por <strong>{Dominio}</strong> y a los formularios o canales de contacto habilitados."),

            Heading("1. Responsable del tratamiento"),
            Paragraph($"<strong>Responsable:</strong> {RazonSocial} · <strong>CIF/NIF:</strong> {CifNif} · <strong>Domicilio:</strong> {DomicilioFiscal} · <strong>Correo:</strong> {EmailContacto}. <strong>Delegado de Protección de Datos:</strong> {DelegadoProteccionDatos} (si procede)."),

            Heading("2. Datos personales que tratamos"),
            Paragraph("Tratamos únicamente los datos necesarios para cada finalidad. Podemos recoger las siguientes categorías:"),
            Bullets(
                "Datos identificativos y de contacto: nombre, correo electrónico y teléfono.",
                "Contenido de las comunicaciones: mensajes, consultas y datos que decidas incluir.",
                "Datos de participación: nombre visible, correo y contenido de los comentarios.",
                "Datos técnicos de navegación: dirección IP, navegador, dispositivo y páginas visitadas.",
                "Preferencias de consentimiento: aceptación o rechazo de cookies y comunicaciones."),

            Heading("3. Finalidades, bases jurídicas y conservación"),
            Table(
                ["Finalidad", "Datos", "Base jurídica", "Conservación"],
                [
                    ["Responder consultas", "Nombre, correo, mensaje", "Consentimiento o interés legítimo", "1 año desde la última interacción"],
                    ["Enviar boletín informativo", "Correo y preferencias", "Consentimiento", "Hasta la baja o 2 años sin aperturas"],
                    ["Gestionar comentarios", "Nombre, correo, contenido", "Consentimiento", "Mientras esté publicado o se solicite su retirada"],
                    ["Analítica web agregada", "Datos técnicos y de navegación", "Consentimiento", "Según la Política de Cookies"],
                    ["Cumplir obligaciones legales", "Datos necesarios", "Obligación legal", "Plazos legales aplicables"]
                ],
                "Resumen de los tratamientos de datos personales del portal."),

            Heading("4. Destinatarios y encargados del tratamiento"),
            Paragraph("No vendemos ni cedemos datos personales a terceros con fines comerciales. Podrán acceder a ellos los proveedores que prestan servicios al portal, bajo contrato de encargo de tratamiento y con las garantías exigidas por el RGPD:"),
            Bullets(
                "Proveedor de alojamiento y mantenimiento del sitio web.",
                "Proveedor de correo electrónico y envío de boletines.",
                "Proveedor de analítica y medición de audiencia.",
                "Asesores legales, fiscales o técnicos cuando sea necesario.",
                "Administraciones públicas y juzgados cuando exista obligación legal."),

            Heading("5. Transferencias internacionales"),
            Paragraph("Con carácter general, los datos se tratan dentro del Espacio Económico Europeo. Si algún proveedor implica una transferencia internacional, se aplicarán decisiones de adecuación de la Comisión Europea o garantías apropiadas, como las cláusulas contractuales tipo, junto con medidas complementarias si fueran necesarias."),

            Heading("6. Derechos de las personas interesadas"),
            Paragraph($"Puedes ejercer los siguientes derechos escribiendo a <strong>{EmailContacto}</strong>, indicando el derecho que ejercitas y acreditando tu identidad:"),
            Bullets(
                "Acceso a los datos personales que tratamos sobre ti.",
                "Rectificación de datos inexactos o incompletos.",
                "Supresión de los datos cuando ya no sean necesarios o el tratamiento sea ilícito.",
                "Oposición al tratamiento basado en interés legítimo o para comunicaciones comerciales.",
                "Limitación del tratamiento en los supuestos previstos por el RGPD.",
                "Portabilidad de los datos que nos hayas facilitado.",
                "Retirada del consentimiento en cualquier momento, sin afectar al tratamiento previo."),
            Paragraph("Si consideras que no hemos atendido correctamente tu solicitud, puedes reclamar ante la Agencia Española de Protección de Datos a través de <a href=\"https://www.aepd.es\" title=\"Agencia Española de Protección de Datos\" target=\"_blank\" rel=\"noopener noreferrer\">www.aepd.es</a>."),

            Heading("7. Seguridad de los datos"),
            Paragraph("Aplicamos medidas técnicas y organizativas razonables para proteger los datos frente a pérdida, acceso no autorizado, alteración o divulgación. Entre ellas se incluyen el cifrado del sitio mediante HTTPS, el control de accesos y la minimización de datos. Ningún sistema es completamente infalible, por lo que revisamos periódicamente nuestras medidas."),

            Heading("8. Menores de edad"),
            Paragraph("El portal no está dirigido a menores de 14 años. Si detectamos datos de un menor sin la autorización exigida, procederemos a su supresión. Los padres o tutores pueden contactarnos para solicitar la eliminación."),

            Heading("9. Cookies y tecnologías similares"),
            Paragraph("El sitio utiliza cookies técnicas necesarias para su funcionamiento. Puedes consultar el detalle y cómo eliminarlas en la <a href=\"/info/politica-de-cookies\">Política de Cookies</a>."),

            Heading("10. Actualizaciones de esta política"),
            Paragraph("Podemos actualizar esta política para reflejar cambios normativos o en nuestros tratamientos. Publicaremos la versión vigente con su fecha de actualización y, si los cambios son sustanciales, lo comunicaremos por los canales habituales."),
            Paragraph($"<em>Última actualización: {FechaActualizacion}.</em>")
        ])
    };

    private static InformativePage PoliticaCookies(IReadOnlyDictionary<string, string> values) => new()
    {
        Title = "Política de Cookies",
        Slug = "politica-de-cookies",
        Subtitle = "Qué cookies utiliza el portal, para qué sirven y cómo puedes eliminarlas.",
        Order = 3,
        ShowInFooter = true,
        Blocks = ResolveBlocks(values,
        [
            Paragraph($"Esta política describe el uso de cookies y tecnologías similares en <strong>{Dominio}</strong>, de acuerdo con el artículo 22.2 de la LSSI-CE y las directrices de la Agencia Española de Protección de Datos."),

            Heading("1. ¿Qué son las cookies?"),
            Paragraph("Son pequeños archivos que se almacenan en tu dispositivo al visitar un sitio web. Permiten recordar preferencias, mantener la seguridad, medir la audiencia o mostrar contenidos personalizados. También pueden utilizarse tecnologías equivalentes, como el almacenamiento local del navegador."),

            Heading("2. Base legal y consentimiento"),
            Paragraph("Este portal utiliza únicamente cookies técnicas necesarias para su funcionamiento y para proteger la seguridad de los formularios. No se instalan cookies analíticas, publicitarias ni de elaboración de perfiles, por lo que no es necesario recabar consentimiento para su uso. El aviso de cookies es informativo y puedes eliminar las cookies en cualquier momento desde tu navegador."),
            Callout(
                "Sin cookies opcionales",
                "La aceptación del aviso se guarda únicamente en el almacenamiento local del navegador y no se comparte con terceros. Si en el futuro se incorporan cookies opcionales, se solicitará el consentimiento previo con opciones para aceptar, rechazar o configurar por categorías."),

            Heading("3. Tipos de cookies utilizadas"),
            Table(
                ["Categoría", "Finalidad", "Ejemplos", "Duración", "Titular"],
                [
                    ["Técnicas necesarias", "Mantener la sesión del panel de administración y proteger los formularios frente a envíos fraudulentos (CSRF)", "Cookie de autenticación y cookie antiforgery", "Hasta 8 horas o fin de la sesión", "Propio"],
                    ["Preferencias", "Recordar que has visto el aviso de cookies", "Almacenamiento local: cookies_accepted", "Hasta borrar los datos del navegador", "Propio"],
                    ["Analítica", "No se utilizan cookies analíticas", "—", "—", "—"],
                    ["Marketing", "No se utilizan cookies publicitarias ni de perfilado", "—", "—", "—"]
                ],
                "Inventario de cookies y tecnologías similares utilizadas actualmente por el portal."),

            Heading("4. Cookies de terceros"),
            Paragraph("El portal no instala cookies de terceros. Si en el futuro se incorporan servicios externos, como vídeos incrustados o herramientas de medición, se detallarán en la tabla anterior y, cuando sea necesario, se solicitará el consentimiento previo."),

            Heading("5. Cómo gestionar o eliminar cookies"),
            Paragraph("Puedes eliminar o bloquear las cookies desde la configuración de tu navegador. Ten en cuenta que bloquear las cookies técnicas puede impedir el acceso al panel de administración. Enlaces de ayuda:"),
            Bullets(
                "<a href=\"https://support.google.com/chrome/answer/95647\" title=\"Gestionar cookies en Chrome\" target=\"_blank\" rel=\"noopener noreferrer\">Google Chrome</a>",
                "<a href=\"https://support.mozilla.org/es/kb/borrar-cookies-y-datos-de-sitios-en-firefox\" title=\"Gestionar cookies en Firefox\" target=\"_blank\" rel=\"noopener noreferrer\">Mozilla Firefox</a>",
                "<a href=\"https://support.apple.com/es-es/guide/safari/sfri11471/mac\" title=\"Gestionar cookies en Safari\" target=\"_blank\" rel=\"noopener noreferrer\">Safari</a>",
                "<a href=\"https://support.microsoft.com/es-es/windows/eliminar-y-administrar-cookies-168dab11-0753-043d-7c16-ede5947fc64d\" title=\"Gestionar cookies en Edge\" target=\"_blank\" rel=\"noopener noreferrer\">Microsoft Edge</a>"),

            Heading("6. Actualización de esta política"),
            Paragraph("Esta política puede cambiar si se modifican las cookies utilizadas o la normativa aplicable. Publicaremos la versión vigente y su fecha de actualización. Si los cambios afectan a cookies opcionales, solicitaremos de nuevo el consentimiento cuando sea necesario."),
            Paragraph($"<em>Última actualización: {FechaActualizacion}.</em>")
        ])
    };

    private static InformativePage TerminosUso(IReadOnlyDictionary<string, string> values) => new()
    {
        Title = "Términos de Uso",
        Slug = "terminos-de-uso",
        Subtitle = "Reglas de uso del portal, participación en comentarios, propiedad intelectual y limitaciones de responsabilidad.",
        Order = 4,
        ShowInFooter = true,
        Blocks = ResolveBlocks(values,
        [
            Paragraph($"Los presentes Términos de Uso regulan el acceso y la utilización de <strong>{Dominio}</strong>. Al navegar o utilizar sus servicios, aceptas estas condiciones. Si no estás de acuerdo, debes abstenerte de utilizar el sitio."),

            Heading("1. Naturaleza del servicio"),
            Paragraph("El portal ofrece contenidos informativos y editoriales sobre energía solar en España. Salvo que se indique expresamente, no comercializa instalaciones ni celebra contratos de suministro o mantenimiento. Cuando existan enlaces a tiendas o profesionales, se identificará su carácter externo."),
            Callout(
                "Contenido divulgativo",
                "La información no sustituye un estudio técnico, la revisión de la normativa aplicable ni el asesoramiento de un profesional cualificado."),

            Heading("2. Uso permitido del sitio"),
            Paragraph("Puedes consultar, imprimir y compartir los contenidos para uso personal o informativo, citando la fuente. No se permite su explotación comercial sin autorización ni la realización de actuaciones que perjudiquen el funcionamiento del portal."),
            Bullets(
                "No utilizar el sitio para fines ilícitos o contrarios a la buena fe.",
                "No extraer contenidos de forma masiva ni automatizada sin autorización.",
                "No introducir código malicioso ni interferir en la seguridad del servicio.",
                "No publicar datos personales de terceros sin su consentimiento.",
                "No suplantar la identidad de personas u organizaciones."),

            Heading("3. Comentarios y participación"),
            Paragraph("Los comentarios son moderados para mantener un debate respetuoso. El portal puede retirar, sin previo aviso, contenidos que incumplan estas reglas o la legislación vigente. La moderación no implica una revisión exhaustiva de todas las opiniones publicadas."),
            Bullets(
                "No se admiten insultos, amenazas, acoso ni discurso de odio.",
                "No se permite spam, publicidad no solicitada ni enlaces engañosos.",
                "No se toleran afirmaciones falsas sobre productos, ayudas o normativa.",
                "El usuario es responsable de la veracidad y licitud de sus aportaciones."),

            Heading("4. Contenidos y propiedad intelectual"),
            Paragraph("Los textos, gráficos, tablas, marcas y elementos visuales del portal están protegidos por la normativa de propiedad intelectual e industrial. Las citas de terceros se atribuyen a sus autores. La reproducción total o parcial requiere autorización, salvo los supuestos legalmente permitidos."),

            Heading("5. Exactitud de la información y previsiones de ahorro"),
            Paragraph("Las guías y comparativas se elaboran con rigor, pero pueden contener errores o quedar desactualizadas. Las cifras de producción, ahorro, subvenciones o amortización son estimaciones y no constituyen una oferta ni una garantía contractual. Verifica siempre las condiciones vigentes con el instalador, la comercializadora o el organismo competente."),

            Heading("6. Enlaces y servicios de terceros"),
            Paragraph("El portal puede enlazar a webs, herramientas o servicios de terceros. No controlamos sus contenidos ni sus condiciones, y no asumimos responsabilidad por ellos. La contratación con terceros se realiza bajo tu responsabilidad y conforme a sus propias políticas."),

            Heading("7. Disponibilidad y responsabilidad"),
            Paragraph("Intentamos mantener el sitio disponible y seguro, pero no garantizamos la ausencia de interrupciones, errores o pérdida de información. En la medida permitida por la ley, no seremos responsables de daños indirectos derivados del uso del portal o de la confianza en sus contenidos."),

            Heading("8. Protección de datos"),
            Paragraph("El tratamiento de datos personales se describe en la <a href=\"/info/politica-de-privacidad\">Política de Privacidad</a>. El uso de cookies se detalla en la <a href=\"/info/politica-de-cookies\">Política de Cookies</a>."),

            Heading("9. Legislación aplicable y resolución de conflictos"),
            Paragraph($"Estos Términos se rigen por la legislación española. Los consumidores conservan los derechos que les reconoce el Texto Refundido de la Ley General para la Defensa de los Consumidores y Usuarios y podrán acudir a las juntas arbitrales de consumo o a los organismos de consumo competentes. Para el resto de usuarios, las partes se someten a los juzgados y tribunales de {DomicilioFiscal}."),

            Heading("10. Modificaciones y nulidad parcial"),
            Paragraph("Podemos modificar estos Términos para adaptarlos a cambios legales o funcionales. Si alguna cláusula resultara nula o inaplicable, el resto conservará su validez. La versión vigente será la publicada en el sitio."),
            Paragraph($"<em>Última actualización: {FechaActualizacion}.</em>")
        ])
    };

    private static List<ContentBlock> ResolveBlocks(
        IReadOnlyDictionary<string, string> values,
        List<ContentBlock> blocks)
        => blocks.Select(block => ResolveBlock(block, values)).ToList();

    private static ContentBlock ResolveBlock(ContentBlock block, IReadOnlyDictionary<string, string> values)
        => block switch
        {
            ContentParagraphBlock paragraph => new ContentParagraphBlock
            {
                Text = ResolveRichText(paragraph.Text, values)
            },
            ContentHeadingBlock heading => new ContentHeadingBlock
            {
                Level = heading.Level,
                Text = ResolvePlainText(heading.Text, values)
            },
            ContentOrderedListBlock list => new ContentOrderedListBlock
            {
                Items = list.Items.Select(item => ResolveRichText(item, values)).ToList()
            },
            ContentBulletListBlock list => new ContentBulletListBlock
            {
                Items = list.Items.Select(item => ResolveRichText(item, values)).ToList()
            },
            ContentQuoteBlock quote => new ContentQuoteBlock
            {
                Text = ResolveRichText(quote.Text, values),
                Attribution = quote.Attribution is null ? null : ResolvePlainText(quote.Attribution, values)
            },
            ContentCalloutBlock callout => new ContentCalloutBlock
            {
                Variant = callout.Variant,
                Title = callout.Title is null ? null : ResolvePlainText(callout.Title, values),
                Text = ResolveRichText(callout.Text, values)
            },
            ContentImageBlock image => new ContentImageBlock
            {
                ImageId = image.ImageId,
                Url = image.Url,
                Alt = ResolvePlainText(image.Alt, values),
                Caption = image.Caption is null ? null : ResolvePlainText(image.Caption, values),
                Credit = image.Credit is null ? null : ResolvePlainText(image.Credit, values),
                Width = image.Width,
                Height = image.Height
            },
            ContentTableBlock table => new ContentTableBlock
            {
                Headers = table.Headers.Select(header => ResolvePlainText(header, values)).ToList(),
                Rows = table.Rows
                    .Select(row => row.Select(cell => ResolvePlainText(cell, values)).ToList())
                    .ToList(),
                Caption = table.Caption is null ? null : ResolvePlainText(table.Caption, values)
            },
            ContentCodeBlock code => new ContentCodeBlock
            {
                Code = ResolvePlainText(code.Code, values),
                Language = code.Language is null ? null : ResolvePlainText(code.Language, values)
            },
            ContentDividerBlock => new ContentDividerBlock(),
            _ => block
        };

    private static RichText ResolveRichText(RichText text, IReadOnlyDictionary<string, string> values)
        => RichText.Create(
            ReplaceTokens(text.Html, values),
            ReplaceTokens(text.Text, values));

    private static PlainText ResolvePlainText(PlainText text, IReadOnlyDictionary<string, string> values)
        => PlainText.Create(ReplaceTokens(text.Value, values));

    private static string ReplaceTokens(string value, IReadOnlyDictionary<string, string> values)
    {
        if (string.IsNullOrEmpty(value) || values.Count == 0)
            return value;

        foreach (var (token, replacement) in values)
        {
            if (!string.IsNullOrEmpty(token) && replacement is not null)
                value = value.Replace(token, replacement, StringComparison.Ordinal);
        }

        return value;
    }

    private static ContentParagraphBlock Paragraph(string html, string? plain = null)
        => new() { Text = RichText.Create(html, plain ?? ToPlainText(html)) };

    private static ContentHeadingBlock Heading(string text)
        => new() { Level = 2, Text = PlainText.Create(text) };

    private static ContentBulletListBlock Bullets(params string[] items)
        => new()
        {
            Items = items
                .Select(item => RichText.Create(item, ToPlainText(item)))
                .ToList()
        };

    private static ContentCalloutBlock Callout(
        string title,
        string html,
        CalloutVariant variant = CalloutVariant.Info)
        => new()
        {
            Variant = variant,
            Title = PlainText.Create(title),
            Text = RichText.Create(html, ToPlainText(html))
        };

    private static ContentTableBlock Table(
        string[] headers,
        string[][] rows,
        string caption)
        => new()
        {
            Headers = headers.Select(header => PlainText.Create(header)).ToList(),
            Rows = rows
                .Select(row => row.Select(cell => PlainText.Create(cell)).ToList())
                .ToList(),
            Caption = PlainText.Create(caption)
        };

    private static string ToPlainText(string html)
    {
        var withoutTags = Regex.Replace(html, "<[^>]+>", string.Empty);
        return WebUtility.HtmlDecode(withoutTags)
            .Replace('\u00A0', ' ')
            .Trim();
    }
}
