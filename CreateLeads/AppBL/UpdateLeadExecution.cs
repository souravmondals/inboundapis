namespace CreateLeads
{
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Xml.Linq;
    using CRMConnect;
    using System.Xml;
    using Microsoft.VisualBasic;

    public class UpdateLeadExecution : IUpdateLeadExecution
    {

        public ILoggers _logger;
        public IQueryParser _queryParser;

        public string Channel_ID
        {
            set
            {
                _logger.Channel_ID = value;
            }
            get
            {
                return _logger.Channel_ID;
            }
        }
        public string Transaction_ID
        {
            set
            {
                _logger.Transaction_ID = value;
            }
            get
            {
                return _logger.Transaction_ID;
            }
        }

        public string appkey { get; set; }
        public string API_Name
        {
            set
            {
                _logger.API_Name = value;
            }
        }
        public string Input_payload
        {
            set
            {
                _logger.Input_payload = value;
            }
        }

        private readonly IKeyVaultService _keyVaultService;

        private ICommonFunction _commonFunc;

        public UpdateLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {

            this._logger = logger;
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;


        }

        public async Task<UpdateLidStatusReturnParam> UpdateLeade(dynamic RequestData)
        {
            UpdateLidStatusReturnParam ldRtPrm = new UpdateLidStatusReturnParam();
            RequestData = await this.getRequestData(RequestData, "UpdateLeadStatus");

            try
            {
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "UpdateLeadStatusappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        Dictionary<string, string> odatab = new Dictionary<string, string>();

                        if (!string.IsNullOrEmpty(RequestData.ApplicantId.ToString()))
                        {
                            string LeadId = await this._commonFunc.getLeadIdByApplicent(RequestData.ApplicantId.ToString());
                            if (!string.IsNullOrEmpty(LeadId))
                            {
                                odatab.Add("eqs_assetleadstatus", await this._queryParser.getOptionSetTextToValue("lead", "eqs_assetleadstatus", RequestData.AssetAccountStatus.ToString()));
                                odatab.Add("eqs_assetaccountstatusremarks", RequestData.AssetAccountStatusRemark.ToString());
                                odatab.Add("eqs_accountnumberbylos", RequestData.AccountNumber.ToString());

                                string postDataParametr = JsonConvert.SerializeObject(odatab);
                                var LeadAccount_details = await this._queryParser.HttpApiCall($"leads({LeadId})", HttpMethod.Patch, postDataParametr);

                                ldRtPrm.ReturnCode = "CRM-SUCCESS";
                                ldRtPrm.Message = OutputMSG.Lead_Success;
                            }
                            else
                            {
                                this._logger.LogInformation("ValidateLeadtInput", "incorrect input ");
                                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                                ldRtPrm.Message = OutputMSG.Incorrect_Input;
                            }
                        }
                        else if (!string.IsNullOrEmpty(RequestData.LeadId.ToString()))
                        {
                            string LeadId = await this._commonFunc.getLeadId(RequestData.LeadId.ToString());
                            if (!string.IsNullOrEmpty(LeadId))
                            {
                                if (RequestData.LeadStatus.ToString() == "Not Onboarded")
                                {
                                    odatab.Add("eqs_leadstatus", await this._queryParser.getOptionSetTextToValue("lead", "eqs_leadstatus", RequestData.LeadStatus.ToString()));
                                    odatab.Add("eqs_notonboardedreason", RequestData.NotOnboardedReason.ToString());
                                    string postDataParametr = JsonConvert.SerializeObject(odatab);
                                    var LeadAccount_details = await this._queryParser.HttpApiCall($"leads({LeadId})", HttpMethod.Patch, postDataParametr);

                                    ldRtPrm.ReturnCode = "CRM-SUCCESS";
                                    ldRtPrm.Message = OutputMSG.Lead_Success;
                                }
                                else
                                {
                                    this._logger.LogInformation("ValidateLeadtInput", "incorrect input ");
                                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                                }
                            }
                            else
                            {
                                this._logger.LogInformation("ValidateLeadtInput", "incorrect input ");
                                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                                ldRtPrm.Message = OutputMSG.Incorrect_Input;
                            }
                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "Input UCIC are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }

                       
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "incorrect input ");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }

                }
                else
                {
                    this._logger.LogInformation("ValidateLeadtInput", "incorrect input");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateLeadtInput", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                ldRtPrm.Message = OutputMSG.Incorrect_Input;
            }

            return ldRtPrm;

        }



        public bool checkappkey(string appkey, string APIKey)
        {
            if (this._keyVaultService.ReadSecret(APIKey) == appkey)
            {
                return true;
            }
            else
            {
                return false;
            }
        }




        public async Task CRMLog(string InputRequest, string OutputRespons, string CallStatus)
        {
            Dictionary<string, string> CRMProp = new Dictionary<string, string>();
            CRMProp.Add("eqs_name", this.Transaction_ID);
            CRMProp.Add("eqs_requestbody", InputRequest);
            CRMProp.Add("eqs_responsebody", OutputRespons);
            CRMProp.Add("eqs_requeststatus", (CallStatus.Contains("ERROR")) ? "615290001" : "615290000");
            string postDataParametr = JsonConvert.SerializeObject(CRMProp);
            await this._queryParser.HttpApiCall("eqs_apilogs", HttpMethod.Post, postDataParametr);
        }

        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID);
        }

        private async Task<dynamic> getRequestData(dynamic inputData, string APIname)
        {

            dynamic rejusetJson;

            var EncryptedData = inputData.req_root.body.payload;
            string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);
            string xpath = "PIDBlock/payload";
            var nodes = xmlDoc.SelectSingleNode(xpath);
            foreach (XmlNode childrenNode in nodes)
            {
                JObject rejusetJson1 = (JObject)JsonConvert.DeserializeObject(childrenNode.Value);

                dynamic payload = rejusetJson1[APIname];

                this.appkey = payload.msgHdr.authInfo.token.ToString();
                this.Transaction_ID = payload.msgHdr.conversationID.ToString();
                this.Channel_ID = payload.msgHdr.channelID.ToString();

                rejusetJson = payload.msgBdy;

                return rejusetJson;

            }

            return "";

        }



    }
}
