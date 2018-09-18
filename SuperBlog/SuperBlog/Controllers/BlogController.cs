using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SuperBlog.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace SuperBlog.Controllers
{
    public class BlogController : Controller
    {
        const string BUCKETNAME = "imaya-blog-content";
        List<PostModel> Posts = new List<PostModel>();

        // GET: Blog
        public ActionResult Index([FromQuery]string lang)
        {
            return View("Index", GetPosts(lang ?? "EN").Result);
        }

        private async Task<List<PostModel>> GetPosts(string lang)
        {
            try
            {
                using (var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1))
                {
                    var listObjectsAsyncResponse = await s3Client.ListObjectsAsync(new ListObjectsRequest()
                    {
                        BucketName = BUCKETNAME,
                        Prefix = lang.ToUpper()
                    });

                    GetObjectRequest request = new GetObjectRequest
                    {
                        BucketName = BUCKETNAME
                    };

                    GetObjectResponse response;

                    foreach (S3Object s3Object in listObjectsAsyncResponse.S3Objects)
                    {
                        request.Key = s3Object.Key;
                        response = await s3Client.GetObjectAsync(request);

                        StreamReader reader = new StreamReader(response.ResponseStream);

                        Posts.Add(new PostModel()
                        {
                            postTitle = s3Object.Key.ToString().Replace("_", " ").Replace(".txt", "").Replace("EN/", "").Replace("ES/", ""),
                            postContent = reader.ReadToEnd(),
                            postTime = s3Object.LastModified
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return Posts;
        }

        // GET: Blog/Create
        public ActionResult Create()
        {
            return View();
        }

        public ActionResult Success()
        {
            return View();
        }

        public ActionResult Failure()
        {
            return View();
        }

        // POST: Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                var result = PostBlogPost(collection);

                if(result.Result.HttpStatusCode == System.Net.HttpStatusCode.OK)
                {
                    return RedirectToAction(nameof(Success));
                }
                else
                    return RedirectToAction(nameof(Failure));
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        private async Task<PutObjectResponse> PostBlogPost(IFormCollection collection)
        {
            var s3Client = new AmazonS3Client(Amazon.RegionEndpoint.USEast1);
            GetObjectRequest request = new GetObjectRequest();

            return await s3Client.PutObjectAsync(new PutObjectRequest()
            {
                ContentBody = collection["postContent"],
                BucketName = BUCKETNAME,
                Key = $"EN/{collection["postTitle"].ToString().Replace(" ", "_")}.txt"

            });
        }
    }
}