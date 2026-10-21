using Domine.Entities;
using MongoDB.Driver;

namespace Infrastructure.Persistence;

/// <summary>
/// Provee datos de prueba completos, agnósticos y enriquecidos para el portal.
/// Genera categorías con subcategorías jerárquicas y mínimo 10 artículos completos
/// con bloques editoriales: subtítulos, listas, tablas comparativas, citas y callouts.
/// </summary>
public static class SeedArticlesData
{
    public static async Task SeedAsync(MongoDbContext context)
    {
        // 1. Eliminar artículos viejos por defecto si existen
        var oldSlugs = new[]
        {
            "revolucion-inteligencia-artificial-nueva-era",
            "mercados-globales-perspectivas-financieras",
            "preservacion-patrimonio-espacios-culturales"
        };
        await context.Articles.DeleteManyAsync(Builders<Article>.Filter.In(a => a.Slug, oldSlugs));

        // 2. Si ya hay 10 o más artículos en el sistema, no sobrecargar
        var currentArticleCount = await context.Articles.CountDocumentsAsync(Builders<Article>.Filter.Empty);
        if (currentArticleCount >= 10) return;

        // 3. Crear o recuperar categorías y subcategorías temáticas variadas
        var catCiencia = await EnsureCategoryAsync(context, "Ciencia y Exploración", "ciencia-exploracion", "Descubrimientos del universo, física y ciencias biológicas.", 1);
        var subAstro = await EnsureCategoryAsync(context, "Astrofísica", "astrofisica", "Exploración del cosmos, exoplanetas y misiones espaciales.", 1, catCiencia.Id);
        var subBio = await EnsureCategoryAsync(context, "Biotecnología", "biotecnologia", "Ingeniería genética, biología sintética y biomedicina.", 2, catCiencia.Id);

        var catSalud = await EnsureCategoryAsync(context, "Salud y Bienestar", "salud-bienestar", "Neurociencia, medicina preventiva, nutrición y estilo de vida.", 2);
        var subNeuro = await EnsureCategoryAsync(context, "Neurociencia", "neurociencia", "Plasticidad cerebral, cognición y salud mental.", 1, catSalud.Id);
        var subNutri = await EnsureCategoryAsync(context, "Nutrición y Longevidad", "nutricion-longevidad", "Metabolismo, microbioma y hábitos para una vida longeva.", 2, catSalud.Id);

        var catHabitat = await EnsureCategoryAsync(context, "Hábitat y Sostenibilidad", "habitat-sostenibilidad", "Urbanismo ecológico, energías limpias y arquitectura bioclimática.", 3);
        var subArqui = await EnsureCategoryAsync(context, "Arquitectura Bioclimática", "arquitectura-bioclimatica", "Diseño pasivo, estructuras de madera y materiales sostenibles.", 1, catHabitat.Id);
        var subEnergia = await EnsureCategoryAsync(context, "Energías Limpias", "energias-limpias", "Almacenamiento de energía, redes solares y transición renovable.", 2, catHabitat.Id);

        var catCultura = await EnsureCategoryAsync(context, "Cultura y Sociedad", "cultura-sociedad", "Expresiones artísticas, cine, música y pensamiento contemporáneo.", 4);
        var subCine = await EnsureCategoryAsync(context, "Cine y Audiovisual", "cine-audiovisual", "Lenguaje cinematográfico, dirección de fotografía y narrativa.", 1, catCultura.Id);
        var subMusica = await EnsureCategoryAsync(context, "Música y Acústica", "musica-acustica", "Física sonora, diseño de salas y producción musical.", 2, catCultura.Id);

        var catGastro = await EnsureCategoryAsync(context, "Gastronomía", "gastronomia", "Cultura culinaria, ciencia de alimentos y productos de origen.", 5);
        var subCafe = await EnsureCategoryAsync(context, "Café de Especialidad", "cafe-especialidad", "Origen, procesos de fermentación y química de extracción.", 1, catGastro.Id);
        var subAltaCocina = await EnsureCategoryAsync(context, "Alta Cocina", "alta-cocina", "Técnicas de vanguardia, patrimonio gastronómico y técnica.", 2, catGastro.Id);

        // 4. Colección de 12 Artículos de Prueba Completos con Bloques Editoriales
        var testArticles = new List<Article>
        {
            // ─── 1. Astrofísica (Ciencia) ──────────────────────────────────────────
            new()
            {
                Title = "Telescopios de Nueva Generación y el Mapeo de Exoplanetas Habitables",
                Slug = "telescopios-nueva-generacion-exoplanetas-habitables",
                Summary = "Los observatorios infrarrojos avanzados detectan firmas atmosféricas de vapor de agua y metano en mundos fuera de nuestro sistema solar.",
                Content = @"<h2>1. La Nueva Cartografía Cósmica</h2>
<p>La astronomía observacional experimenta su mayor revolución en medio siglo. Gracias a espectrómetros criogénicos de ultra alta resolución, la detección de mundos rocosos en la zona habitable de enanas rojas ha pasado de ser una posibilidad teórica a una rutina científica rigurosa.</p>

<div class=""callout callout-info"" data-callout=""info"">
<div class=""callout-body""><strong>Hito astronómico:</strong> En los últimos 18 meses se han catalogado más de 140 exoplanetas con atmósferas que presentan estabilidad térmica y presencia potencial de agua líquida.</div>
</div>

<h2>2. Biomarcadores Espectroscópicos</h2>
<p>Para determinar si una atmósfera alberga actividad biológica o equilibrio prebiótico, los astrofísicos monitorean las siguientes bandas espectrales:</p>

<ol class=""article-ordered-list"">
<li><strong>Banda de absorción de agua (1.4 y 1.9 µm):</strong> Confirma condensación en capas intermedias.</li>
<li><strong>Desequilibrio metano-dióxido de carbono:</strong> Señal primaria de actividad geoquímica o biológica.</li>
<li><strong>Efecto invernadero natural:</strong> Regulación térmica propicia para superficies estables.</li>
</ol>

<figure class=""article-figure"">
<img src=""https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=1200&q=80"" alt=""Vista satelital de la atmósfera y el espacio profundo"" loading=""lazy"" />
<figcaption>Reconstrucción óptica de tránsitos planetarios en sistemas solares cercanos.</figcaption>
</figure>

<h2>3. Comparativa de Misiones Observacionales</h2>
<div class=""table-responsive-wrapper"">
<table class=""article-table"">
<thead>
<tr>
<th>Instrumento</th>
<th>Longitud de Onda</th>
<th>Apertura</th>
<th>Objetivo Principal</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>JWST (Infrarrojo)</strong></td>
<td>0.6 a 28 µm</td>
<td>6.5 m</td>
<td>Atmósferas exoplanetarias tempranas</td>
</tr>
<tr>
<td><strong>ELT (Tierra)</strong></td>
<td>Óptico / Infrarrojo</td>
<td>39.3 m</td>
<td>Imagen directa de planetas rocosos</td>
</tr>
<tr>
<td><strong>Nancy Grace Roman</strong></td>
<td>0.5 a 2.0 µm</td>
<td>2.4 m</td>
<td>Sondeo de campo amplio y microlentes</td>
</tr>
</tbody>
</table>
</div>

<blockquote>
<p>“Mirar la firma luminosa de una atmósfera a cuarenta años luz es aprender a escuchar el pulso químico de la galaxia.”</p>
<cite>Dra. Claudia Benítez, Instituto de Ciencias Espaciales</cite>
</blockquote>",
                Author = "Martín Valenzuela",
                AuthorRole = "Divulgador Científico",
                AuthorBio = "Físico e investigador en instrumentación astronómica.",
                CategoryId = catCiencia.Id,
                SubcategoryId = subAstro.Id,
                Tags = ["Ciencia", "Astrofísica", "Espacio", "Astronomía"],
                CoverImageUrl = "https://images.unsplash.com/photo-1451187580459-43490279c0fa?auto=format&fit=crop&w=1200&q=80",
                Featured = true,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-3),
                CreatedAt = DateTime.UtcNow.AddHours(-3),
                UpdatedAt = DateTime.UtcNow.AddHours(-3),
                ReadingTimeMinutes = 5,
                ViewCount = 210
            },

            // ─── 2. Biotecnología (Ciencia) ────────────────────────────────────────
            new()
            {
                Title = "Edición Genética de Precisión y la Nueva Era de la Terapia Celular",
                Slug = "edicion-genetica-precision-terapia-celular",
                Summary = "Los editores epigenéticos y las herramientas moleculares de segunda generación logran corregir mutaciones sin cortar la doble hebra de ADN.",
                Content = @"<h2>1. Superando el Corte Tradicional</h2>
<p>La biotecnología molecular ha dado un salto decisivo. Las nuevas enzimas modificadas permiten el cambio preciso de una base nucleótida sin generar rupturas de doble cadena, minimizando drásticamente los efectos colaterales no deseados.</p>

<div class=""callout callout-success"" data-callout=""success"">
<div class=""callout-body""><strong>Eficacia demostrada:</strong> En ensayos clínicos recientes, los editores de bases lograron una tasa de corrección del 91% en células madre hematopoyéticas.</div>
</div>

<h2>2. Aplicaciones Terapéuticas Principales</h2>
<ul class=""article-unordered-list"">
<li><strong>Enfermedades raras monogénicas:</strong> Corrección in vivo en tejidos musculares y hepáticos.</li>
<li><strong>Inmunoterapia oncológica:</strong> Reprogramación de linfocitos T para resistencia al agotamiento tumoral.</li>
<li><strong>Agricultura regenerativa:</strong> Plantas con tolerancia térmica reforzada sin transgenes externos.</li>
</ul>

<blockquote>
<p>“La precisión molecular actual nos permite reprogramar dianas biológicas con la exactitud de un tipógrafo corrigiendo una sola errata en una enciclopedia.”</p>
<cite>Dra. Mónica Holguín, Laboratorio de Biología Sintética</cite>
</blockquote>",
                Author = "Dra. Sofía Alarcón",
                AuthorRole = "Editora de Biotecnología",
                CategoryId = catCiencia.Id,
                SubcategoryId = subBio.Id,
                Tags = ["Biotecnología", "Genética", "Ciencia", "Medicina"],
                CoverImageUrl = "https://images.unsplash.com/photo-1532187863486-abf9dbad1b69?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-7),
                CreatedAt = DateTime.UtcNow.AddHours(-7),
                UpdatedAt = DateTime.UtcNow.AddHours(-7),
                ReadingTimeMinutes = 4,
                ViewCount = 142
            },

            // ─── 3. Neurociencia (Salud) ───────────────────────────────────────────
            new()
            {
                Title = "Plasticidad Cerebral en Adultos: Cómo el Cerebro se Reconfigura Continuamente",
                Slug = "plasticidad-cerebral-adultos-reconfiguracion-continua",
                Summary = "Nuevos hallazgos en resonancia magnética funcional demuestran que las sinapsis adultas mantienen una notable capacidad de reorganización.",
                Content = @"<h2>1. El Fin del Dogma de la Estructura Rígida</h2>
<p>Durante décadas se creyó que tras la maduración juvenil los circuitos neuronales quedaban fijos. Hoy la evidencia científica demuestra que la mielinización dinámica y la neurogénesis en el giro dentado permanecen activas a lo largo de toda la vida.</p>

<h2>2. Factores de Estimulación Sináptica</h2>
<ol class=""article-ordered-list"">
<li><strong>Entrenamiento en habilidades motoras complejas:</strong> Estimula la densidad dendrítica en áreas parietales.</li>
<li><strong>Ejercicio aeróbico estructurado:</strong> Eleva los niveles de factor neurotrófico derivado del cerebro (BDNF).</li>
<li><strong>Sueño de ondas lentas:</strong> Consolida la poda de conexiones redundantes y fija la memoria a largo plazo.</li>
</ol>

<div class=""callout callout-info"" data-callout=""info"">
<div class=""callout-body""><strong>Dato neurológico:</strong> 40 minutos de actividad aeróbica moderada incrementan el flujo sanguíneo cerebral regional en más de un 15% durante las tres horas posteriores.</div>
</div>

<blockquote>
<p>“El cerebro no es una máquina de mármol que se desgasta, sino un ecosistema plástico que responde con fidelidad a lo que le exigimos cada día.”</p>
<cite>Dr. Andrés Pineda, Instituto de Neurociencias Aplicadas</cite>
</blockquote>",
                Author = "Dr. Andrés Pineda",
                AuthorRole = "Investigador en Neurobiología",
                CategoryId = catSalud.Id,
                SubcategoryId = subNeuro.Id,
                Tags = ["Salud", "Neurociencia", "Bienestar", "Cerebro"],
                CoverImageUrl = "https://images.unsplash.com/photo-1559757175-5700dde675bc?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-10),
                CreatedAt = DateTime.UtcNow.AddHours(-10),
                UpdatedAt = DateTime.UtcNow.AddHours(-10),
                ReadingTimeMinutes = 4,
                ViewCount = 189
            },

            // ─── 4. Nutrición y Longevidad (Salud) ─────────────────────────────────
            new()
            {
                Title = "Microbioma Intestinal y Longevidad: La Ciencia Detrás de la Nutrición Celular",
                Slug = "microbioma-intestinal-longevidad-nutricion-celular",
                Summary = "Los metabolitos producidos por bacterias benéficas regulan la inflamación sistémica, la salud cardiovascular y la respuesta metabólica.",
                Content = @"<h2>1. El Eje Intestino-Inmunidad</h2>
<p>El tracto digestivo aloja más de cien billones de microorganismos cuya diversidad funcional influye directamente en el perfil lipídico y la modulación del sistema inmune adaptativo.</p>

<h2>2. Ácidos Grasos de Cadena Corta (AGCC)</h2>
<p>La fermentación de fibras prebióticas produce butirato, acetato y propionato, moléculas mensajeras vitales para:</p>
<ul class=""article-unordered-list"">
<li>Fortalecer las uniones estrechas del epitelio intestinal.</li>
<li>Regular la sensibilidad periférica a la insulina.</li>
<li>Atenuar la inflamación de bajo grado asociada al envejecimiento.</li>
</ul>

<div class=""table-responsive-wrapper"">
<table class=""article-table"">
<thead>
<tr>
<th>Metabolito</th>
<th>Origen Dietario</th>
<th>Efecto Fisiológico Principal</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Butirato</strong></td>
<td>Fibras solubles y almidón resistente</td>
<td>Energía para colonocitos y acción antiinflamatoria</td>
</tr>
<tr>
<td><strong>Propionato</strong></td>
<td>Inulina y betaglucanos</td>
<td>Modulación de la gluconeogénesis hepática</td>
</tr>
<tr>
<td><strong>Polifenoles</strong></td>
<td>Frutos rojos, té verde y cacao puro</td>
<td>Protección antioxidante de la microbiota</td>
</tr>
</tbody>
</table>
</div>",
                Author = "Carla Mendoza",
                AuthorRole = "Especialista en Nutrición",
                CategoryId = catSalud.Id,
                SubcategoryId = subNutri.Id,
                Tags = ["Salud", "Nutrición", "Longevidad", "Bienestar"],
                CoverImageUrl = "https://images.unsplash.com/photo-1498837167922-ddd27525d352?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-14),
                CreatedAt = DateTime.UtcNow.AddHours(-14),
                UpdatedAt = DateTime.UtcNow.AddHours(-14),
                ReadingTimeMinutes = 4,
                ViewCount = 165
            },

            // ─── 5. Arquitectura Bioclimática (Hábitat) ───────────────────────────
            new()
            {
                Title = "Rascacielos de Madera y Estructuras Vivas: La Revolución del Urbanismo Bioclimático",
                Slug = "rascacielos-madera-estructuras-vivas-urbanismo-bioclimatico",
                Summary = "La madera contralaminada (CLT) y el diseño biofílico reducen la huella de carbono de las metrópolis mientras transforman la calidad espacial.",
                Content = @"<h2>1. Dejar Atrás el Monopolio del Hormigón</h2>
<p>La construcción masiva en madera contralaminada (Cross-Laminated Timber) permite levantar edificios de más de veinte plantas con una huella de carbono negativa, capturando toneladas de CO₂ en lugar de emitirlo durante el fraguado del hormigón.</p>

<div class=""callout callout-info"" data-callout=""info"">
<div class=""callout-body""><strong>Dato sostenible:</strong> Un metro cúbico de madera estructural secuestra aproximadamente una tonelada de dióxido de carbono equivalente durante todo su ciclo de vida útil.</div>
</div>

<h2>2. Principios del Diseño Pasivo Urbano</h2>
<ol class=""article-ordered-list"">
<li><strong>Ventilación cruzada inducida:</strong> Atrios centrales que generan tiro térmico natural sin consumo eléctrico.</li>
<li><strong>Fachadas vivas con vegetación nativa:</strong> Reducción de hasta 4°C en el efecto de isla de calor urbano.</li>
<li><strong>Captación solar orientada:</strong> Aleros calculados para sombrear en verano y calentar en invierno.</li>
</ol>

<blockquote>
<p>“Las ciudades del siglo XXI no deben verse como máquinas de asfalto, sino como ecosistemas habitables integrados en el ciclo del carbono.”</p>
<cite>Arq. Mateo Echeverri, Estudio de Bioarquitectura</cite>
</blockquote>",
                Author = "Mateo Echeverri",
                AuthorRole = "Arquitecto Urbanista",
                CategoryId = catHabitat.Id,
                SubcategoryId = subArqui.Id,
                Tags = ["Arquitectura", "Sostenibilidad", "Urbanismo", "Diseño"],
                CoverImageUrl = "https://images.unsplash.com/photo-1513694203232-719a280e022f?auto=format&fit=crop&w=1200&q=80",
                Featured = true,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-18),
                CreatedAt = DateTime.UtcNow.AddHours(-18),
                UpdatedAt = DateTime.UtcNow.AddHours(-18),
                ReadingTimeMinutes = 5,
                ViewCount = 278
            },

            // ─── 6. Energías Limpias (Hábitat) ────────────────────────────────────
            new()
            {
                Title = "Baterías de Estado Sólido y Redes Inteligentes: La Próxima Frontera Energética",
                Slug = "baterias-estado-solido-redes-inteligentes-energia",
                Summary = "El reemplazo de electrolitos líquidos por cerámicas conductoras promete triplicar la densidad de almacenamiento y estabilizar matrices solares y eólicas.",
                Content = @"<h2>1. El Reto de la Intermitencia</h2>
<p>El despliegue masivo de generación renovable requiere sistemas de almacenamiento seguros, rápidos y de altísima densidad para abastecer la demanda nocturna y los picos industriales.</p>

<h2>2. Ventajas del Electrolito Sólido</h2>
<div class=""table-responsive-wrapper"">
<table class=""article-table"">
<thead>
<tr>
<th>Parámetro</th>
<th>Litio-Ion Convencional</th>
<th>Batería de Estado Sólido</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Densidad Energética</strong></td>
<td>250 Wh/kg</td>
<td>450 a 500 Wh/kg</td>
</tr>
<tr>
<td><strong>Tiempo de Carga (80%)</strong></td>
<td>35 a 45 minutos</td>
<td>12 a 15 minutos</td>
</tr>
<tr>
<td><strong>Riesgo de Inflamabilidad</strong></td>
<td>Medio (electrolito orgánico)</td>
<td>Casi nulo (barrera cerámica inerte)</td>
</tr>
<tr>
<td><strong>Vida Útil (Ciclos)</strong></td>
<td>1,200 ciclos</td>
<td>Más de 3,000 ciclos</td>
</tr>
</tbody>
</table>
</div>

<blockquote>
<p>“El almacenamiento sólido es la pieza que faltaba para convertir a las energías solar y eólica en fuentes de potencia base ininterrumpida.”</p>
<cite>Ing. Laura Tamayo, Alianza Global de Almacenamiento</cite>
</blockquote>",
                Author = "Ignacio Navarro",
                AuthorRole = "Analista Energético",
                CategoryId = catHabitat.Id,
                SubcategoryId = subEnergia.Id,
                Tags = ["Energía", "Sostenibilidad", "Innovación", "Tecnología"],
                CoverImageUrl = "https://images.unsplash.com/photo-1497435334941-8c899ee9e8e9?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-22),
                CreatedAt = DateTime.UtcNow.AddHours(-22),
                UpdatedAt = DateTime.UtcNow.AddHours(-22),
                ReadingTimeMinutes = 4,
                ViewCount = 195
            },

            // ─── 7. Cine y Audiovisual (Cultura) ───────────────────────────────────
            new()
            {
                Title = "La Nueva Estética Cinematográfica: Iluminación Natural y Narrativas Inmersivas",
                Slug = "nueva-estetica-cinematografica-iluminacion-narrativas",
                Summary = "Directores de fotografía contemporáneos combinan sensores de rango dinámico extremo con ópticas clásicas para rescatar la autenticidad visual.",
                Content = @"<h2>1. El Retorno a la Luz Orgánica</h2>
<p>Frente al abuso de iluminación artificial y efectos digitales pesados, el cine contemporáneo abraza el claroscuro natural, aprovechando la sensibilidad de los sensores modernos que registran gradaciones tonales antes imposibles en penumbra.</p>

<h2>2. Claves de la Fotografía Atmosférica</h2>
<ul class=""article-unordered-list"">
<li><strong>Uso de ópticas anamórficas vintage:</strong> Aberraciones cromáticas controladas y bokeh elíptico con carácter.</li>
<li><strong>Planos secuencia fluidos:</strong> Estabilización física para acompañar al actor sin cortes abruptos.</li>
<li><strong>Composición basada en luz incidental:</strong> Aprovechamiento de ventanas, velas y fuentes prácticas en cuadro.</li>
</ul>

<blockquote>
<p>“La luz verdadera no ilumina un set, revela la emoción contenida en el rostro de un personaje sin intermediarios técnicos.”</p>
<cite>Felipe Arbeláez, Director de Fotografía</cite>
</blockquote>",
                Author = "Camila Restrepo",
                AuthorRole = "Crítica de Cine",
                CategoryId = catCultura.Id,
                SubcategoryId = subCine.Id,
                Tags = ["Cine", "Cultura", "Fotografía", "Arte"],
                CoverImageUrl = "https://images.unsplash.com/photo-1489599849927-2ee91cede3ba?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-26),
                CreatedAt = DateTime.UtcNow.AddHours(-26),
                UpdatedAt = DateTime.UtcNow.AddHours(-26),
                ReadingTimeMinutes = 4,
                ViewCount = 138
            },

            // ─── 8. Música y Acústica (Cultura) ────────────────────────────────────
            new()
            {
                Title = "El Diseño de Auditorios Sinfónicos: La Física Detrás de la Acústica Perfecta",
                Slug = "diseno-auditorios-sinfonicos-fisica-acustica-perfecta",
                Summary = "La geometría de viñedo, difusores fractales de madera y simulaciones computacionales recrean el balance sonoro ideal en las grandes salas mundiales.",
                Content = @"<h2>1. El Tiempo de Reverberación Ideal</h2>
<p>Una sala de conciertos sinfónicos busca un tiempo de reverberación de entre 1.8 y 2.2 segundos para brindar calidez sonora a las frecuencias medias sin enturbiar los pasajes percusivos rápidos.</p>

<h2>2. Elementos Clave del Confort Acústico</h2>
<ol class=""article-ordered-list"">
<li><strong>Difusores de residuo cuadrático:</strong> Superficies anguladas que dispersan la energía sonora de modo uniforme.</li>
<li><strong>Balcones en terraza (estilo viñedo):</strong> Acercan al espectador al escenario y optimizan las reflexiones laterales tempranas.</li>
<li><strong>Aislamiento pasivo de masa flotante:</strong> Cero intrusión de ruido exterior o vibraciones del transporte subterráneo.</li>
</ol>",
                Author = "Javier Morales",
                AuthorRole = "Ingeniero Acústico y Músico",
                CategoryId = catCultura.Id,
                SubcategoryId = subMusica.Id,
                Tags = ["Música", "Acústica", "Cultura", "Física"],
                CoverImageUrl = "https://images.unsplash.com/photo-1511671782779-c97d3d27a1d4?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-30),
                CreatedAt = DateTime.UtcNow.AddHours(-30),
                UpdatedAt = DateTime.UtcNow.AddHours(-30),
                ReadingTimeMinutes = 4,
                ViewCount = 112
            },

            // ─── 9. Café de Especialidad (Gastronomía) ─────────────────────────────
            new()
            {
                Title = "La Química del Café de Especialidad: Del Grano a la Extracción de Precisión",
                Slug = "quimica-cafe-especialidad-grano-extraccion-precision",
                Summary = "Altitud de cultivo, perfiles de tueste y control milimétrico del agua revelan notas florales y acidez brillante en variedades exóticas.",
                Content = @"<h2>1. Más Allá del Tueste Oscuro</h2>
<p>El movimiento de café de especialidad trata al fruto del cafeto con la misma reverencia que la vitivinicultura le otorga a la vid. Los microclimas por encima de 1,600 metros de altitud desaceleran la maduración del grano, concentrando azúcares y ácidos orgánicos complejos.</p>

<div class=""callout callout-info"" data-callout=""info"">
<div class=""callout-body""><strong>Regla del Barista:</strong> El agua representa el 98% de la taza. Mantener una mineralización de 120-150 ppm de calcio y magnesio es esencial para extraer compuestos aromáticos sin sobreextraer amargor.</div>
</div>

<h2>2. Variables Críticas en el Método de Filtrado</h2>
<div class=""table-responsive-wrapper"">
<table class=""article-table"">
<thead>
<tr>
<th>Variable</th>
<th>Rango Óptimo</th>
<th>Efecto en Taza</th>
</tr>
</thead>
<tbody>
<tr>
<td><strong>Temperatura del Agua</strong></td>
<td>91°C – 94°C</td>
<td>Balance entre acidez málica y notas de caramelo</td>
</tr>
<tr>
<td><strong>Molienda</strong></td>
<td>Media / Granulometría uniforme</td>
<td>Tiempo de contacto y tasa de flujo equilibrada</td>
</tr>
<tr>
<td><strong>Ratio de Extracción</strong></td>
<td>1:15 a 1:16 (café:agua)</td>
<td>Cuerpo sedoso y claridad de origen</td>
</tr>
</tbody>
</table>
</div>",
                Author = "Valeria Ochoa",
                AuthorRole = "Catadora Q Grader",
                CategoryId = catGastro.Id,
                SubcategoryId = subCafe.Id,
                Tags = ["Café", "Gastronomía", "Química", "Origen"],
                CoverImageUrl = "https://images.unsplash.com/photo-1495474472287-4d71bcdd2085?auto=format&fit=crop&w=1200&q=80",
                Featured = true,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-35),
                CreatedAt = DateTime.UtcNow.AddHours(-35),
                UpdatedAt = DateTime.UtcNow.AddHours(-35),
                ReadingTimeMinutes = 4,
                ViewCount = 245
            },

            // ─── 10. Alta Cocina (Gastronomía) ─────────────────────────────────────
            new()
            {
                Title = "Fermentación de Vanguardia: La Biotecnología Culinaria en la Alta Cocina",
                Slug = "fermentacion-vanguardia-biotecnologia-culinaria-cocina",
                Summary = "Hongos koji, vinagres de autor y garums vegetales abren una dimensión inexplorada de sabor umami en los mejores restaurantes del mundo.",
                Content = @"<h2>1. Redescubrimiento de Sabores Ancestrales</h2>
<p>Los laboratorios culinarios más reconocidos del mundo han reemplazado las técnicas de gelificación puramente visuales por fermentaciones microbianas controladas. Inocular koji (<em>Aspergillus oryzae</em>) sobre granos locales permite desdoblar proteínas complejas en aminoácidos que intensifican el sabor natural sin aditivos químicos.</p>

<h2>2. Procesos Primordiales en el Laboratorio Culinario</h2>
<ol class=""article-ordered-list"">
<li><strong>Cultivo de Koji:</strong> Control estricto de 30°C y 85% de humedad relativa durante 48 horas.</li>
<li><strong>Producción de Garums:</strong> Autólisis enzimática acelerada con temperatura de 60°C para seguridad microbiológica.</li>
<li><strong>Vinagres vivos de frutas endémicas:</strong> Fermentación acética lenta con aireación constante.</li>
</ol>

<blockquote>
<p>“Fermentar es colaborar con el tiempo y los microorganismos para transformar la materia prima en una experiencia sensorial profunda.”</p>
<cite>Chef Sebastián Londoño, Taller de Investigación Culinaria</cite>
</blockquote>",
                Author = "Sebastián Londoño",
                AuthorRole = "Chef Investigador",
                CategoryId = catGastro.Id,
                SubcategoryId = subAltaCocina.Id,
                Tags = ["Gastronomía", "Alta Cocina", "Fermentación", "Innovación"],
                CoverImageUrl = "https://images.unsplash.com/photo-1555396273-367ea4eb4db5?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-40),
                CreatedAt = DateTime.UtcNow.AddHours(-40),
                UpdatedAt = DateTime.UtcNow.AddHours(-40),
                ReadingTimeMinutes = 5,
                ViewCount = 178
            },

            // ─── 11. Movilidad y Transporte (Hábitat) ──────────────────────────────
            new()
            {
                Title = "Trenes de Alta Velocidad y Levitas Magnéticas: Conectando Megaciudades del Futuro",
                Slug = "trenes-alta-velocidad-levitacion-magnetica-transporte",
                Summary = "La tecnología Maglev y los corredores ferroviarios electrificados se consolidan como la alternativa ecológica prioritaria a los vuelos de corto alcance.",
                Content = @"<h2>1. Eficiencia Energética por Pasajero</h2>
<p>El tren de alta velocidad emite hasta un 85% menos de CO₂ por pasajero-kilómetro en comparación con la aviación comercial regional, mientras libera los cielos congestionados y reduce tiempos totales de viaje puerta a puerta.</p>

<h2>2. Innovaciones en Levitas Magnéticas (Maglev)</h2>
<p>Al eliminar el contacto físico entre la rueda y el carril, los convoyes reducen la fricción mecánica a cero, alcanzando velocidades operativas superiores a los 500 km/h con un consumo eléctrico optimizado.</p>",
                Author = "Ignacio Navarro",
                AuthorRole = "Analista de Movilidad",
                CategoryId = catHabitat.Id,
                SubcategoryId = subEnergia.Id,
                Tags = ["Transporte", "Movilidad", "Sostenibilidad", "Ingeniería"],
                CoverImageUrl = "https://images.unsplash.com/photo-1474487548417-781cb71495f3?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-45),
                CreatedAt = DateTime.UtcNow.AddHours(-45),
                UpdatedAt = DateTime.UtcNow.AddHours(-45),
                ReadingTimeMinutes = 3,
                ViewCount = 130
            },

            // ─── 12. Filosofía y Ensayo (Cultura) ──────────────────────────────────
            new()
            {
                Title = "El Arte del Ensayo: Pensar con Pausa en Tiempos de Inmediatez Digital",
                Slug = "arte-del-ensayo-pensar-pausa-inmediatez-digital",
                Summary = "Frente al bombardeo de estímulos efímeros, la lectura y redacción pausada del ensayo recupera el valor de la duda metódica y la argumentación sólida.",
                Content = @"<h2>1. La Resistencia de la Atención Sostenida</h2>
<p>Vivimos en una economía de atención fragmentada donde los algoritmos recompensan la reacción instintiva y la indignación rápida. El ensayo literario y filosófico se erige como un refugio de contemplación activa y análisis pausado.</p>

<h2>2. Valores del Género Humanista</h2>
<ul class=""article-unordered-list"">
<li><strong>La exploración honesta de la duda:</strong> Reconocer la complejidad antes de dictar sentencias apresuradas.</li>
<li><strong>Riqueza léxica y metafórica:</strong> Nombrar matices que el lenguaje plano pasa por alto.</li>
<li><strong>Diálogo entre disciplinas:</strong> Cruzar literatura, ciencia, historia y vivencia personal.</li>
</ul>

<blockquote>
<p>“Escribir un ensayo no es defender una certeza, sino salir a caminar con el lector para descubrir lo que ignorábamos juntos.”</p>
<cite>Helena Durán, Catedrática de Humanidades</cite>
</blockquote>",
                Author = "Helena Durán",
                AuthorRole = "Ensayista y Docente",
                CategoryId = catCultura.Id,
                SubcategoryId = subCine.Id,
                Tags = ["Filosofía", "Cultura", "Literatura", "Sociedad"],
                CoverImageUrl = "https://images.unsplash.com/photo-1457369804613-52c61a468e7d?auto=format&fit=crop&w=1200&q=80",
                Featured = false,
                Status = ArticleStatus.Publicado,
                PublishedAt = DateTime.UtcNow.AddHours(-50),
                CreatedAt = DateTime.UtcNow.AddHours(-50),
                UpdatedAt = DateTime.UtcNow.AddHours(-50),
                ReadingTimeMinutes = 4,
                ViewCount = 156
            }
        };

        // Insertar los 12 artículos completos
        await context.Articles.InsertManyAsync(testArticles);
    }

    private static async Task<Category> EnsureCategoryAsync(
        MongoDbContext context, string name, string slug, string description, int order, string? parentId = null)
    {
        var existing = await context.Categories.Find(c => c.Slug == slug).FirstOrDefaultAsync();
        if (existing is not null) return existing;

        var cat = new Category
        {
            Name = name,
            Slug = slug,
            Description = description,
            Order = order,
            ParentId = parentId,
            ShowInMenu = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await context.Categories.InsertOneAsync(cat);
        return cat;
    }
}
