using AwsS3.Models;
using AwsServices.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace api.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class FileManagementController : ControllerBase
    {
        private readonly ILogger<FileManagementController> _logger;
        private readonly IConfiguration _configuration;
        private readonly IStorageService _storageService;

        public FileManagementController(
            ILogger<FileManagementController> logger,
            IConfiguration configuration,
            IStorageService storageService)
        {
            _logger = logger;
            _configuration = configuration;
            _storageService = storageService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadFiles()
        {
            // Directly access the files from the request
            var files = Request.Form.Files;

            // Checking what files we have got
            _logger.LogInformation($"Files count in request: {files.Count}");
            foreach (var fileName in Request.Form.Files.Select(f => f.FileName))
            {
                _logger.LogInformation($"File received: {fileName}");
            }

            if (files.Count == 0)
            {
                return BadRequest("No files");
            }

            var awsCredentials = new AwsCredentials()
            {
                AccessKey = _configuration["AwsConfiguration:AWSAccessKey"]!,
                SecretKey = _configuration["AwsConfiguration:AWSSecretKey"]!,
                BucketName = _configuration["AwsConfiguration:BucketName"]!,
            };

            Console.WriteLine($"AccessKey: {awsCredentials.AccessKey}");
            Console.WriteLine(
                $"SecretKey: {awsCredentials}"); // Only print first 3 chars for security
            Console.WriteLine($"BucketName: {awsCredentials.BucketName}");

            var uploadObjects = new List<S3UploadObject>();

            foreach (var file in files)
            {
                if (file.Length <= 0) continue;
                var memoryStream = new MemoryStream();
                await file.CopyToAsync(memoryStream);
                memoryStream.Position = 0;

                var uploadObject = new S3UploadObject()
                {
                    BucketName = awsCredentials.BucketName,
                    Key = file.FileName,
                    InputStream = memoryStream,
                };

                uploadObjects.Add(uploadObject);
            }

            var response = await _storageService.UploadFiles(uploadObjects, awsCredentials);

            return Ok(response);
        }
    }
}