using System.Security.Cryptography.X509Certificates;

namespace FreshStock.API.Entities
{
    public class Proveedor : BaseEntity
    {
        public string Nombre { get; set; }
        public string Telefono { get; set; }
        public string Email { get; set; }
        public string Contacto { get; set; }
        public bool Activo { get; set; }
    }
}
