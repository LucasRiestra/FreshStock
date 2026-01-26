using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace FreshStock.API.Entities
{
    [BsonIgnoreExtraElements(Inherited = true)]
    public abstract class BaseEntity
    {
        [BsonId]
        [BsonRepresentation(BsonType.Int32)]
        public int Id { get; set; }
    }
}
