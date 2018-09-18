using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SuperBlog.Models
{
    public class PostModel
    {
        public string postTitle { get; set; }
        public string postContent { get; set; }
        public DateTime postTime { get; set; }
    }
}
