using FreshStock.API.DTOs;
using Microsoft.AspNetCore.Http;

namespace FreshStock.API.Interfaces
{
    public interface IImportacionExcelService
    {
        /// <summary>
        /// Analiza el archivo Excel y devuelve un preview sin guardar nada
        /// </summary>
        Task<ImportacionPreviewResultDTO> PreviewAsync(IFormFile archivo, ImportacionPreviewRequestDTO request);

        /// <summary>
        /// Ejecuta la importación guardando los productos en la base de datos
        /// </summary>
        Task<ImportacionResultDTO> EjecutarAsync(IFormFile archivo, ImportacionEjecutarRequestDTO request);

        /// <summary>
        /// Genera una plantilla Excel vacía con las columnas esperadas
        /// </summary>
        byte[] GenerarPlantilla();

        /// <summary>
        /// Exporta productos existentes a un archivo Excel
        /// </summary>
        Task<byte[]> ExportarAsync(ExportacionRequestDTO request);
    }
}
