using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System.Net.Mail;
using System.Text.RegularExpressions;
using Attachment = SendGrid.Helpers.Mail.Attachment;

namespace techrehubApi
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function("RepairRequest")]
        public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
        ILogger log)
        {
            //log.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(value: requestBody);

            // Extract form data
            string name = data?.name;
            string email = data?.email;
            string phone = data?.phone;
            string deviceType = data?.deviceType;
            string description= data?.description;
            string imageBase64 = data?.image;

            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(email) )
            {
                return new BadRequestObjectResult("Please provide all required fields.");
            }


            // Send email using SendGrid
            await SendEmailWithSendGrid(name, email, phone, deviceType, description,imageBase64);

            return new OkObjectResult("Form submitted successfully");
        }


        private static async Task SendEmailWithSendGrid(string name, string email, string phone, string deviceType,string description ,string imageBase64)
        {
            var apiKey = Environment.GetEnvironmentVariable("SendGridApiKey");
            var client = new SendGridClient(apiKey);
            var from = new EmailAddress(Environment.GetEnvironmentVariable("FromEmail"), "Device Repair Service");
            var subject = "New Device Repair Request";
            var to = new EmailAddress(Environment.GetEnvironmentVariable("ToEmail"));
            var plainTextContent = $"Name: {name}\nEmail: {email}\nPhone: {phone}\nDevice Type: {deviceType}";
            var htmlContent = $"<strong>Name:</strong> {name}<br><strong>Email:</strong> {email}<br><strong>Phone:</strong> {phone}<br><strong>Device Type:</strong> {deviceType}<br><strong>Fault:</strong> {description}";
            var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            
            if (!string.IsNullOrEmpty(imageBase64))
            {
                byte[] imageBytes = Convert.FromBase64String(imageBase64);
                var attachment = new Attachment
                {
                    Content = Convert.ToBase64String(imageBytes),
                    Type = "image/jpeg", // Adjust this if you're using a different image format
                    Filename = "device_images.jpg", // You can customize the filename
                    Disposition = "attachment",
                    ContentId = Guid.NewGuid().ToString()
                };
                msg.AddAttachment(attachment);
            }
            var response = await client.SendEmailAsync(msg);
            if (response.StatusCode != System.Net.HttpStatusCode.Accepted)
            {
                throw new Exception($"Failed to send email. Status code: {response.StatusCode}");
            }
        }
    }
}
