using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Transfer;
using AwsS3.Models;

namespace AwsServices.Services;

public class S3StorageService : IStorageService
{
    public async Task<List<S3ResponseDto>> UploadFiles(IEnumerable<S3UploadObject> files,
        AwsCredentials awsCredentials)
    {
        var credentials =
            new BasicAWSCredentials(awsCredentials.AccessKey, awsCredentials.SecretKey);


        var config = new AmazonS3Config()
        {
            RegionEndpoint = Amazon.RegionEndpoint.APSouth1
        };

        using var client = new AmazonS3Client(credentials, config);
        var transferUtility = new TransferUtility(client);

        var uploadTasks = files.Select(async file =>
        {
            var response = new S3ResponseDto();

            try
            {
                var uploadRequest = new TransferUtilityUploadRequest()
                {
                    BucketName = file.BucketName,
                    Key = file.Key,
                    InputStream = file.InputStream,
                    CannedACL = S3CannedACL.NoACL,
                };
                await transferUtility.UploadAsync(uploadRequest);
                response.StatusCode = 200;
                response.Message = $"{file.Key} has been uploaded";
            }
            catch (AmazonS3Exception ex)
            {
                response.StatusCode = (int)ex.StatusCode;
                response.Message = $"{file.Key} could not be uploaded: {ex.Message} {file.BucketName}";
            }
            catch (Exception e)
            {
                Console.WriteLine("Hey im here");
                response.StatusCode = 500;
                response.Message = $"{e.Message} {file.BucketName}";
            }

            return response;
        });

        var responses = (await Task.WhenAll(uploadTasks)).ToList();
        return responses;
    }
}