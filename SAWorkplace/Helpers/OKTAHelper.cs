using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using RestSharp;
using SAWorkplace.Models;
using SAWorkplace.Data;
using LoggerService;
using Newtonsoft.Json;
using System.Security.Claims;

namespace SAWorkplace.Helpers
{
    public class OKTAHelper
    {
        private readonly IConfiguration _config;
        protected ApplicationDBContext _context;
        protected Decrypter decrypter;
        private static ILoggerManager _logger;
        protected string oktaDomain = "";
        protected string oktaAPIKey = "";

        public OKTAHelper(ApplicationDBContext context, IConfiguration configuration, ILoggerManager logger)
        {
            _context = context;
            _config = configuration;
            _logger = logger;
        }

        public async Task<List<TeamMembersModel>> callOKTA()
        {
            var membersModel = new List<TeamMembersModel>();
            try
            {
                var dbTeamMembers = _context.tblOktaUsers.Find(1);

                if (dbTeamMembers != null)
                {
                    membersModel = JsonConvert.DeserializeObject<List<TeamMembersModel>>(dbTeamMembers.OktaResults);
                }
            }
            catch (Exception e)
            {
                _logger.LogError(e, $"Exception Message. {e.Message} {e.StackTrace};");
                
            }

            return membersModel;
        }

        public async Task<TeamMembersModel> GetUserInfo(IEnumerable<Claim> claims)
        {
            var userClaims = claims;
            string oktaUserName, oktaUserEmail;
            oktaUserName = oktaUserEmail = string.Empty;

            foreach (var claim in userClaims)
            {
                if (claim.Type == "name")
                {
                    oktaUserName = claim.Value;
                }
                if (claim.Type == "email")
                {
                    oktaUserEmail = claim.Value;
                }
                if (oktaUserName != "" && oktaUserEmail != "")
                {
                    break;
                }
            }

            _logger.LogDebug($"In OKTAHelper.GetUserInfo(): oktaUserName is {oktaUserName} and oktaUserEmail is {oktaUserEmail}");

            var member = new TeamMembersModel{
                UserId = oktaUserEmail.Replace("@ipipeline.com", ""),
                UserName = oktaUserName, 
                EmailAddress = oktaUserEmail
            };

            return member;
        }

    }

}
