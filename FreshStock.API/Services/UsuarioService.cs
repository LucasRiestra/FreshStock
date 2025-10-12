using AutoMapper;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;
using FreshStock.API.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Services
{
    public class UsuarioService : IUsuarioService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public UsuarioService(ApplicationDbContext context, IMapper mapper)
        {
            _context = context;
            _mapper = mapper;
        }

        public async Task<IEnumerable<UsuarioResponseDTO>> GetAllAsync()
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<UsuarioResponseDTO>>(usuarios);
            return response;
        }

        public async Task<UsuarioResponseDTO?> GetByIdAsync(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return null;

            var response = _mapper.Map<UsuarioResponseDTO>(usuario);
            return response;
        }

        public async Task<IEnumerable<UsuarioResponseDTO>> GetByRestauranteIdAsync(int restauranteId)
        {
            var usuarios = await _context.Usuarios
                .Where(u => u.RestauranteId == restauranteId && u.Activo)
                .ToListAsync();

            var response = _mapper.Map<IEnumerable<UsuarioResponseDTO>>(usuarios);
            return response;
        }

        public async Task<UsuarioResponseDTO> CreateAsync(CreateUsuarioDTO dto)
        {
            var usuario = _mapper.Map<Usuario>(dto);
            usuario.Activo = true;

            // TODO: Hash password antes de guardar (BCrypt, etc.)
            // usuario.Password = HashPassword(dto.Password);

            _context.Usuarios.Add(usuario);
            await _context.SaveChangesAsync();

            var response = _mapper.Map<UsuarioResponseDTO>(usuario);
            return response;
        }

        public async Task<UsuarioResponseDTO?> UpdateAsync(UpdateUsuarioDTO dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == dto.Id);

            if (usuario == null)
                return null;

            // Mantener el password existente
            var currentPassword = usuario.Password;
            _mapper.Map(dto, usuario);
            usuario.Password = currentPassword;

            await _context.SaveChangesAsync();

            var response = _mapper.Map<UsuarioResponseDTO>(usuario);
            return response;
        }

        public async Task<bool> DeleteAsync(int id)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Id == id);

            if (usuario == null)
                return false;

            // Soft delete
            usuario.Activo = false;
            await _context.SaveChangesAsync();

            return true;
        }
    }
}
