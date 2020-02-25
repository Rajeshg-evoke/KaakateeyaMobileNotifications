using Amazon;
using Amazon.SimpleNotificationService;
using Amazon.SimpleNotificationService.Model;
using Kaakateeya.Customer.NotificationService.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Kaakateeya.Customer.NotificationService
{
    public class NotificationService
    {
        private readonly ILogger _logger;
        private static string awsAccessKeyId = ConfigurationManager.AppSettings["awsAccessKeyId"].ToString();
        private static string awsSecretAccessKey = ConfigurationManager.AppSettings["awsSecretAccessKey"].ToString();
        private static string platformApplicationAndroidARN = ConfigurationManager.AppSettings["platformApplicationAndroidARN"].ToString();
        private static string platformApplicationiOSARN = ConfigurationManager.AppSettings["platformApplicationiOSARN"].ToString();
        private static string topicARN = ConfigurationManager.AppSettings["topicARN"].ToString();
        private static string environment = ConfigurationManager.AppSettings["Environment"].ToString();
        private static string arnStorage = null;
        private AmazonSimpleNotificationServiceClient client = new AmazonSimpleNotificationServiceClient(awsAccessKeyId, awsSecretAccessKey, RegionEndpoint.APSouth1);

        public NotificationService(ILogger<NotificationService> logger)
        {
            _logger = logger;
        }

        public async Task DeleteAllDisabledEndpoints()
        {
            try
            {
                await DeleteDisabledEndpoints(platformApplicationAndroidARN);
                await DeleteDisabledEndpoints(platformApplicationiOSARN);
            }
            catch (Exception Ex)
            {
                _logger.LogError(Common.LogMessage("", "DeleteAllDisabledEndpoints", Ex.Message, Ex.StackTrace));
            }
        }

        public async Task DeleteDisabledEndpoints(string platformApplicationARN)
        {
            try
            {
                var response = await ListEndpointsByPlatformApplication(platformApplicationARN);
                var disabled = response.Endpoints.Where(x => x.Attributes.Any(y => y.Key == "Enabled" && y.Value == "false")).ToList();
                foreach (var item in disabled)
                {
                    DeleteEndpointRequest deleteEndpointRequest = new DeleteEndpointRequest()
                    {
                        EndpointArn = item.EndpointArn
                    };
                    DeleteEndpointResponse deleteEndpointResponse = await client.DeleteEndpointAsync(deleteEndpointRequest);
                }
                string endPoint = String.Join(",", disabled.Select(x => x.EndpointArn).ToArray());
                DeleteDeviceEndPointsDTO deleteDeviceEndPointsDTO = new DeleteDeviceEndPointsDTO();
                deleteDeviceEndPointsDTO.EndPoint = endPoint;
                await DataAccess.DeleteDeviceEndPoints(deleteDeviceEndPointsDTO);
            }
            catch (Exception)
            {

                throw;
            }
        }

        public async Task<ListEndpointsByPlatformApplicationResponse> ListEndpointsByPlatformApplication(string platformApplicationARN)
        {
            try
            {
                ListEndpointsByPlatformApplicationRequest listEndpointsByPlatformApplicationRequest = new ListEndpointsByPlatformApplicationRequest()
                {
                    PlatformApplicationArn = platformApplicationARN
                };
                return await client.ListEndpointsByPlatformApplicationAsync(listEndpointsByPlatformApplicationRequest);
            }
            catch (Exception)
            {

                throw;
            }
        }
        public async Task CreateAndSaveMobileEndpoints()
        {
            try
            {
                _logger.LogInformation("CreateAndSaveMobileEndpoints method started at " + DateTime.Now.ToString());
                List<DeviceDetailsDTO> DeviceTokenDetailsForEmptyEndpoints = await DataAccess.GetDeviceTokenDetailsForEmptyEndpoints();
                _logger.LogInformation("Total empty endpoints count : " + DeviceTokenDetailsForEmptyEndpoints.Count);
                int i = 0;
                foreach (DeviceDetailsDTO DeviceDetails in DeviceTokenDetailsForEmptyEndpoints)
                {
                    try
                    {
                        if (DeviceDetails.PlatformTypeID == 1)
                        {
                            string endpointARN = await CreateMobileEndpoint(DeviceDetails.DeviceToken, platformApplicationiOSARN);
                            await CreateSubscription(endpointARN);
                            await DataAccess.UpdateDeviceTokenEndpoint(DeviceDetails.Cust_ID, DeviceDetails.DeviceToken, endpointARN, DeviceDetails.PlatformTypeID, "");
                        }
                        else if (DeviceDetails.PlatformTypeID == 2)
                        {
                            string endpointARN = await CreateMobileEndpoint(DeviceDetails.DeviceToken, platformApplicationAndroidARN);
                            await CreateSubscription(endpointARN);
                            await DataAccess.UpdateDeviceTokenEndpoint(DeviceDetails.Cust_ID, DeviceDetails.DeviceToken, endpointARN, DeviceDetails.PlatformTypeID, "");
                        }
                        i++;
                    }
                    catch (Exception Ex)
                    {
                        _logger.LogError(Common.LogMessage("", "CreateAndSaveMobileEndpoints", Ex.Message, Ex.StackTrace));
                        await DataAccess.UpdateDeviceTokenEndpoint(DeviceDetails.Cust_ID, DeviceDetails.DeviceToken, "", DeviceDetails.PlatformTypeID, "An error occured with message " + Ex.Message + " and stack trace " + Ex.StackTrace);
                    }
                }
                _logger.LogInformation("Total updated endpoints count : " + i);
                _logger.LogInformation("CreateAndSaveMobileEndpoints method ended at " + DateTime.Now.ToString());
            }
            catch (Exception Ex)
            {
                _logger.LogError(Common.LogMessage("", "CreateAndSaveMobileEndpoints", Ex.Message, Ex.StackTrace));
            }
        }


        public async Task<string> CreateMobileEndpoint(string DeviceToken, string platformApplicationARN)
        {
            string endpointArn = null;
            try
            {
                CreatePlatformEndpointRequest cpeReq = new CreatePlatformEndpointRequest();
                cpeReq.PlatformApplicationArn = platformApplicationARN;
                cpeReq.Token = DeviceToken;
                Dictionary<String, String> attributes = new Dictionary<String, String>();
                attributes["Enabled"] = "true";
                cpeReq.Attributes = attributes;
                CreatePlatformEndpointResponse cpeRes = await client.CreatePlatformEndpointAsync(cpeReq);
                endpointArn = cpeRes.EndpointArn;
            }
            catch (InvalidParameterException ipe)
            {
                string message = ipe.Message;
                Regex rgx = new Regex(".*Endpoint (arn:aws:sns[^ ]+) already exists with the same [Tt]oken.*",
                    RegexOptions.IgnoreCase);
                MatchCollection m = rgx.Matches(message);
                if (m.Count > 0 && m[0].Groups.Count > 1)
                {
                    // The platform endpoint already exists for this token, but with
                    // additional custom data that createEndpoint doesn't want to overwrite.
                    // Just use the existing platform endpoint.
                    endpointArn = m[0].Groups[1].Value;
                }
                else
                {
                    // Rethrow the exception, the input is actually bad.
                    throw ipe;
                }
            }
            EndpointArn = endpointArn;
            return endpointArn;
        }

        public async Task PublishMessage()
        {
            try
            {
                _logger.LogInformation("PublishMessage method started at " + DateTime.Now.ToString());
                List<DeviceDetailsDTO> DeviceTokenDetailsEndpoints = await DataAccess.GetDeviceTokenDetailsEndpoints();
                _logger.LogInformation("Total device token endpoints count : " + DeviceTokenDetailsEndpoints.Count);
                List<MobileNotificationMessageDTO> mobileNotificationMessages = await DataAccess.GetMobileNotificationMessage();
                _logger.LogInformation("Total mobile notification messages count (to be published) : " + mobileNotificationMessages.Count);
                List<long?> ToCustIds = mobileNotificationMessages.Select(x => x.ToCustID).Distinct().ToList();
                int j = 0;
                foreach (var ToCustId in ToCustIds)
                {
                    var deviceEndPointsForUser = DeviceTokenDetailsEndpoints.Where(x => x.Cust_ID == ToCustId).ToList();
                    var messagesForUser = mobileNotificationMessages.Where(x => x.ToCustID == ToCustId).ToList();

                    if (deviceEndPointsForUser.Count > 0 && messagesForUser.Count > 0)
                    {
                        foreach (var message in messagesForUser)
                        {
                            foreach (var deviceEndPoint in deviceEndPointsForUser)
                            {
                                try
                                {
                                    RootObject reqObj = new RootObject(deviceEndPoint.PlatformTypeID, message.MessageText);
                                    PublishRequest publishRequest = new PublishRequest()
                                    {
                                        Message = JsonConvert.SerializeObject(reqObj),
                                        TargetArn = deviceEndPoint.EndpointARN,
                                        MessageStructure = "json"
                                    };
                                    PublishResponse publishResponse = await client.PublishAsync(publishRequest);
                                    if (publishResponse.HttpStatusCode == System.Net.HttpStatusCode.OK)
                                    {
                                        await DataAccess.UpdateMobileNotificationMessagePublishStatus(message.ID, message.ToCustID, null);

                                    }
                                }
                                catch (Exception Ex)
                                {
                                    _logger.LogError(Common.LogMessage(message.ID.ToString(), "PublishMessage", Ex.Message, Ex.StackTrace));
                                    await DataAccess.UpdateMobileNotificationMessagePublishStatus(message.ID, message.ToCustID, "An error occured with message " + Ex.Message + " and stack trace " + Ex.StackTrace);
                                }
                            }
                            j++;
                        }
                    }
                }
                _logger.LogInformation("Total no. of messages published : " + j);
                _logger.LogInformation("PublishMessage method ended at " + DateTime.Now.ToString());
            }
            catch (Exception Ex)
            {
                _logger.LogError(Common.LogMessage("", "PublishMessage", Ex.Message, Ex.StackTrace));
            }
        }

        public async Task<string> CreateSubscription(string endpointARN)
        {
            string endpointArn = null;
            try
            {
                SubscribeRequest subReq = new SubscribeRequest();
                subReq.TopicArn = topicARN;
                subReq.Endpoint = endpointARN;
                subReq.Protocol = "Application";
                SubscribeResponse cpeRes = await client.SubscribeAsync(subReq);
            }
            catch (InvalidParameterException ipe)
            {
                string message = ipe.Message;
                Regex rgx = new Regex(".*Endpoint (arn:aws:sns[^ ]+) already exists with the same [Tt]oken.*",
                    RegexOptions.IgnoreCase);
                MatchCollection m = rgx.Matches(message);
                if (m.Count > 0 && m[0].Groups.Count > 1)
                {
                    endpointArn = m[0].Groups[1].Value;
                }
                else
                {
                    // Rethrow the exception, the input is actually bad.
                    throw ipe;
                }
            }
            catch (Exception)
            {
                throw;
            }
            EndpointArn = endpointArn;
            return endpointArn;
        }


        public static string EndpointArn
        {
            get
            {
                return arnStorage;
            }
            set
            {
                arnStorage = value;
            }
        }



        public class Aps
        {
            public string alert
            {
                get;
                set;
            }
        }
        public class APNS
        {
            public Aps aps
            {
                get;
                set;
            }
        }
        public class GCMData
        {
            public string message
            {
                get;
                set;
            }
        }
        public class GCM
        {
            public GCMData data
            {
                get;
                set;
            }
        }
        public class RootObject
        {

            public string APNS_SANDBOX
            {
                get;
                set;
            }

            public string APNS
            {
                get;
                set;
            }

            public string GCM
            {
                get;
                set;
            }
            public RootObject() { }
            public RootObject(int PlatformTypeID, string MessageText)
            {
                if (PlatformTypeID == 1)
                {
                    APNS apns = new APNS()
                    {
                        aps = new Aps()
                        {
                            alert = MessageText
                        }
                    };
                    if (environment == "PROD" || environment == "UAT")
                    {
                        this.APNS = JsonConvert.SerializeObject(apns);
                    }
                    else if (environment == "DEV")
                    {
                        this.APNS_SANDBOX = JsonConvert.SerializeObject(apns);
                    }
                }
                else if (PlatformTypeID == 2)
                {
                    GCM gcm = new GCM()
                    {
                        data = new GCMData()
                        {
                            message = MessageText
                        }
                    };
                    this.GCM = JsonConvert.SerializeObject(gcm);
                }
            }
        }
    }
}