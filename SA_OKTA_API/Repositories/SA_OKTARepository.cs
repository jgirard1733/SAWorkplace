using SA_OKTA_API.Contracts;
using LoggerService;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using RestSharp;
using SAWorkplace.Models;
using SAWorkplace.Data;
using System.Linq;

namespace SA_OKTA_API.Repositories
{
    /// <summary>
    /// This is the main class that handles all your project's logic.
    /// You can find examples of how to parse JSON, write to logs, read from the config and decrypt your UN/PW.
    /// </summary>
    public class SAOktaRepository : ISAOktaRepository
    {
        private static ILoggerManager _logger;
        protected ApplicationDBContext aContext;
        private IConfiguration _config { get; set; }
        protected string oktaDomain = "";
        protected string oktaAPIKey = "";
        /// <summary>
        /// This is the consrtuctor for the DEMORepository.
        /// </summary>
        /// <param name="logger">The logger (NLog) is being injected at the time of creation.</param>
        /// <param name="config">The config (appsettings.json) is being injected at the time of creation.</param>
        public SAOktaRepository(ApplicationDBContext context, ILoggerManager logger, IConfiguration config)
        {
            aContext = context;
            _logger = logger;
            _config = config;
            try { oktaDomain = _config["Okta:Domain"]; oktaAPIKey = Decrypt("'EE\\qceJs+p$u24N", _config["Okta:APIKey"]); } catch { }
        }

        /// <summary>
        /// This example shows how to pull a value from the <c>_config</c> object created from the appsettings.json
        /// </summary>
        /// <param name="customer"></param>
        /// <returns></returns>
        public string UpdateOktaUsers()
        {
            {
                _logger.LogInfo("Starting UpdateOktaUsers");

                var client = new RestClient($"{oktaDomain}/api/v1/users?filter=status eq \"ACTIVE\"");
                client.Timeout = -1;
                var request = new RestRequest(Method.GET);
                request.AddHeader("Accept", "application/json");
                request.AddHeader("Content-Type", "application/json");
                request.AddHeader("Authorization", $"SSWS {oktaAPIKey}");
                IRestResponse response = client.Execute(request);
                var membersModel = new List<TeamMembersModel>();

                if (response.IsSuccessful)
                {
                    _logger.LogInfo("Got successful response");
                    _logger.LogInfo($"Response: {response.Content}");
                    dynamic responseObj = JsonConvert.DeserializeObject(response.Content);
                    foreach (dynamic user in responseObj)
                    {
                        string profileName = $"{user.profile.firstName} {user.profile.lastName}";
                        string profileID = user.profile.login;
                        string profileEmail = user.profile.email;
                        membersModel.Add(new TeamMembersModel { UserName = profileName, UserId = profileID.Replace("@ipipeline.com", ""), EmailAddress = profileEmail });
                    }

                    //OKTA returns a max of 200 users and provides a Link Header with another URL to call for the next 200
                    //So we need to make additional API calls to get full list.
                    bool getMoreUsers = true;
                    while (getMoreUsers)
                    {
                        string oktaLink = response.Headers.ToList()
                        .Find(x => x.Name == "Link")
                        .Value.ToString();
                        if (!String.IsNullOrEmpty(oktaLink))
                        {
                            string[] linkArray = oktaLink.Split(",");
                            string nextURL = string.Empty;

                            foreach (string link in linkArray)
                            {
                                if (link.Contains("next"))
                                {
                                    string[] linkProps = link.Split(";");
                                    int pFrom = linkProps[0].IndexOf("<") + 1;
                                    int pTo = linkProps[0].LastIndexOf(">");
                                    nextURL = linkProps[0][pFrom..pTo];
                                }
                            }
                            if (!String.IsNullOrEmpty(nextURL))
                            {
                                client = new RestClient(nextURL);
                                client.Timeout = -1;
                                request = new RestRequest(Method.GET);
                                request.AddHeader("Accept", "application/json");
                                request.AddHeader("Content-Type", "application/json");
                                request.AddHeader("Authorization", $"SSWS {oktaAPIKey}");
                                response = client.Execute(request);
                                if (response.IsSuccessful)
                                {
                                    _logger.LogInfo("Got another successful response");
                                    _logger.LogInfo($"Next Response: {response.Content}");
                                    responseObj = JsonConvert.DeserializeObject(response.Content);
                                    foreach (dynamic user in responseObj)
                                    {
                                        string profileName = $"{user.profile.firstName} {user.profile.lastName}";
                                        string profileID = user.profile.login;
                                        string profileEmail = user.profile.email;
                                        membersModel.Add(new TeamMembersModel { UserName = profileName, UserId = profileID.Replace("@ipipeline.com", ""), EmailAddress = profileEmail });
                                    }
                                }
                            }
                            else
                            {
                                getMoreUsers = false;
                            }
                        }
                        else
                        {
                            getMoreUsers = false;
                        }
                    }
                    //return membersModel.OrderBy(a => a.UserName).ToList();

                    var exists = aContext.tblOktaUsers.Any(u => u.ID == 1);

                    if (exists)
                    {
                        aContext.tblOktaUsers.Update(new OktaUsersModel() { ID = 1, OktaResults = JsonConvert.SerializeObject(membersModel.OrderBy(a => a.UserName).ToList()) });
                    } else
                    {
                        aContext.tblOktaUsers.Add(new OktaUsersModel() { ID = 1, OktaResults = JsonConvert.SerializeObject(membersModel.OrderBy(a => a.UserName).ToList()) });
                    }
                    
                    aContext.SaveChanges();
                    return "Ok";
                } else
                {
                    _logger.LogWarn($"Failed Response. {response.Content}");
                    throw new Exception($"Failed Response. {response.Content}");
                }

            }
        }

        //Decrypt method provided by Jim Girard as part of the new encryption on usernames and passwords requirement.
        private static string Decrypt(string key, string cipherString)
        {
            try
            {
                AesCryptoServiceProvider aes = new AesCryptoServiceProvider
                {
                    BlockSize = 128,
                    KeySize = 256,
                    IV = Encoding.UTF8.GetBytes(@"!QAZ2WSX#EDC4RFV"),
                    Key = Encoding.UTF8.GetBytes(key),
                    Mode = CipherMode.CBC,
                    Padding = PaddingMode.PKCS7
                };

                // Convert Base64 strings to byte array
                byte[] src = Convert.FromBase64String(cipherString);

                // decryption
                using ICryptoTransform decrypt = aes.CreateDecryptor();
                byte[] dest = decrypt.TransformFinalBlock(src, 0, src.Length);
                aes.Dispose();
                return Encoding.Unicode.GetString(dest);
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Exception Message. {e.Message} {e.StackTrace};");
                throw new Exception($"Exception Message. {e.Message} {e.StackTrace};");
            }
        }

    }
}
