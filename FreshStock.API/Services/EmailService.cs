using System.Net;
using System.Net.Mail;
using FreshStock.API.Data;
using FreshStock.API.DTOs;
using FreshStock.API.Enums;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly MongoDbContext _context;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration configuration, MongoDbContext context, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public async Task SendEmailAsync(string to, string subject, string htmlBody)
        {
            await SendEmailAsync(new[] { to }, subject, htmlBody);
        }

        public async Task SendEmailAsync(IEnumerable<string> to, string subject, string htmlBody)
        {
            var smtpSettings = _configuration.GetSection("SmtpSettings");
            var host = smtpSettings["Host"];
            var port = int.Parse(smtpSettings["Port"] ?? "587");
            var username = smtpSettings["Username"];
            var password = smtpSettings["Password"];
            var fromEmail = smtpSettings["FromEmail"];
            var fromName = smtpSettings["FromName"] ?? "FreshStock";
            var enableSsl = bool.Parse(smtpSettings["EnableSsl"] ?? "true");

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                _logger.LogWarning("SMTP no configurado. Email no enviado. Destinatarios: {To}", string.Join(", ", to));
                return;
            }

            try
            {
                using var client = new SmtpClient(host, port)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = enableSsl
                };

                var message = new MailMessage
                {
                    From = new MailAddress(fromEmail ?? username, fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };

                foreach (var recipient in to)
                {
                    message.To.Add(recipient);
                }

                await client.SendMailAsync(message);
                _logger.LogInformation("Email enviado a: {To}", string.Join(", ", to));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error enviando email a: {To}", string.Join(", ", to));
            }
        }

        public async Task SendAlertaStockEmailAsync(int restauranteId, GeneracionAlertasResultDTO resultado)
        {
            if (resultado.AlertasGeneradas == 0)
            {
                _logger.LogInformation("No hay alertas que enviar para restaurante {RestauranteId}", restauranteId);
                return;
            }

            // Obtener restaurante
            var restaurante = await _context.Restaurantes
                .Find(r => r.Id == restauranteId)
                .FirstOrDefaultAsync();

            if (restaurante == null)
                return;

            // Obtener admins y gerentes del restaurante
            var usuarioRestaurantes = await _context.UsuarioRestaurantes
                .Find(ur => ur.RestauranteId == restauranteId &&
                           ur.Activo &&
                           (ur.Rol == RolUsuario.Admin || ur.Rol == RolUsuario.Gerente))
                .ToListAsync();

            if (!usuarioRestaurantes.Any())
            {
                _logger.LogWarning("No hay admins/gerentes para enviar alertas del restaurante {RestauranteId}", restauranteId);
                return;
            }

            var usuarioIds = usuarioRestaurantes.Select(ur => ur.UsuarioId).ToList();
            var usuarios = await _context.Usuarios
                .Find(u => usuarioIds.Contains(u.Id) && u.Activo)
                .ToListAsync();

            var emails = usuarios.Select(u => u.Email).Where(e => !string.IsNullOrEmpty(e)).ToList();

            if (!emails.Any())
                return;

            // Obtener detalles de las alertas
            var alertas = await _context.AlertasStock
                .Find(a => a.RestauranteId == restauranteId && !a.Leida)
                .SortByDescending(a => a.TipoAlerta)
                .ToListAsync();

            // Construir email HTML
            var subject = $"[FreshStock] Alertas de Stock - {restaurante.Nombre}";
            var htmlBody = BuildAlertaEmailHtml(restaurante.Nombre, resultado, alertas);

            await SendEmailAsync(emails, subject, htmlBody);
        }

        private string BuildAlertaEmailHtml(string nombreRestaurante, GeneracionAlertasResultDTO resultado, List<Entities.AlertaStock> alertas)
        {
            var html = $@"
<!DOCTYPE html>
<html>
<head>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
        .header {{ background-color: #dc3545; color: white; padding: 20px; text-align: center; }}
        .summary {{ background-color: #f8f9fa; padding: 15px; margin: 20px 0; border-radius: 5px; }}
        .alert-critical {{ border-left: 4px solid #dc3545; padding: 10px; margin: 10px 0; background: #fff5f5; }}
        .alert-low {{ border-left: 4px solid #ffc107; padding: 10px; margin: 10px 0; background: #fffbf0; }}
        .alert-expire {{ border-left: 4px solid #17a2b8; padding: 10px; margin: 10px 0; background: #f0fafc; }}
        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Alertas de Stock</h1>
            <p>{nombreRestaurante}</p>
        </div>

        <div class='summary'>
            <h3>Resumen de Alertas</h3>
            <ul>
                <li><strong>Total de alertas:</strong> {resultado.AlertasGeneradas}</li>
                <li><strong>Sin stock:</strong> {resultado.AlertasSinStock}</li>
                <li><strong>Stock crítico:</strong> {resultado.AlertasStockCritico}</li>
                <li><strong>Stock bajo:</strong> {resultado.AlertasStockBajo}</li>
                <li><strong>Próximos a caducar:</strong> {resultado.AlertasProximoCaducar}</li>
            </ul>
        </div>

        <h3>Detalle de Alertas</h3>";

            // Agrupar por tipo
            var sinStock = alertas.Where(a => a.TipoAlerta == TipoAlerta.SinStock).ToList();
            var criticos = alertas.Where(a => a.TipoAlerta == TipoAlerta.StockCritico).ToList();
            var bajos = alertas.Where(a => a.TipoAlerta == TipoAlerta.StockBajo).ToList();
            var caducar = alertas.Where(a => a.TipoAlerta == TipoAlerta.ProximoCaducar).ToList();

            if (sinStock.Any())
            {
                html += "<h4>Sin Stock</h4>";
                foreach (var a in sinStock)
                {
                    html += $"<div class='alert-critical'>{a.Mensaje}</div>";
                }
            }

            if (criticos.Any())
            {
                html += "<h4>Stock Crítico</h4>";
                foreach (var a in criticos)
                {
                    html += $"<div class='alert-critical'>{a.Mensaje}</div>";
                }
            }

            if (bajos.Any())
            {
                html += "<h4>Stock Bajo</h4>";
                foreach (var a in bajos)
                {
                    html += $"<div class='alert-low'>{a.Mensaje}</div>";
                }
            }

            if (caducar.Any())
            {
                html += "<h4>Próximos a Caducar</h4>";
                foreach (var a in caducar)
                {
                    html += $"<div class='alert-expire'>{a.Mensaje}</div>";
                }
            }

            html += $@"
        <div class='footer'>
            <p>Este email fue enviado automáticamente por FreshStock al finalizar un inventario.</p>
            <p>Fecha: {DateTime.UtcNow:dd/MM/yyyy HH:mm} UTC</p>
        </div>
    </div>
</body>
</html>";

            return html;
        }
    }
}
