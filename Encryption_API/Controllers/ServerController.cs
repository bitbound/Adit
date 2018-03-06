using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Encryption_API.Controllers
{
    [Route("api/[controller]")]
    public class ServerController : Controller
    {
        private IHostingEnvironment HostEnv { get; set; }
        public ServerController(IHostingEnvironment env)
        {
        }           
    } 
}
