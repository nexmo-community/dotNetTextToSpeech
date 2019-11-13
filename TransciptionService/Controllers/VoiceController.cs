using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Nexmo.Api.Voice.Nccos;
using Nexmo.Api.Voice.Nccos.Endpoints;
using System.Collections.Generic;
using System.Net;

namespace TransciptionService.Controllers
{
    public class VoiceController : Controller
    {
        const string BASE_URL = "BASE_URL";
        const string LANGUAGE = "en-US";
        public IConfiguration _configuration;
        public VoiceController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public HttpStatusCode Events() {
            return HttpStatusCode.OK;
        }

        [HttpGet]
        public string Answer()
        {
            var webSocketAction = new ConnectAction()
            {
                Endpoint = new[]
                {
                    new WebsocketEndpoint()
                    {
                        Uri = $"wss://{BASE_URL}/ws",                        
                        ContentType="audio/l16;rate=8000", 
                        Headers = new Dictionary<string, string>(){{"language", LANGUAGE } }
                        
                    }
                }
            };

            var ncco = new Ncco(webSocketAction);
            return ncco.ToString();
        }
    }
}