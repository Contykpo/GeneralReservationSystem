using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using System;
using System.Threading.Tasks;

namespace GeneralReservationSystem.Web.Client.Helpers
{
    /// <summary>
    /// Helper estatico para enviar correos electronicos utilizando MailKit.
    /// Soporta plantillas predefinidas y mensajes personalizados asincronicos.
    /// </summary>
    public static class EmailManager
    {
        #region Configuration Fields

        // TO-DO: Deberiamos usar una variable de entorno o un gestor de secretos para la clave real.

        // Podriamos usar Postmark SMTP u otro servicio de emails gratuito.
        private static readonly string SmtpServer = "smtp.postmarkapp.com";
        private static readonly int SmtpPort = 587;
        // El nombre de usuario real para Postmark.
        private static readonly string SmtpUsername = "apikey";
        // Reemplazar "POSTMARK_API_KEY" con la clave real en produccion.
        private static readonly string SmtpPassword = "POSTMARK_API_KEY";

        private static readonly string SenderEmail = "realreservationsystem@realgrs.com";
        private static readonly string SenderName = "General Reservation System";

        #endregion

        /// <summary>
        /// Envia un correo electronico en texto plano o HTML de forma asincronica.
        /// </summary>
        public static async Task SendEmailAsync(string recipientEmail, string subject, string bodyHtml, bool isHtml = true)
        {
            var email = new MimeMessage();
            email.From.Add(new MailboxAddress(SenderName, SenderEmail));
            email.To.Add(MailboxAddress.Parse(recipientEmail));
            email.Subject = subject;
            email.Body = new TextPart(isHtml ? TextFormat.Html : TextFormat.Plain)
            {
                Text = bodyHtml
            };

            using var smtp = new SmtpClient();
            try
            {
                await smtp.ConnectAsync(SmtpServer, SmtpPort, SecureSocketOptions.StartTls);
                await smtp.AuthenticateAsync(SmtpUsername, SmtpPassword);
                await smtp.SendAsync(email);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EmailManager] Ocurrio un error al enviar el email a: {recipientEmail}: {ex.Message}");
                throw;
            }
            finally
            {
                await smtp.DisconnectAsync(true);
            }
        }

        // --- Email Templates ---

        /// <summary>
        /// Envia un correo electronico de confirmacion de reserva personalizado al usuario.
        /// </summary>
        public static async Task SendReservationConfirmationAsync(string recipientEmail, string userName, string departureStation, string arrivalStation, string departureTime, int seatNumber)
        {
            string subject = $"Notificacion de reserva confirmada - General Reservation System";
            string body = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <h2>¡Hola {userName}!</h2>
                        <p>Tu reserva fue confirmada exitosamente.</p>
                        <p><strong>Detalles de la reserva:</strong></p>
                        <ul>
                            <li><b>Desde:</b> {departureStation}</li>
                            <li><b>Hasta:</b> {arrivalStation}</li>
                            <li><b>Asiento:</b> {seatNumber}</li>
                            <li><b>Fecha de salida:</b> {departureTime}</li>
                        </ul>
                        <p>¡¡¡Gracias por confiar!!!.</p>
                        <br/>
                        <p style='font-size: 0.9em; color: #666;'>Este es un correo automático, por favor no respondas a este mensaje.</p>
                    </body>
                </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }

        /// <summary>
        /// Envia una notificacion generica para propositos administrativos o de alerta.
        /// </summary>
        public static async Task SendNotificationAsync(string recipientEmail, string title, string message)
        {
            string subject = $"[Notificación] {title}";
            string body = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <h3>{title}</h3>
                        <p>{message}</p>
                        <br/>
                        <p style='font-size: 0.9em; color: #666;'>Este mensaje fue generado automáticamente por el sistema de reservas.</p>
                    </body>
                </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }

        /// <summary>
        /// Envia un correo electronico de notificacion de cancelacion.
        /// </summary>
        public static async Task SendCancellationEmailAsync(string recipientEmail, string userName, string departureTime)
        {
            string subject = $"Reserva cancelada - General Reservation System";
            string body = $@"
                <html>
                    <body style='font-family: Arial, sans-serif; color: #333;'>
                        <h2>Hola {userName},</h2>
                        <p>Lamentamos informarte que tu reserva para el día #{departureTime} ha sido cancelada.</p>
                        <p>Si no solicitaste esta cancelación, por favor contacta con soporte lo antes posible.</p>
                        <br/>
                        <p>Atentamente,<br/>Equipo de Soporte</p>
                    </body>
                </html>";

            await SendEmailAsync(recipientEmail, subject, body);
        }
    }
}
