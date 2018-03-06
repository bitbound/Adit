using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using System.IO;

namespace Encryption_API.Controllers
{
    [Route("api/[controller]")]
    public class ClientController : Controller
    {
        private IHostingEnvironment HostEnv { get; set; }
        public ClientController(IHostingEnvironment env)
        {
            this.HostEnv = env;
        }

        [HttpGet]
        public IEnumerable<string> Get()
        {
            var di = Directory.CreateDirectory(Path.Combine(HostEnv.ContentRootPath, "Data"));
            foreach (var file in di.GetFiles())
            {
                if (DateTime.Now - file.CreationTime > TimeSpan.FromMinutes(1))
                {
                    file.Delete();
                }
            }
            using (var rng = System.Security.Cryptography.RandomNumberGenerator.Create())
            {
                var key = new byte[32];
                rng.GetBytes(key);
                var id = Guid.NewGuid().ToString();
                System.IO.File.WriteAllText(Path.Combine(di.FullName, id), Convert.ToBase64String(key));
                return new string[] { id, Convert.ToBase64String(key) };
            }
        }

        [HttpGet("{id}")]
        public string Get(string id)
        {
            var fi = new FileInfo(Path.Combine(HostEnv.ContentRootPath, "Data", id));
            if (fi.Exists)
            {
                var key = System.IO.File.ReadAllText(fi.FullName);
                fi.Delete();
                return key;
            }
            else
            {
                return string.Empty;
            }
        }

       
        [HttpPost]
        public void Post([FromBody]string hostname, string port, string sessionID)
        {

        }
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
