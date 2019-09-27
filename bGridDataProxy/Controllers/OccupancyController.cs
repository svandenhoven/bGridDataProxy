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
    public class OccupancyController : ControllerBase
    {
        private readonly IOptions<Azure> _azureConfig;

        public OccupancyController(IOptions<Azure> azureConfig)
        {
            _azureConfig = azureConfig;
        }
        // GET: api/bGrid/Occupancy
        [HttpGet]
        public ActionResult<IEnumerable<bGridOccpancy>> Get()
        {
            List<bGridOccpancy> occupancyList = GetOccupancyList();
            return occupancyList;
        }

        // GET: api/occupancy/5
        [HttpGet("{id}", Name = "GetOccupancy")]
        public ActionResult<bGridOccpancy> Get(int id)
        {
            List<bGridOccpancy> occupancyList = GetOccupancyList();
            return occupancyList.Where(l => l.location_id == id).FirstOrDefault();
        }

        private List<bGridOccpancy> GetOccupancyList()
        {
            var container = GetBlobContainer("mindparkstorage", _azureConfig.Value.StorageKey, "bgriddata");
            List<string> files = GetBlobs(container);
            var occupancyList = new List<bGridOccpancy>();

            if (files.Count > 0)
            {
                var lastMeasurement = files.Last().ToString();
                CloudBlockBlob blob = container.GetBlockBlobReference(lastMeasurement);

                string text;
                using (var memoryStream = new MemoryStream())
                {
                    blob.DownloadToStreamAsync(memoryStream).Wait();
                    text = System.Text.Encoding.UTF8.GetString(memoryStream.ToArray()).Substring(1);
                    occupancyList = JsonConvert.DeserializeObject<List<bGridOccpancy>>(text);
                }
            }

            return occupancyList;
        }

        private static List<string> GetBlobs(CloudBlobContainer container)
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



        private  CloudBlobContainer GetBlobContainer(string accountName, string accountKey, string containerName)
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
