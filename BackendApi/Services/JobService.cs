using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using NATS.Client;
using StackExchange.Redis;
using Microsoft.Extensions.Configuration;
using System.Threading;
using System.IO;

namespace BackendApi.Services
{
    public class JobService : Job.JobBase
    {
        private readonly ILogger<JobService> _logger;
        private IConfiguration _config;
        private readonly ConnectionMultiplexer _redis;

        public JobService(ILogger<JobService> logger, IConfiguration config)
        {
            _logger = logger;
            _config = config;
            _redis = ConnectionMultiplexer.Connect($"{config.GetValue<string>("RedisHost")}:{config.GetValue<int>("RedisPort")}");
        }

        public override Task<RegisterResponse> Register(RegisterRequest request, ServerCallContext context)
        {
            string id = Guid.NewGuid().ToString();
            SaveToDb(id, request);
            PublishEvent(id);
            var resp = new RegisterResponse
            {
                Id = id
            };

            return Task.FromResult(resp);
        }

        public override Task<GetProcessingResultResponse> GetProcessingResult(GetProcessingResultRequest request, ServerCallContext context)
        {
            string id = request.Id;

            string status = "inProgress";
            string resultRank = String.Empty;

            var responce = new GetProcessingResultResponse { Rank = resultRank, Status = status };

            IDatabase db = _redis.GetDatabase();

            for (int i = 0; i < 10; i++)
            {
                resultRank = db.StringGet("rank_" + id);
                if (resultRank != null)
                {
                    responce.Status = "Completed";
                    responce.Rank = resultRank;
                    break;
                }

                Thread.Sleep(500);
            }

            return Task.FromResult(responce);
        }

        private void SaveToDb(string id, RegisterRequest request)
        {
            IDatabase db = _redis.GetDatabase();
            db.StringSet("description" + id, request.Description);
            db.StringSet(id, request.Data);
        }

        private void PublishEvent(string id)
        {
            var publisherService = new Publisher.PublisherService(_config);

            using (IConnection connection = new ConnectionFactory().CreateConnection($"localhost:{_config.GetValue<int>("NatsPort")}"))
            {
                publisherService.RunAsync(connection, id).Wait();
            }
        }
    }
}