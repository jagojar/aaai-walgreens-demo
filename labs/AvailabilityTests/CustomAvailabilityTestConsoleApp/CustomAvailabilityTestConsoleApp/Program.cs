using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace CustomAvailabilityTestConsoleApp
{
    class Program
    {
        // The Application Insights Instrumentation Key can be changed by going to the overview page of your Function App, selecting configuration, and changing the value of the APPINSIGHTS_INSTRUMENTATIONKEY Application setting.
        // DO NOT replace the code below with your instrumentation key, the key's value is pulled from the environment variable/application setting key/value pair.
        private static readonly string instrumentationKey = "YOUR INSTRUMENTATION KEY"; //Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");

        //[CONFIGURATION_REQUIRED]
        // If your resource is in a region like Azure Government or Azure China, change the endpoint address accordingly.
        // Visit https://docs.microsoft.com/azure/azure-monitor/app/custom-endpoints#regions-that-require-endpoint-modification for more details.
        private const string EndpointAddress = "https://dc.services.visualstudio.com/v2/track";

        private static readonly TelemetryConfiguration telemetryConfiguration = new TelemetryConfiguration(instrumentationKey, new InMemoryChannel { EndpointAddress = EndpointAddress });
        private static readonly TelemetryClient telemetryClient = new TelemetryClient(telemetryConfiguration);

        static void Main(string[] args)
        {
            Console.WriteLine("Hello Custom Availability Test!");

            ILogger log = NullLogger.Instance;

            RunCustomAIAvailabilityTest(log);
        }

        private static void RunCustomAIAvailabilityTest(ILogger log)
        {
            log.LogInformation($"Entering Run at: {DateTime.Now}");            

            // [CONFIGURATION_REQUIRED] provide {testName} accordingly for your test function
            string testName = "AvailabilityTestConsole";

            // REGION_NAME is a default environment variable that comes with App Service
            string location = "Boston, MA"; //"East US"; //Environment.GetEnvironmentVariable("REGION_NAME");

            log.LogInformation($"Executing availability test run for {testName} at: {DateTime.Now}");
            string operationId = Guid.NewGuid().ToString("N");

            Console.WriteLine("Enter: 1 for Github Test, 2 for local Test, other for null Test");
            int i = Convert.ToInt32(Console.ReadLine());

            var availability = new AvailabilityTelemetry
            {
                Id = operationId,
                Name = testName,
                RunLocation = location,
                Success = false
            };

            var stopwatch = new Stopwatch();
            stopwatch.Start();

            try
            {                

                switch (i)
                {
                    case 1:
                        availability.Properties.Add("Test Url", "https://api.github.com/orgs/dotnet/repos");
                        RunAvailabilityTestGithubDotnet(log);
                        break;
                    case 2:
                        RunAvailabilityTestLocal(log);
                        availability.Properties.Add("Test Url", "http://localhost:5555/");
                        break;
                    default:
                        RunNotImplemented(log);
                        break;
                }
                
                availability.Success = true;
                availability.Message = "Availability test from Console";                
                availability.Properties.Add("Server", "JuanLaptop");
            }
            catch (Exception ex)
            {
                availability.Message = ex.Message;
                availability.Success = false;

                var exceptionTelemetry = new ExceptionTelemetry(ex);
                exceptionTelemetry.Context.Operation.Id = operationId;
                exceptionTelemetry.Properties.Add("TestName", testName);
                exceptionTelemetry.Properties.Add("TestLocation", location);
                telemetryClient.TrackException(exceptionTelemetry);
            }
            finally
            {
                stopwatch.Stop();
                availability.Duration = stopwatch.Elapsed;
                availability.Timestamp = DateTimeOffset.UtcNow;                

                telemetryClient.TrackAvailability(availability);
                // call flush to ensure telemetry is sent
                telemetryClient.Flush();
            }
        }

        private static void RunAvailabilityTestGithubDotnet(ILogger log)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/vnd.github.v3+json"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Foundation Repository Reporter");
                        
            var responseMessage = client.GetAsync("https://api.github.com/orgs/dotnet/repos").Result;
            Console.WriteLine(responseMessage.StatusCode);
            var streamTask = responseMessage.Content.ReadAsStreamAsync().Result;

            var repositories = JsonSerializer.DeserializeAsync<List<Repository>>(streamTask).Result;

            foreach (var repo in repositories)
                Console.WriteLine($"{repo.Name} - {repo.GitHubHomeUrl}");            
        }

        private static void RunAvailabilityTestLocal(ILogger log)
        {
            var client = new HttpClient();

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("text/html"));
            client.DefaultRequestHeaders.Add("User-Agent", ".NET Core Console App");            

            var responseMessage = client.GetAsync("http://localhost:5555/").Result;
            Console.WriteLine(responseMessage.StatusCode);            

            Console.Write(responseMessage.Content.ReadAsStringAsync().Result);
        }

        private static void RunNotImplemented(ILogger log)
        {
            // Add your business logic here.
            throw new NotImplementedException();
        }
    }
} 

 



