using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.Translate;
using Amazon.Translate.Model;


// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]

namespace TranslateBatch
{
    public class Function
    {
        IAmazonS3 S3Client { get; set; }
        private static readonly string[] languages = { "ES", "DE", "FR" };


        /// <summary>
        /// Default constructor. This constructor is used by Lambda to construct the instance. When invoked in a Lambda environment
        /// the AWS credentials will come from the IAM role associated with the function and the AWS region will be set to the
        /// region the Lambda function is executed in.
        /// </summary>
        public Function()
        {
            S3Client = new AmazonS3Client();
        }

        /// <summary>
        /// Constructs an instance with a preconfigured S3 client. This can be used for testing the outside of the Lambda environment.
        /// </summary>
        /// <param name="s3Client"></param>
        public Function(IAmazonS3 s3Client)
        {
            this.S3Client = s3Client;
        }

        /// <summary>
        /// This method is called for every Lambda invocation. This method takes in an S3 event object and can be used 
        /// to respond to S3 notifications.
        /// </summary>
        /// <param name="evnt"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<string> FunctionHandler(S3Event evnt, ILambdaContext context)
        {
            var s3Event = evnt.Records?[0].S3;
            if (s3Event == null)
            {
                return null;
            }

            try
            {
                String content;

                using (var s3Client = new AmazonS3Client())
                {
                    GetObjectRequest request = new GetObjectRequest
                    {
                        BucketName = evnt.Records.First().S3.Bucket.Name,
                        Key = evnt.Records.First().S3.Object.Key
                    };

                    GetObjectResponse response = await s3Client.GetObjectAsync(request);

                    StreamReader reader = new StreamReader(response.ResponseStream);
                    content = reader.ReadToEnd();

                    using (var translateClient = new AmazonTranslateClient(Amazon.RegionEndpoint.EUWest1))
                    {
                        foreach (string language in languages)
                        {
                            var translateTextResponse = await translateClient.TranslateTextAsync(
                            new TranslateTextRequest()
                            {
                                Text = content,
                                SourceLanguageCode = "EN",
                                TargetLanguageCode = language
                            });

                            await S3Client.PutObjectAsync(new PutObjectRequest()
                            {
                                ContentBody = translateTextResponse.TranslatedText,
                                BucketName = evnt.Records.First().S3.Bucket.Name,
                                Key = evnt.Records.First().S3.Object.Key.Replace("EN", language)
                            });
                        }
                    }
                    return "Complete";
                }
            }
            catch (Exception e)
            {
                context.Logger.LogLine(e.Message);
                context.Logger.LogLine(e.StackTrace);
                throw;
            }
        }
    }
}
