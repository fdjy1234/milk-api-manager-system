using System;
using System.Security.Cryptography;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MilkApiManager.Models;
using MilkApiManager.Services;
using MilkApiManager.Models.Apisix;
using System.Collections.Generic;

namespace MilkApiManager.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class KeysController : ControllerBase
    {
        private readonly IVaultService _vaultService;
        private readonly ApisixClient _apisixClient;

        public KeysController(IVaultService vaultService, ApisixClient apisixClient)
        {
            _vaultService = vaultService;
            _apisixClient = apisixClient;
        }

        [HttpPost]
        public async Task<IActionResult> CreateKey([FromBody] CreateKeyRequest request)
        {
            // 1. 生成高強度 API Key
            var rawKey = $"milk_{Guid.NewGuid().ToString("N")}";
            
            // 2. 存入 Vault
            await _vaultService.StoreSecretAsync($"secret/apikeys/{request.Owner}", rawKey);

            // 3. 同步至 APISIX Consumer
            var consumer = new Consumer
            {
                Username = request.Owner,
                Plugins = new Dictionary<string, object>
                {
                    { "key-auth", new { key = rawKey } }
                }
            };
            await _apisixClient.CreateConsumerAsync(request.Owner, consumer);

            // 4. 建立 DB 紀錄 (只存 Hash 或是 Metadata)
            var record = new ApiKey
            {
                Id = Guid.NewGuid(),
                Owner = request.Owner,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(request.ValidityDays),
                IsActive = true
            };

            // 5. 回傳原始 Key 給用戶 (這是唯一一次看到它的機會)
            return Ok(new { 
                ApiKey = rawKey, 
                Message = "Please save this key immediately. It will not be shown again." 
            });
        }
    }
}
