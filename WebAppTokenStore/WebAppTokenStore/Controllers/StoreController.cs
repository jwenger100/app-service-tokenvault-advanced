﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Text;
using Microsoft.Azure.Services.AppAuthentication;
using System.Net.Http.Headers;
using System.Net;
using System.Configuration;

namespace WebAppTokenStore.Controllers
{
    [RoutePrefix("store")]
    public class VaultController : Controller
    {
        const string TokenStoreResource = "https://tokenstore.azure.net";
        // static client to have connection pooling
        private static HttpClient _httpClient = new HttpClient();

        [HttpGet]
        [Route("{storeName}/{serviceName}/{tokenName}/login")]
        public ActionResult LoginAsync(string storeName, string serviceName, string tokenName, string PostLoginRedirectUrl)
        {
            var vaultUrl = $"{ConfigurationManager.AppSettings["tokenResourceUrl"]}";
            return Redirect($"{vaultUrl}/services/{serviceName}/tokens/{tokenName}/login?PostLoginRedirectUrl={PostLoginRedirectUrl}");
        }

        [HttpGet]
        [Route("{storeName}/{serviceName}/{tokenName}/save")]
        public async Task<ActionResult> SaveTokenAsync(string storeName, string serviceName, string tokenName, string storeUrl, string code)
        {
            if (Session.SessionID != tokenName)
            {
                throw new InvalidOperationException($"Failed to commit token.");
                // This indicates the save call is coming from a different session then the login page was linked from and should not be allowed.
            }

            var azureServiceTokenProvider = new AzureServiceTokenProvider();

            string apiToken = await azureServiceTokenProvider.GetAccessTokenAsync(storeUrl);

            var request = new HttpRequestMessage(HttpMethod.Post, $"{storeUrl}/services/{serviceName}/tokens/{tokenName}/save");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiToken);
            request.Content = new StringContent(new JObject
                    {
                        {
                            "code", code
                        }
                    }.ToString(),
                    Encoding.UTF8,
                    "application/json");

            var response = await _httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();

                throw new InvalidOperationException($"Failed to commit token. {content}");
            }            

            return Redirect(this.Request.Url.GetLeftPart(UriPartial.Authority));
        }
    }
}