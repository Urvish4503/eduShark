namespace AwsS3.Models;

public class S3UploadObject
{
    public string Key { get; set; } = null!;
    public MemoryStream InputStream { get; set; } = null!;
    public string BucketName { get; set; } = null!;
    
}