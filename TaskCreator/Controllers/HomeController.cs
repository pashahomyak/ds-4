using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using TaskCreator.Models;
using Grpc.Net.Client;
using BackendApi;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using System.Net.Http;

namespace TaskCreator.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IConfiguration _config;

        public HomeController(ILogger<HomeController> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
        }

        [HttpPost]
        //[HttpGet]
        public async Task<IActionResult> SubmitFormAsync(string taskDescription, string taskData)
        {
            string taskResult = String.Empty;

            AppContext.SetSwitch("System.Net.Http.SocketsHttpHandler.Http2UnencryptedSupport", true);
            using var channel = GrpcChannel.ForAddress($"http://localhost:{_config.GetValue<int>("BackendApiPort")}");
            var client = new Job.JobClient(channel);
            try
            {
                var reply = await client.RegisterAsync(
                              new RegisterRequest { Description = taskDescription, Data = taskData });
                taskResult = reply.Id;
                //ViewBag.TaskResult = "Задача создана с идентификатором: " + taskResult;
            }
            catch (RpcException)
            {
                ViewBag.TaskResult = "Подключение не установлено, т.к. конечный компьютер отверг запрос на подключение";
            }
            catch (ArgumentNullException)
            {
                ViewBag.TaskResult = "Описание задачи не может быть пустым";
            }

            //string url = Url.Page("TextDetails", new JobId { jobId = taskResult });
            //return Redirect(url);
            return RedirectToAction("TextDetails", new { jobId = taskResult});
        }

        public async Task<IActionResult> TextDetailsAsync(string jobId)
        {
            string status = String.Empty;

            using var channel = GrpcChannel.ForAddress($"http://localhost:{_config.GetValue<int>("BackendApiPort")}");
            var client = new Job.JobClient(channel);
            try
            {
                var reply = await client.GetProcessingResultAsync( new GetProcessingResultRequest { Id = jobId } );

                ViewBag.Status = reply.Status;
                ViewBag.ResultRank = reply.Rank;
            }
            catch (RpcException)
            {
                ViewBag.TaskResult = "Подключение не установлено, т.к. конечный компьютер отверг запрос на подключение";
            }
            
            return View();
        }

        public IActionResult Index()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}