using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using bGridDataProxy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Newtonsoft.Json;

namespace bGridDataProxy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TemperatureController : ControllerBase
    {
        private readonly IOptions<Azure> _azureConfig;

        public TemperatureController(IOptions<Azure> azureConfig)
        {
            _azureConfig = azureConfig;
        }
        // GET: api/bGrid/Temp
        [HttpGet]
        public IEnumerable<bGridTemperature> Get()
        {
            List<bGridTemperature> tempList = GetLastTemperaturesList();
            return tempList;
        }

        // GET api/temperature/5
        [HttpGet("{id}")]
        public ActionResult<bGridTemperature> Get(int id)
        {
            List<bGridTemperature> tempList = GetLastTemperaturesList();
            return tempList.Where(l => l.location_id == id).FirstOrDefault();
        }

        private List<bGridTemperature> GetLastTemperaturesList()
        {
            var container = GetBlobContainer("mindparkstorage", _azureConfig.Value.StorageKey, "bgridtemperature");
            List<string> files = GetBlobNames(container);
            List<bGridTemperature> tempList = GetLastTemperatures(container, files);
            return tempList;
        }

        private static List<bGridTemperature> GetLastTemperatures(CloudBlobContainer container, List<string> files)
        {
            var tempList = new List<bGridTemperature>();

            if (files.Count > 0)
            {
                var lastMeasurement = files.Last().ToString();
                CloudBlockBlob blob = container.GetBlockBlobReference(lastMeasurement);

                string text;
                using (var memoryStream = new MemoryStream())
                {
                    blob.DownloadToStreamAsync(memoryStream).Wait();
                    text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray()).Substring(1);
                    tempList = JsonConvert.DeserializeObject<List<bGridTemperature>>(text);
                }
            }

            return tempList;
        }

        private static List<string> GetBlobNames(CloudBlobContainer container)
        {
            var files = new List<string>();
            BlobContinuationToken blobContinuationToken = null;
            do
            {
                var results = container.ListBlobsSegmentedAsync(DateTime.Now.ToString("yyyy-MM-dd"), blobContinuationToken).Result;
                // Get the value of the continuation token returned by the listing call.
                blobContinuationToken = results.ContinuationToken;
                foreach (IListBlobItem item in results.Results)
                {
                    files.Add(item.Uri.Segments.Last());
                }
            } while (blobContinuationToken != null);
            return files;
        }

        private CloudBlobContainer GetBlobContainer(string accountName, string accountKey, string containerName)
        {
            CloudStorageAccount storageAccount = new CloudStorageAccount(new StorageCredentials(accountName, accountKey), true);
            // blob client
            CloudBlobClient blobClient = storageAccount.CreateCloudBlobClient();
            // container
            CloudBlobContainer blobContainer = blobClient.GetContainerReference(containerName);
            return blobContainer;
        }
    }

}
