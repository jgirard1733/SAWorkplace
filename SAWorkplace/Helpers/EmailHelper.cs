using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SAWorkplace.Models;
using SAWorkplace.Data;
using System.Web;
using Microsoft.Extensions.Configuration;
using LoggerService;
using MailKit.Net.Smtp;
using MimeKit;
using Microsoft.AspNetCore.Http;

namespace SAWorkplace.Helpers
{
    public class EmailHelper
    {
        protected ApplicationDBContext aContext;
        private readonly IConfiguration _config;
        protected bool emailFlag = true;
        private static ILoggerManager _logger;
        public EmailHelper(ApplicationDBContext context, IConfiguration configuration, ILoggerManager logger)
        {
            aContext = context;
            _config = configuration;
            _logger = logger;
            try { emailFlag = Convert.ToBoolean(_config["EmailSettings:SendEmails"]); } catch { }
            //_logger.LogDebug("EmailHelper: emailFlag = " + emailFlag.ToString());
        }
        public async void sendEmails(RequestEditModel request, string status, string userName, string userEmail, string newComment)
        {
            if (!emailFlag) return;

            try
            {
                var mail = new MimeMessage();
                mail.To.Add(new MailboxAddress(request.Requests.RequestorName, request.Requests.RequestorEmail));
                mail.From.Add(new MailboxAddress("~PS Architects", "psarchitects@ipipeline.com"));

                bool sendSAemail = true;
                if (status.StartsWith("SA") == true) sendSAemail = false;
                bool sendECemail = true;
                string ECUser = _config["Admins:EC"];
                if (ECUser == "") sendECemail = false;
                if (request.Requests.RequestType < 4) sendECemail = false;
                if (status.StartsWith("EC") == true) sendECemail = false;

                _logger.LogDebug("In sendEmails(): status = " + status + ", sendSAemail = " + sendSAemail.ToString() + ", sendECemail = " + sendECemail.ToString());

                var mailSA = new MimeMessage();
                var mailEC = new MimeMessage();
                string assignedSA = request.Requests.AssignedSA;
                string assignedSAName = request.Requests.AssignedSAName;
                if (sendSAemail)
                {
                    string assignedSAEmail = request.Requests.AssignedSAEmail;
                    if (!string.IsNullOrEmpty(assignedSA) && !string.IsNullOrEmpty(assignedSAEmail))
                    {
                        mailSA.To.Add(new MailboxAddress(assignedSAName, assignedSAEmail));
                    }
                    else
                    {
                        mailSA.To.Add(new MailboxAddress("~PS Architects", "psarchitects@ipipeline.com"));
                    }
                }
                if (sendECemail)
                {
                    mailSA.To.Add(new MailboxAddress(ECUser + "@ipipeline.com", ECUser + "@ipipeline.com"));
                    mailEC.To.Add(new MailboxAddress(ECUser + "@ipipeline.com", ECUser + "@ipipeline.com"));
                    mailEC.From.Add(new MailboxAddress("~PS Architects", "psarchitects@ipipeline.com"));
                }
                if (sendSAemail || sendECemail)
                {
                    sendSAemail = true;
                    mailSA.From.Add(new MailboxAddress("~PS Architects", "psarchitects@ipipeline.com"));
                }

                string bodyHeader = "<p>" + request.Requests.RequestorName + ",</p>";

                DataAccess data = new DataAccess(aContext, null);
                request.RequestTypes = await data.GetRequestTypes();
                RequestTypeModel requestType = request.RequestTypes.Where(x => Convert.ToInt32(x.RequestType) == Convert.ToInt32(request.Requests.RequestType)).FirstOrDefault();
                request.Status = await data.GetStatuses();
                StatusModel reqStatus = request.Status.Where(x => x.ID.Equals(request.Requests.RequestStatus)).FirstOrDefault();


                string bodyRequest = "<p><h3>Architectural Request: " + request.Requests.ProjectName + "</h3></p>";
                bodyRequest += "<p><b>Request ID: </b>" + request.Requests.TicketNumber + "</p>";
                bodyRequest += "<p><b>Carrier: </b>" + request.Requests.CarrierName + "</p>";

                bodyRequest += "<p><b>Request Type: </b>";
                if (requestType != null)
                    bodyRequest += requestType.RequestName;
                else if (request.Requests.RequestType == 4)
                    bodyRequest += "Feasibility Review";
                else if (request.Requests.RequestType == 6)
                    bodyRequest += "Initiation Review";
                else if (request.Requests.RequestType == 7)
                    bodyRequest += "Implementation Review";
                bodyRequest += "</p>";

                bodyRequest += "<p><b>Requested Date: </b>";
                if (request.Requests.RequestDate != null)
                    bodyRequest += request.Requests.RequestDate.ToShortDateString();
                bodyRequest += "</p>";

                bodyRequest += "<p><b>Due By: </b>";
                if (request.Requests.RequestReviewDate != null)
                    bodyRequest += request.Requests.RequestReviewDate.Value.ToShortDateString();
                bodyRequest += "</p>";

                bodyRequest += "<p><b>Description: </b>";
                var fullDesc = "";
                if (request.Requests.RequestType >= 4 && request.Requests.Requirements != null)
                {
                    fullDesc = HttpUtility.HtmlDecode(request.Requests.Requirements.Trim()).ToString();
                }
                else if (request.Requests.RequestDesc != null)
                {
                    fullDesc = HttpUtility.HtmlDecode(request.Requests.RequestDesc.Trim()).ToString();
                }
                if (fullDesc != "")
                {
                    int descLength = fullDesc.Length;
                    if (descLength > 1000) descLength = 1000;
                    bodyRequest += fullDesc.Substring(0, descLength);
                }
                bodyRequest += "</p>";

                string eSigFlag = request.Requests.eSig;
                string splunkFlag = request.Requests.Splunk;
                if (eSigFlag == "Yes" || splunkFlag == "Yes")
                {
                    if (eSigFlag == "Yes") bodyRequest += "<p><b>New eSignature Implementation: </b>" + eSigFlag + "</p>";
                    if (splunkFlag == "Yes") bodyRequest += "<p><b>Splunk Training: </b>" + splunkFlag + "</p>";
                }
                else { sendECemail = false; }

                bodyRequest += "<p><b>Requestor: </b>" + request.Requests.RequestorName + "</p>";

                if (!string.IsNullOrEmpty(assignedSA)) bodyRequest += "<p><b>Assigned SA: </b>" + assignedSAName + "</p>";
                if (reqStatus != null) bodyRequest += "<p><b>Status: </b>" + reqStatus.Text + "</p>";
                if (!string.IsNullOrEmpty(newComment)) bodyRequest += "<p><b>Comment from " + userName + ": </b>" + newComment + "</p>";

                //footer
                bodyRequest += "<hr><p>* <i>This email has been generated by SA Workplace</i></p>";

                string subject = "SA Workplace Ticket #" + request.Requests.TicketNumber + " " + request.Requests.ProjectName;

                switch (status)
                {
                    case "add":
                        mail.Subject = subject + " - Your Architectural Request has been received";
                        mail.Body = new TextPart("html")
                        {
                            Text = bodyHeader + "<p>Your request has been received by the Architecture team! This will be assigned to an Architect and you will receive updates as the request is reviewed.</p>" + bodyRequest
                        };

                        mailSA.Subject = subject + " - A new request has been submitted";
                        mailSA.Body = new TextPart("html")
                        {
                            Text = "<p>A new request has been submitted in SA Workplace.</p>" + bodyRequest
                        };

                        //EC
                        subject = "SA Workplace Ticket #" + request.Requests.TicketNumber;
                        if (eSigFlag == "Yes")
                        {
                            subject += " - new ESIG request";
                            bodyRequest = bodyRequest.Replace("<p><b>New eSignature Implementation: </b>", "<p style='color: red'><b>New eSignature Implementation: </b>");
                        }
                        if (splunkFlag == "Yes")
                        {
                            subject += " - needs Splunk Training";
                            bodyRequest = bodyRequest.Replace("<p><b>Splunk Training: </b>", "<p style='color: red'><b>Splunk Training: </b>");
                        }
                        mailEC.Subject = subject;
                        mailEC.Body = new TextPart("html")
                        {
                            Text = "<p>A new request has been submitted in SA Workplace.</p>" + bodyRequest
                        };
                        break;
                    case "assigned":
                        mail.Subject = subject + " - Your Architectural Request has been assigned";
                        mail.Body = new TextPart("html")
                        {
                            Text = bodyHeader + "<p>Your request has been assigned to an Architecture team member!</p>" + bodyRequest
                        };
                        
                        mailSA.Subject = subject + " - A Request has been assigned to you";
                        mailSA.Body = new TextPart("html")
                        {
                            Text = "<p>A Request has been assigned to you.</p>" + bodyRequest
                        };

                        break;
                    case "SApass":
                        mail.Subject = subject + " - Your Architectural Request has PASSED!";
                        mail.Body = new TextPart("html")
                        {
                            Text = bodyHeader + "<p>Your request has PASSED!</p>" + bodyRequest
                        };
                        break;
                    case "SAfail":
                        mail.Subject = subject + " - Your Architectural Request has Failed";
                        mail.Body = new TextPart("html")
                        {
                            Text = bodyHeader + "<p>Your request has FAILED. Please see any additional comments in your ticket.</p>" + bodyRequest
                        };
                        break;
                    default:
                        mail.Subject = subject + " - Your Architectural Request has been updated";
                        mail.Body = new TextPart("html")
                        {
                            Text = bodyHeader + "<p>Your request has been updated.</p>" + bodyRequest
                        };
                        if (sendSAemail)
                        {
                            mailSA.Subject = subject + " - A request has been updated";
                            mailSA.Body = new TextPart("html")
                            {
                                Text = "<p>A request has been updated.</p>" + bodyRequest
                            };
                        }
                        break;
                }

                _logger.LogDebug("In sendEmails(): " + mail.Subject);

                string server = _config["EmailSettings:SmtpServer"];
                int port = 587;
                Int32.TryParse(_config["EmailSettings:SmtpPort"], out port);

                using (var client = new SmtpClient())
                {
                    try
                    {
                        // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                        client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                        client.Connect(server, port, false);

                        // Note: only needed if the SMTP server requires authentication
                        //client.Authenticate("user", "password");

                        client.Send(mail);
                        if (sendSAemail) client.Send(mailSA);
                        if (sendECemail) client.Send(mailEC);
                    }
                    catch (Exception ex) {
                        _logger.LogError(ex, "Mail Exception: " + ex.Message + ": " + ex.InnerException);
                    }
                    finally {
                        client.Disconnect(true);
                    }
                }
            }
            catch (Exception mailExp)
            {
                _logger.LogError(mailExp, "EMailHelper - Exception: " + mailExp.Message + ": " + mailExp.InnerException);
            }
        }

    public async void sendSMEmails(RequestEditModel request, string status, string userName, string userEmail, string newComment, string SME)
    {
        if (!emailFlag) return;

        try
        {
            var mail = new MimeMessage();
            mail.To.Add(new MailboxAddress(request.Requests.RequestorName, request.Requests.RequestorEmail));
            mail.From.Add(new MailboxAddress("~PS Architects", "psarchitects@ipipeline.com"));

                bool sendSMEemail = true;
            if (status.StartsWith("SME") == true) sendSMEemail = false;

            _logger.LogDebug("In sendEmails(): status = " + status + ", sendSMEemail = " + sendSMEemail.ToString());

            var mailSME = new MimeMessage();


            string assignedSME = request.Requests.AssignedSA;
            string assignedSMEName = request.Requests.AssignedSAName;
            string assignedSMEEmail = request.Requests.AssignedSAEmail;

            if (!string.IsNullOrEmpty(assignedSME) && !string.IsNullOrEmpty(assignedSMEEmail))
            {
                mailSME.To.Add(new MailboxAddress(assignedSMEName, assignedSMEEmail));
            }
            else
            {
                    if (SME.Contains(",") == true)
                    {
                        string[] smeArray = SME.Split(',');
                        foreach(string name in smeArray)
                        {
                            mailSME.To.Add(new MailboxAddress("", name + "@ipipeline.com"));
                        }
                    }
                    else
                    {
                        mailSME.To.Add(new MailboxAddress("", SME + "@ipipeline.com"));
                    }
                }

            string bodyHeader = "<p>" + request.Requests.RequestorName + ",</p>";

            DataAccess data = new DataAccess(aContext, null);
            request.RequestTypes = await data.GetRequestTypes();
            RequestTypeModel requestType = request.RequestTypes.Where(x => Convert.ToInt32(x.RequestType) == Convert.ToInt32(request.Requests.RequestType)).FirstOrDefault();

            string bodyRequest = "<p><h3>Subject Matter Expert Request: " + request.Requests.ProjectName + "</h3></p>";
            bodyRequest += "<p><b>Request ID: </b>" + request.Requests.TicketNumber + "</p>";
            bodyRequest += "<p><b>Carrier: </b>" + request.Requests.CarrierName + "</p>";

            bodyRequest += "<p><b>Request Type: </b>";
            bodyRequest += requestType.RequestName;
            bodyRequest += "</p>";

            bodyRequest += "<p><b>Requested Date: </b>";
            if (request.Requests.RequestDate != null)
                bodyRequest += request.Requests.RequestDate.ToShortDateString();
            bodyRequest += "</p>";

            bodyRequest += "<p><b>Due By: </b>";
            if (request.Requests.RequestReviewDate != null)
                bodyRequest += request.Requests.RequestReviewDate.Value.ToShortDateString();
            bodyRequest += "</p>";

            bodyRequest += "<p><b>Description: </b>";
            var fullDesc = "";
                fullDesc = HttpUtility.HtmlDecode(request.Requests.RequestDesc.Trim()).ToString();
            if (fullDesc != "")
            {
                int descLength = fullDesc.Length;
                if (descLength > 1000) descLength = 1000;
                bodyRequest += fullDesc.Substring(0, descLength);
            }
            bodyRequest += "</p>";


            bodyRequest += "<p><b>Requestor: </b>" + request.Requests.RequestorName + "</p>";

            if (!string.IsNullOrEmpty(assignedSME)) bodyRequest += "<p><b>Assigned SA: </b>" + assignedSMEName + "</p>";
            bodyRequest += "<p><b>Status: </b>Assigned</p>";
            if (!string.IsNullOrEmpty(newComment)) bodyRequest += "<p><b>Comment from " + userName + ": </b>" + newComment + "</p>";

            //footer
            bodyRequest += "<hr><p>* <i>This email has been generated by SA Workplace</i></p>";

            string subject = "SA Workplace Ticket #" + request.Requests.TicketNumber + " " + request.Requests.ProjectName;

            switch (status)
            {
                case "add":
                    mail.Subject = subject + " - Your Subject Matter Expert Request has been received";
                    mail.Body = new TextPart("html")
                    {
                        Text = bodyHeader + "<p>Your request has been received by the SME team! This will be assigned to a Subject Matter Expert and you will receive updates as the request is reviewed.</p>" + bodyRequest
                    };

                    mailSME.Subject = subject + " - A new SME request has been submitted";
                    mailSME.Body = new TextPart("html")
                    {
                        Text = "<p>A new SME request has been submitted in SA Workplace.</p>" + bodyRequest
                    };
                    break;
                    case "assigned":
                        mail.Subject = subject + " - Your Subject Matter Expert Request has been assigned";
                        mail.Body = new TextPart("html")
                        {
                            Text = bodyHeader + "<p>Your request has been assigned to a SME team member!</p>" + bodyRequest
                        };

                        mailSME.Subject = subject + " - A Request has been assigned to you";
                        mailSME.Body = new TextPart("html")
                        {
                            Text = "<p>A Request has been assigned to you.</p>" + bodyRequest
                        };

                        break;
                    default:
                    mail.Subject = subject + " - Your Subject Matter Expert Request has been updated";
                    mail.Body = new TextPart("html")
                    {
                        Text = bodyHeader + "<p>Your request has been updated.</p>" + bodyRequest
                    };
                    if (sendSMEemail)
                    {
                        mailSME.Subject = subject + " - A request has been updated";
                        mailSME.Body = new TextPart("html")
                        {
                            Text = "<p>A request has been updated.</p>" + bodyRequest
                        };
                    }
                    break;
            }

            _logger.LogDebug("In sendEmails(): " + mail.Subject);

            string server = _config["EmailSettings:SmtpServer"];
            int port = 587;
            Int32.TryParse(_config["EmailSettings:SmtpPort"], out port);

            using (var client = new SmtpClient())
            {
                try
                {
                    // For demo-purposes, accept all SSL certificates (in case the server supports STARTTLS)
                    client.ServerCertificateValidationCallback = (s, c, h, e) => true;

                    client.Connect(server, port, false);

                    // Note: only needed if the SMTP server requires authentication
                    //client.Authenticate("user", "password");

                    client.Send(mail);
                        if (sendSMEemail)
                        {
                            mailSME.From.Add(new MailboxAddress("~PS Architects", "psarchitects@ipipeline.com"));
                            client.Send(mailSME);
                        }

                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Mail Exception: " + ex.Message + ": " + ex.InnerException);
                }
                finally
                {
                    client.Disconnect(true);
                }
            }
        }
        catch (Exception mailExp)
        {
            _logger.LogError(mailExp, "EMailHelper - Exception: " + mailExp.Message + ": " + mailExp.InnerException);
        }
    }

    }
}