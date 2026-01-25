using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly MongoDbContext _context;
        private readonly IMapper _mapper;

        public UsuarioService(MongoDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync()
        {
            var usuarios = await _context.Usuarios
                .Find(u => u.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<UsuarioResponseDTO>>(usuarios);
            return response;
        }

        public async Task<UsuarioResponseDTO?> GetByIdAsync(int id)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.Id == id)
                .FirstOrDefaultAsync();

            if (usuario == null)
                return null;

            var response = _mapper.Map<UsuarioResponseDTO>(usuario);
            return response;
        }

        public async Task<IEnumerable<UsuarioResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var usuarios = await _context.Usuarios
                .Find(u => u.RestauranteId == restauranteId && u.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<UsuarioResponseDTO>>(usuarios);
            return response;
        }

        public async Task<UsuarioResponseDTO> CreateAsync(CreateUsuarioDTO dto)
        {
            var usuario = _mapper.Map<Usuario>(dto);
            usuario.Id = await _context.GetNextSequenceAsync("usuarios");
            usuario.Activo = true;

            await _context.Usuarios.InsertOneAsync(usuario);

            var response = _mapper.Map<UsuarioResponseDTO>(usuario);
            return response;
        }

        public async Task<UsuarioResponseDTO?> UpdateAsync(UpdateUsuarioDTO dto)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.Id == dto.Id)
                .FirstOrDefaultAsync();

            if (usuario == null)
                return null;

            // Mantener el password hash existente
            var currentPasswordHash = usuario.PasswordHash;
            _mapper.Map(dto, usuario);
            usuario.PasswordHash = currentPasswordHash;

            await _context.Usuarios.ReplaceOneAsync(u => u.Id == dto.Id, usuario);

            var response = _mapper.Map<UsuarioResponseDTO>(usuario);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.Id == id)
                .FirstOrDefaultAsync();

            if (usuario == null)
                return false;

            // Soft delete
            var update = Builders<Usuario>.Update.Set(u => u.Activo, false);
            await _context.Usuarios.UpdateOneAsync(u => u.Id == id, update);

            return true;
        }
    }
}
