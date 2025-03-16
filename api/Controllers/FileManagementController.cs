using Amazon.S3;
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
        private readonly IAmazonS3 _amazonS3;
        private readonly IStorageService _storageService;

        public FileManagementController(
            ILogger<FileManagementController> logger,
            IConfiguration configuration,
            IAmazonS3 amazonS3,
            IStorageService storageService)
        {
            _logger = logger;
            _configuration = configuration;
            _amazonS3 = amazonS3;
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

            var awsConfig = _configuration.GetSection("AWS");

            var awsCredentials = new AwsCredentials
            {
                AccessKey = awsConfig["AccessKey"]!,
                SecretKey = awsConfig["SecretKey"]!,
                Region = awsConfig["Region"]!,
                BucketName = awsConfig["BucketName"]!
            };
            

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

            var responses = await _storageService.UploadFiles(uploadObjects, awsCredentials);


            var errorResponses = responses.Where(r => r.StatusCode >= 400).ToList();

            if (errorResponses.Any())
            {
                // Return appropriate status code based on errors
                var firstError = errorResponses.First();
                return StatusCode(firstError.StatusCode, new
                {
                    Error = errorResponses
                });
            }

            return Ok(responses);
        }
    }
}