namespace FreshStock.API.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string htmlBody);
        Task SendEmailAsync(IEnumerable<string> to, string subject, string htmlBody);
        Task SendAlertaStockEmailAsync(int restauranteId, DTOs.GeneracionAlertasResultDTO resultado);
    }
}
