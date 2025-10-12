using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Services
{
    public class CategoriaService : ICategoriaService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public CategoriaService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<CategoriaResponseDTO>> GetAllAsync()
        {
            var categorias = await _context.Categorias.ToListAsync();

            var response = _mapper.Map<IEnumerable<CategoriaResponseDTO>>(categorias);
            return response;
        }

        public async Task<CategoriaResponseDTO?> GetByIdAsync(int id)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
                return null;

            var response = _mapper.Map<CategoriaResponseDTO>(categoria);
            return response;
        }

        public async Task<CategoriaResponseDTO> CreateAsync(CreateCategoriaDTO dto)
        {
            var categoria = _mapper.Map<Categoria>(dto);

            _context.Categorias.Add(categoria);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<CategoriaResponseDTO>(categoria);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var categoria = await _context.Categorias
                .FirstOrDefaultAsync(c => c.Id == id);

            if (categoria == null)
                return false;

            // Hard delete (categorías no tienen soft delete)
            _context.Categorias.Remove(categoria);
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
