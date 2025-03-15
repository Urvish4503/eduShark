using AwsS3.Models;

namespace AwsServices.Services;

public interface IStorageService
{
   Task<List<S3ResponseDto>> UploadFiles(IEnumerable<S3UploadObject> files, AwsCredentials awsCredentials);
}