using Domine.ContentBlocks;
using Domine.Entities;
using Domine.ValueObjects;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.IdGenerators;

namespace Infrastructure.Persistence;

/// <summary>
/// Registro central del mapeo BSON de las entidades y value objects del dominio.
/// Sustituye a los atributos <c>[Bson...]</c> que antes vivían en <c>Domine</c>,
/// de modo que el dominio desconozca el motor de persistencia.
/// Replica exactamente el mapeo previo para no alterar el formato almacenado.
/// </summary>
public static class MongoClassMaps
{
    private static readonly object Gate = new();
    private static bool _registered;

    /// <summary>Registra todos los class maps. Es idempotente y seguro de llamar varias veces.</summary>
    public static void Register()
    {
        lock (Gate)
        {
            if (_registered) return;
            _registered = true;

            RegisterValueObjects();
            RegisterContentBlocks();
            RegisterEntities();
        }
    }

    private static void RegisterValueObjects()
    {
        // SafeUrl: solo se persiste "value"; Host e IsRelative se derivan en memoria.
        BsonClassMap.RegisterClassMap<SafeUrl>(cm =>
        {
            cm.AutoMap();
            cm.GetMemberMap(v => v.Value).SetElementName("value");
            cm.UnmapProperty(v => v.Host);
            cm.UnmapProperty(v => v.IsRelative);
        });

        BsonClassMap.RegisterClassMap<RichText>(cm =>
        {
            cm.AutoMap();
            cm.GetMemberMap(v => v.Html).SetElementName("html");
            cm.GetMemberMap(v => v.Text).SetElementName("text");
        });

        BsonClassMap.RegisterClassMap<PlainText>(cm =>
        {
            cm.AutoMap();
            cm.GetMemberMap(v => v.Value).SetElementName("value");
        });
    }

    private static void RegisterContentBlocks()
    {
        BsonClassMap.RegisterClassMap<ContentBlock>(cm =>
        {
            cm.AutoMap();
            cm.SetIsRootClass(true);
            cm.UnmapProperty(c => c.Type); // era [BsonIgnore]
            cm.AddKnownType(typeof(ContentParagraphBlock));
            cm.AddKnownType(typeof(ContentHeadingBlock));
            cm.AddKnownType(typeof(ContentOrderedListBlock));
            cm.AddKnownType(typeof(ContentBulletListBlock));
            cm.AddKnownType(typeof(ContentQuoteBlock));
            cm.AddKnownType(typeof(ContentCalloutBlock));
            cm.AddKnownType(typeof(ContentImageBlock));
            cm.AddKnownType(typeof(ContentDocumentBlock));
            cm.AddKnownType(typeof(ContentTableBlock));
            cm.AddKnownType(typeof(ContentCodeBlock));
            cm.AddKnownType(typeof(ContentDividerBlock));
        });

        RegisterBlock<ContentParagraphBlock>("paragraph");
        RegisterBlock<ContentHeadingBlock>("heading");
        RegisterBlock<ContentOrderedListBlock>("ordered-list");
        RegisterBlock<ContentBulletListBlock>("bullet-list");
        RegisterBlock<ContentQuoteBlock>("quote");
        RegisterBlock<ContentCalloutBlock>("callout");
        RegisterBlock<ContentImageBlock>("image");
        RegisterBlock<ContentDocumentBlock>("document");
        RegisterBlock<ContentTableBlock>("table");
        RegisterBlock<ContentCodeBlock>("code");
        RegisterBlock<ContentDividerBlock>("divider");
    }

    private static void RegisterBlock<TBlock>(string discriminator) where TBlock : ContentBlock
    {
        BsonClassMap.RegisterClassMap<TBlock>(cm =>
        {
            cm.AutoMap();
            cm.SetDiscriminator(discriminator);
        });
    }

    private static void RegisterEntities()
    {
        BsonClassMap.RegisterClassMap<Article>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            MapObjectIdId(cm);
            SetObjectIdRepresentation(cm.GetMemberMap(a => a.CategoryId));
            SetObjectIdRepresentation(cm.GetMemberMap(a => a.SubcategoryId));
            SetObjectIdRepresentation(cm.GetMemberMap(a => a.CoverImageId));
            SetObjectIdRepresentation(cm.GetMemberMap(a => a.ImageIds));
            SetObjectIdRepresentation(cm.GetMemberMap(a => a.DocumentIds));
            SetStringRepresentation(cm.GetMemberMap(a => a.Status));
        });

        BsonClassMap.RegisterClassMap<Category>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            MapObjectIdId(cm);
            SetObjectIdRepresentation(cm.GetMemberMap(c => c.ParentId));
        });

        BsonClassMap.RegisterClassMap<Comment>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            MapObjectIdId(cm);
            SetObjectIdRepresentation(cm.GetMemberMap(c => c.ArticleId));
            SetObjectIdRepresentation(cm.GetMemberMap(c => c.VisitorId));
            SetStringRepresentation(cm.GetMemberMap(c => c.Status));
        });

        RegisterSimpleEntity<AdminUser>();
        RegisterSimpleEntity<Visitor>();
        RegisterSimpleEntity<DocumentMetadata>();
        RegisterSimpleEntity<InformativePage>();
        RegisterSimpleEntity<AutomationSettings>();

        BsonClassMap.RegisterClassMap<ImageMetadata>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            MapObjectIdId(cm);
            SetObjectIdRepresentation(cm.GetMemberMap(i => i.ArticleId));
        });

        BsonClassMap.RegisterClassMap<AutomationRun>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            MapObjectIdId(cm);
        });
        BsonClassMap.RegisterClassMap<GeneratedArticleInfo>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<AutomationSkippedTopic>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<AutomationDecision>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });

        // SiteSettings usa un Id fijo de tipo string ("site-settings"), sin representación ObjectId.
        BsonClassMap.RegisterClassMap<SiteSettings>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<ImageQualitySettings>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<WhatsAppGroupLink>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
        BsonClassMap.RegisterClassMap<SocialProfileLink>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
        });
    }

    /// <summary>Entidades con un único Id ObjectId y sin referencias ni enums especiales.</summary>
    private static void RegisterSimpleEntity<TEntity>() where TEntity : class
    {
        BsonClassMap.RegisterClassMap<TEntity>(cm =>
        {
            cm.AutoMap();
            cm.SetIgnoreExtraElements(true);
            MapObjectIdId(cm);
        });
    }

    /// <summary>
    /// Configura el miembro <c>Id</c> para que se genere y almacene como ObjectId,
    /// replicando <c>[BsonId]</c> + <c>[BsonRepresentation(BsonType.ObjectId)]</c>.
    /// </summary>
    private static void MapObjectIdId(BsonClassMap classMap)
    {
        var memberMap = classMap.GetMemberMap("Id");
        memberMap.SetIdGenerator(StringObjectIdGenerator.Instance);
        ApplyRepresentation(memberMap, BsonType.ObjectId);
    }

    /// <summary>Replica <c>[BsonRepresentation(BsonType.ObjectId)]</c> sobre el miembro.</summary>
    private static void SetObjectIdRepresentation(BsonMemberMap memberMap) =>
        ApplyRepresentation(memberMap, BsonType.ObjectId);

    /// <summary>Replica <c>[BsonRepresentation(BsonType.String)]</c> sobre el miembro.</summary>
    private static void SetStringRepresentation(BsonMemberMap memberMap) =>
        ApplyRepresentation(memberMap, BsonType.String);

    private static void ApplyRepresentation(BsonMemberMap memberMap, BsonType representation) =>
        ((IBsonMemberMapAttribute)new BsonRepresentationAttribute(representation)).Apply(memberMap);
}
