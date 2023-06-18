using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml.Linq;
using System.Collections.Generic;
using CRMConnect;
using System.Xml;
using Microsoft.Identity.Client;

namespace DedupeDigiLead
{
    public class DedupDgAccountNLExecution : IDedupDgAccountNLExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;

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

        public string API_Name { set
            {
                _logger.API_Name = value;
            }
        }
        public string Input_payload { set {
                _logger.Input_payload = value;
            } 
        }

        private readonly IKeyVaultService _keyVaultService;        
        
        private ICommonFunction _commonFunc;

        public DedupDgAccountNLExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

        }


        public async Task<dynamic> ValidateDedupDgAccNL(dynamic RequestData, string type)
        {

            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgAccNLTRReturn() : new DedupDgAccNLReturn();
            dynamic dedupDgChk = (type == "NLTR") ? new List<DedupDgChkNLTR>() : new List<DedupDgChkNL>();
            try
            {
                RequestData = await this.getRequestData(RequestData, "DedupeDigiAccount" + type);

                string LeadAccount = RequestData.LeadAccount;

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "DedupDgLdNLappkey"))
                {
                    if (!string.IsNullOrEmpty(LeadAccount) && LeadAccount != "")
                    {
                        var AccLead_data = await this._commonFunc.getLeadAccData(LeadAccount);
                        foreach (var AccId in AccLead_data)
                        {
                            var Account_data = await this.getDedupDgAccNLStatus(AccId, type);
                            dedupDgChk.Add(Account_data);
                        }
                        ldRtPrm.accountData = dedupDgChk;
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFtchDgLdSts", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateFtchDgLdSts", ex.Message);               
                ldRtPrm.ReturnCode = "CRM-ERROR-101";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
                return ldRtPrm;
            }

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


        public async Task<dynamic> getDedupDgAccNLStatus(string ApplicantId, string type)
        {
            dynamic ldRtPrm = (type == "NLTR") ? new DedupDgChkNLTR() : new DedupDgChkNL();

            DedupDgChkNLTR dedupDgChkNLTR;
            DedupDgChkNL dedupDgChkNL;
            JArray NLTR_data;
            int mData = 0;
            if (type == "NLTR" && this._commonFunc.GetMvalue<DedupDgChkNLTR>("NLTR" + ApplicantId, out dedupDgChkNLTR))
            {
                mData = 1;
                ldRtPrm = dedupDgChkNLTR;
            }
            else if (type == "NL" && this._commonFunc.GetMvalue<DedupDgChkNL>("NL" + ApplicantId, out dedupDgChkNL))
            {
                mData = 1;
                ldRtPrm = dedupDgChkNL;
            }
            else
            {

                try
                {

                    var Lead_data = await this._commonFunc.getLeadData(ApplicantId);
                    if (Lead_data.Count > 0)
                    {
                        dynamic LeadData = Lead_data[0];

                        if (type == "NLTR")
                        {
                            NLTR_data = await this._commonFunc.getNLTRData(LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString());
                        }
                        else
                        {
                            NLTR_data = await this._commonFunc.getNLData(LeadData.eqs_internalpan.ToString(), LeadData.eqs_aadhar.ToString(), LeadData.eqs_passportnumber.ToString(), LeadData.eqs_cinnumber.ToString());
                        }

                        if (NLTR_data.Count > 0)
                        {
                            ldRtPrm.AccountID = ApplicantId;
                            if (type == "NLTR")
                            {
                                ldRtPrm.decideNLTR = true;
                            }
                            else if (type == "NL")
                            {
                                ldRtPrm.decideNL = true;
                            }

                            ldRtPrm.ReturnCode = "CRM - SUCCESS";
                            ldRtPrm.Message = "";
                        }
                        else
                        {
                            if (type == "NLTR")
                            {
                                ldRtPrm.decideNLTR = false;
                            }
                            else if (type == "NL")
                            {
                                ldRtPrm.decideNL = false;
                            }

                            ldRtPrm.ReturnCode = "CRM - SUCCESS";
                            ldRtPrm.Message = "";
                        }

                        if (type == "NLTR")
                        {
                            this._commonFunc.SetMvalue<DedupDgChkNLTR>("NLTR" + ApplicantId, 60, ldRtPrm);
                        }
                        else if (type == "NL")
                        {
                            this._commonFunc.SetMvalue<DedupDgChkNL>("NL" + ApplicantId, 60, ldRtPrm);
                        }

                    }
                }
                catch (Exception ex)
                {

                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Resource_n_Found;
                }

            }

                return ldRtPrm;
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
