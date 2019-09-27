using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using bGridDataProxy.Helpers;
using bGridDataProxy.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace bGridDataProxy.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AssetsController : ControllerBase
    {
        private readonly IOptions<bGrid> _bGridConfig;

        public AssetsController(IOptions<bGrid> bGridConfig)
        {
            _bGridConfig = bGridConfig;
        }

        // GET: api/Assets
        [HttpGet]
        public IEnumerable<bGridAsset> Get()
        {
            return GetTrackedAssets().Result;
        }

        // GET: api/Assets/5
        [HttpGet("{id}", Name = "GetAssets")]
        public bGridAsset Get(int id)
        {
            var assets = GetTrackedAssets().Result;
            var asset = assets.Where(a => a.id == id).FirstOrDefault();
            return asset;
        }

        private async Task<List<bGridAsset>> GetTrackedAssets()
        {
            var allAssets = await GetAssets();
            var knowAssets = new int[] { 5448, 5451, 5465, 5656 };
            var assetsList = allAssets.Where(a => knowAssets.Contains(a.id));
            var assets = new List<bGridAsset>();

            foreach (var asset in assetsList)
            {
                switch (asset.id)
                {
                    case 5448:
                        asset.assetType = "surfacehub";
                        break;
                    case 5451:
                        asset.assetType = "cleantrolley";
                        break;
                    case 5465:
                        asset.assetType = "surfacehub2";
                        break;
                    default:
                        asset.assetType = "unknowntype";
                        break;
                }
                assets.Add(asset);
            }
            return assets;
        }

        private async Task<List<bGridAsset>> GetAssets()
        {
            var bGridAssets = await BuildingActionHelper.ExecuteGetAction<List<bGridAsset>>("api/assets", _bGridConfig);
            return bGridAssets;
        }
    }
}
