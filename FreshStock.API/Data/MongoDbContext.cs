using FreshStock.API.Entities;
using MongoDB.Driver;

namespace FreshStock.API.Data
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(string connectionString, string databaseName)
        {
            var client = new MongoClient(connectionString);
            _database = client.GetDatabase(databaseName);
        }

        public IMongoCollection<Restaurante> Restaurantes => _database.GetCollection<Restaurante>("restaurantes");
        public IMongoCollection<Usuario> Usuarios => _database.GetCollection<Usuario>("usuarios");
        public IMongoCollection<Categoria> Categorias => _database.GetCollection<Categoria>("categorias");
        public IMongoCollection<Proveedor> Proveedores => _database.GetCollection<Proveedor>("proveedores");
        public IMongoCollection<Producto> Productos => _database.GetCollection<Producto>("productos");
        public IMongoCollection<StockLocal> StockLocal => _database.GetCollection<StockLocal>("stockLocal");
        public IMongoCollection<MovimientoInventario> MovimientosInventario => _database.GetCollection<MovimientoInventario>("movimientosInventario");
        public IMongoCollection<Counter> Counters => _database.GetCollection<Counter>("counters");

        // Método para obtener el siguiente ID secuencial
        public async Task<int> GetNextSequenceAsync(string collectionName)
        {
            var filter = Builders<Counter>.Filter.Eq(c => c.Id, collectionName);
            var update = Builders<Counter>.Update.Inc(c => c.SequenceValue, 1);
            var options = new FindOneAndUpdateOptions<Counter>
            {
                ReturnDocument = ReturnDocument.After,
                IsUpsert = true
            };

            var counter = await Counters.FindOneAndUpdateAsync(filter, update, options);
            return counter.SequenceValue;
        }
    }

    // Entidad para manejar IDs secuenciales (como en SQL)
    public class Counter
    {
        public string Id { get; set; } = null!;
        public int SequenceValue { get; set; }
    }
}
