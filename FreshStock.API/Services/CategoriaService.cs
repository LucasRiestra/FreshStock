using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public CategoriaService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoriaResponseDTO>> GetAllAsync()
        {
            var categorias = await _context.Categorias
                .Find(_ => true)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<CategoriaResponseDTO>>(categorias);
            return response;
        }

        public async Task<CategoriaResponseDTO?> GetByIdAsync(int id)
        {
            var categoria = await _context.Categorias
                .Find(c => c.Id == id)
                .FirstOrDefaultAsync();

            if (categoria == null)
                return null;

            var response = _mapper.Map<CategoriaResponseDTO>(categoria);
            return response;
        }

        public async Task<CategoriaResponseDTO> CreateAsync(CreateCategoriaDTO dto)
        {
            var categoria = _mapper.Map<Categoria>(dto);
            categoria.Id = await _context.GetNextSequenceAsync("categorias");

            await _context.Categorias.InsertOneAsync(categoria);

            var response = _mapper.Map<CategoriaResponseDTO>(categoria);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var result = await _context.Categorias.DeleteOneAsync(c => c.Id == id);
            return result.DeletedCount > 0;
        }
    }
}
