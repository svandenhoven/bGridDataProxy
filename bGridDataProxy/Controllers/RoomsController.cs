using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
    public class RoomsController : ControllerBase
    {
        private readonly IOptions<Azure> _azureConfig;

        public RoomsController(IOptions<Azure> azureConfig)
        {
            _azureConfig = azureConfig;
        }

        // GET: api/Rooms/5
        [HttpGet("{id}", Name = "Get")]
        public int Get(int id)
        {
            var webClient = new WebClient();
            var jsonAzure = webClient.DownloadString("https://mindparkstorage.blob.core.windows.net/roomchecker/rooms.json?sp=r&st=2019-09-11T14:20:22Z&se=2019-09-11T22:20:22Z&spr=https&sv=2018-03-28&sig=iewhlE3NzAt%2FHz6XgL47SZLW%2Bu%2F34JnuhiXTISwdfAc%3D&sr=b");
            var rooms = JsonConvert.DeserializeObject<List<Room>>(jsonAzure);
            var room = rooms.Where(r => r.Name == id.ToString()).FirstOrDefault();

            var roomOccupancy = GetOccupancyList();
            var occupied = 0;
            foreach(var node in room.Nodes)
            {
                var o = roomOccupancy.Where(r => r.location_id.ToString() == node.Id).FirstOrDefault();
                if(o.value > 0)
                {
                    occupied = 1;
                    break;
                }
            }
            room.Occupied = occupied;
            return room.Occupied;
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
