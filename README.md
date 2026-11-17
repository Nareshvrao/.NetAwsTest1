# S3 Upload Starter

A small ASP.NET Core app that uploads files to Amazon S3. It includes a browser upload form, a health endpoint, and a JSON upload API.

## Requirements

- .NET 10 SDK (LTS)
- AWS CLI v2.32.0 or newer
- An AWS account and an S3 bucket
- An IAM Identity Center profile with permission to upload to the test bucket

## Run locally

1. Set your AWS region and bucket name in `appsettings.json`, or set environment variables:

   ```powershell
   $env:AWS__Region = "us-east-1"
   $env:AWS__S3Bucket = "your-unique-bucket-name"
   ```

2. Configure and sign in to a named IAM Identity Center profile:

   ```powershell
   aws configure sso --profile s3-upload-local
   aws sso login --profile s3-upload-local
   $env:AWS_PROFILE = "s3-upload-local"
   ```

   Use a permission set with only the S3 access the app needs. Do not use the AWS account root user for local development. The AWS SDK reads the selected profile; no AWS credentials are stored in this project.
3. Start the app:

   ```powershell
   dotnet run
   ```

4. Open the URL printed by `dotnet run`. Select a file (up to 10 MB) and upload it.

The API is `POST /api/uploads` using `multipart/form-data` with a `file` field. `GET /health` returns a simple health response.

## AWS permissions

The identity used by the app needs `s3:PutObject` for the bucket's `uploads/*` prefix. For example, attach a policy like this to the IAM role or user used locally:

```json
{
  "Version": "2012-10-17",
  "Statement": [
    {
      "Effect": "Allow",
      "Action": "s3:PutObject",
      "Resource": "arn:aws:s3:::your-unique-bucket-name/uploads/*"
    }
  ]
}
```

Keep the bucket private. This starter returns the object key but does not make files public or generate download links.

## Configuration

| Setting | Environment variable | Default |
| --- | --- | --- |
| AWS region | `AWS__Region` | `us-east-1` |
| S3 bucket | `AWS__S3Bucket` | (required) |
| AWS profile | `AWS_PROFILE` | `default` |

`__` maps to `:` in .NET configuration. Never commit AWS access keys or secrets.
