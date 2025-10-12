using System.ComponentModel.DataAnnotations;

namespace FreshStock.API.Entities
{
    public abstract class BaseEntity
    {
        public int Id { get; set; }
    }
}
