﻿namespace AccountLead
{
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Security.Cryptography;
    using System.Xml.Linq;
    using CRMConnect;
    using System.Xml;
    using System.Threading.Channels;
    using System.ComponentModel;
    using System;

    public class CrdgAccLeadExecution : ICrdgAccLeadExecution
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

        public string appkey { set; get; }

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

        public CrdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<AccountLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            AccountLeadReturn ldRtPrm = new AccountLeadReturn();
            RequestData = await this.getRequestData(RequestData, "CreateDigiAccountLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateDigiAccountLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (this.ValidateAccountLead(RequestData.accountLead) && this.ValidateAccountApplicent(RequestData.CustomerAccountLeadRelation))
                        {
                            if (!string.IsNullOrEmpty(leadId) && leadId != "")
                            {
                                ldRtPrm = await this.getApplicentToProduct(leadId, CategoryCode);
                            }
                            else if (!string.IsNullOrEmpty(customerID) && customerID != "")
                            {
                                ldRtPrm = await this.getUcicToProduct(customerID, CategoryCode);
                            }
                            else
                            {
                                this._logger.LogInformation("ValidateLeadtInput", "Input parameters are incorrect");
                                ldRtPrm.ReturnCode = "CRM-ERROR-102";
                                ldRtPrm.Message = OutputMSG.Resource_n_Found;
                            }
                            

                        }
                        else
                        {
                            this._logger.LogInformation("ValidateLeadtInput", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                    }
                    else
                    {
                        this._logger.LogInformation("ValidateLeadtInput", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateLeadtInput", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateLeadtInput", ex.Message);
                throw ex;
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

        

        public async Task<AccountLeadReturn> getUcicToProduct(string customerID, string CategoryCode)
        {
            AccountLeadReturn csRtPrm = new AccountLeadReturn();
            try
            {
                string CategoryId = await this._commonFunc.getCategoryId(CategoryCode);
                JArray applicentDetails = await this._commonFunc.getCustomerDetails(customerID);
                if (applicentDetails.Count > 0)
                {
                    productFilter product_Filter = new productFilter();

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_gender"].ToString()) && applicentDetails[0]["eqs_gender"].ToString() == "789030001")
                    {
                        product_Filter.gender = "Woman";
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_age"].ToString()))
                    {
                        product_Filter.age = applicentDetails[0]["eqs_age"].ToString();
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["_eqs_subentitytypeid_value"].ToString()))
                    {
                        string subentity = await this._commonFunc.getSubentity(applicentDetails[0]["_eqs_subentitytypeid_value"].ToString());
                        if (subentity.ToLower() == "foreigners")
                        {
                            product_Filter.subentity = "NRI";
                        }
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_customersegment"].ToString()) && applicentDetails[0]["eqs_customersegment"].ToString() == "789030002")
                    {
                        product_Filter.customerSegment = "elite";
                    }

                    if (!string.IsNullOrEmpty(applicentDetails[0]["eqs_isstafffcode"].ToString()))
                    {
                        product_Filter.gender = "staff";
                    }

                    product_Filter.productCategory = CategoryId;

                    csRtPrm = await this.getProductDetails(product_Filter, CategoryCode);
                }
            }
            catch(Exception ex)
            {
                this._logger.LogError("getUcicToProduct", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            
            

            return csRtPrm;
        }

        private async Task<bool> ValidateAccountLead(dynamic AccountData)
        {
            return true;
        }

        private async Task<bool> ValidateAccountApplicent(dynamic ApplicentData)
        {
            return true;
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