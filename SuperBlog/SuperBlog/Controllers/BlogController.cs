using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using SuperBlog.Models;

namespace SuperBlog.Controllers
{
    public class BlogController : Controller
    {
        const string BUCKETNAME = "imaya-blog-content";
        List<BlogModel> Blogs = new List<BlogModel>();

        // GET: Blog
        public ActionResult Index([FromQuery]string lang)
        {
            Task.WaitAll(GetBlogs(lang ?? "EN"));
            return View("Index", Blogs);
        }

        private async Task<List<BlogModel>> GetBlogs(string lang)
        {
            using (var s3Client = new AmazonS3Client())
            {
                var listObjectsAsyncResponse = await s3Client.ListObjectsAsync(new ListObjectsRequest()
                {
                    BucketName = BUCKETNAME,
                    Prefix = lang.ToUpper()
                });

                GetObjectRequest request = new GetObjectRequest();
                request.BucketName = BUCKETNAME;
                GetObjectResponse response;

                foreach (S3Object s3Object in listObjectsAsyncResponse.S3Objects)
                {
                    request.Key = s3Object.Key;
                    response = await s3Client.GetObjectAsync(request);

                    StreamReader reader = new StreamReader(response.ResponseStream);

                    Blogs.Add(new BlogModel()
                    {
                        blogTitle = s3Object.Key.ToString().Replace("_", " ").Replace(".txt", "").Replace("EN/", "").Replace("ES/",""),
                        blogContent =  reader.ReadToEnd()
                    });
                }
            }
            return Blogs;
        }

        // GET: Blog/Details/5
        public ActionResult Details(int id)
        {
            return View();
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

        // POST: Blog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(IFormCollection collection)
        {
            try
            {
                Task.WaitAll(PostBlog(collection));
                return RedirectToAction(nameof(Success));
            }
            catch
            {
                return View();
            }
        }

        private async Task<IActionResult> PostBlog(IFormCollection collection)
        {
            var s3Client = new AmazonS3Client();
            GetObjectRequest request = new GetObjectRequest();

            await s3Client.PutObjectAsync(new PutObjectRequest()
            {
                ContentBody = collection["blogContent"],
                BucketName = BUCKETNAME,
                Key = $"EN/{collection["blogTitle"].ToString().Replace(" ", "_")}.txt"

            });
            return null;
        }

        // GET: Blog/Edit/5
        public ActionResult Edit(int id)
        {
            return View();
        }

        // POST: Blog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add update logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }

        // GET: Blog/Delete/5
        public ActionResult Delete(int id)
        {
            return View();
        }

        // POST: Blog/Delete/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, IFormCollection collection)
        {
            try
            {
                // TODO: Add delete logic here

                return RedirectToAction(nameof(Index));
            }
            catch
            {
                return View();
            }
        }
    }
}