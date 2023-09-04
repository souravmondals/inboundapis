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
    using System.Security.Cryptography.Xml;
    using Azure;
    using Microsoft.Identity.Client;
    using Microsoft.Azure.KeyVault.Models;

    public class CrdgAccByLeadExecution : ICrdgAccByLeadExecution
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

        private List<string> applicents = new List<string>();
        private AccountLead _accountLead;
        private LeadParam _leadParam;
        private List<AccountApplicant> _accountApplicants;
       
        private ICommonFunction _commonFunc;

        public CrdgAccByLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadParam();
            _accountLead = new AccountLead();
            _accountApplicants = new List<AccountApplicant>();

            
        }


        public async Task<AccountByLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            AccountByLeadReturn ldRtPrm = new AccountByLeadReturn();
            RequestData = await this.getRequestData(RequestData, "CreateDigiAccountByLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateDigiAccountByLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.accountLead.ToString()))
                        {
                            ldRtPrm = await this.CreateAccountByLead(RequestData.accountLead.ToString());
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


        private async Task<AccountByLeadReturn> CreateAccountByLead(string accountLead)
        {
            AccountByLeadReturn accountLeadReturn = new AccountByLeadReturn();
            try
            {               

                string Token = await this._queryParser.getAccessToken();
                string RequestTemplate = "{\"createAccountRequest\":{\"msgHdr\":{\"channelID\":\"FOS\",\"transactionType\":\"create\",\"transactionSubType\":\"account\",\"conversationID\":\"FOS2023071305441888603509\",\"externalReferenceId\":\"FOS2023071305441888603509\",\"isAsync\":false,\"authInfo\":{\"branchID\":\"1002\",\"userID\":\"IBUSER\",\"token\":\"1002\"}},\"msgBdy\":{\"accountNo\":\"\",\"accountNominee\":{\"city\":\"CHENNAI\",\"country\":\"IN\",\"dateOfBirth\":\"19880514\",\"isNomineeDisplay\":\"false\",\"isNomineeBankCustomer\":\"false\",\"nominee\":{\"phone\":{\"country\":\"9834\",\"area\":\"91\",\"number\":\"7678982345\",\"extn\":\"2342\"},\"address\":{\"line1\":\"Adrs1\",\"line2\":\"Adrs2\",\"line3\":\"Adrs3\"},\"emailId\":\"bhr@gml.com\",\"name\":\"BHR\"},\"guardian\":{\"phone\":{\"area\":\"293\",\"number\":\"\"},\"address\":{\"line1\":\"Gadds1\",\"line2\":\"Gadds2\",\"line3\":\"Gadds3\",\"line4\":\"string\",\"city\":\"CHENNAI\",\"area\":\"string\",\"state\":\"TAMIL NADU\",\"country\":\"IN\",\"zip\":\"600040\"},\"emailId\":\"jnas@s1.com\",\"name\":\"Grdnbhr\"},\"refGuardPhnCountry\":\"424\",\"refGuardPhnExtn\":\"334534\",\"relAcctHolder\":\"15\",\"relationGuardian\":\"\",\"shareAmount\":100,\"sharePercentage\":100,\"state\":\"TAMIL NADU\",\"zip\":\"600041\",\"customerId\":\"\"},\"branchCode\":9999,\"customerAndRelation\":[{\"customerId\":\"13972557\",\"customerName\":\"MayankAccountCreationTest\",\"relation\":\"JAF\"},{\"customerId\":\"13972559\",\"customerName\":\"SendoryApplicant\",\"relation\":\"NOM\"}],\"customerID\":\"13972557\",\"isJointHolder\":\"true\",\"isRestrictAcct\":false,\"transactionType\":\"A\",\"minorAcctStatus\":false,\"productCode\":\"1025\",\"tdaccountPayinRequest\":{\"depositAmount\":0,\"fromAccountNo\":\"\",\"termDays\":0,\"termMonths\":0},\"rdaccountPayinRequest\":{\"installmentAmount\":0,\"payoutAccountNo\":\"\",\"termMonths\":0}}}}";
                dynamic Request_Template = JsonConvert.DeserializeObject(RequestTemplate);
                dynamic msgHdr = Request_Template.createAccountRequest.msgHdr;
                dynamic msgBdy = Request_Template.createAccountRequest.msgBdy;
                Guid ReferenceId = Guid.NewGuid();
                msgHdr.conversationID = ReferenceId.ToString().Replace("-","");
                msgHdr.externalReferenceId = ReferenceId.ToString().Replace("-", "");
                Request_Template.createAccountRequest.msgHdr = msgHdr;

                Dictionary<string, string> odatab = new Dictionary<string, string>();

                var AccountDDE = await this._commonFunc.getAccountLeadData(accountLead);
                if (AccountDDE.Count > 0)
                {
                    var Nominee = await this._commonFunc.getAccountNominee(AccountDDE[0]["eqs_ddeaccountid"].ToString());
                    var AccApplicent = await this._commonFunc.getAccountApplicd(AccountDDE[0]["_eqs_leadaccountid_value"].ToString());

                    if (Nominee.Count > 0)
                    {
                        if (!string.IsNullOrEmpty(Nominee[0]["_eqs_city_value"].ToString()))
                        {
                            msgBdy.accountNominee.city = await this._commonFunc.getCityName(Nominee[0]["_eqs_city_value"].ToString());
                        }

                        if (!string.IsNullOrEmpty(Nominee[0]["_eqs_state_value"].ToString()))
                        {
                            msgBdy.accountNominee.state = "TAMIL NADU";  //await this._commonFunc.getStateName(Nominee[0]["_eqs_state_value"].ToString());
                        }

                        if (!string.IsNullOrEmpty(Nominee[0]["_eqs_country_value"].ToString()))
                        {
                            msgBdy.accountNominee.country = "IN";    //await this._commonFunc.getCountryName(Nominee[0]["_eqs_country_value"].ToString());
                        }

                        string dd = Nominee[0]["eqs_nomineedob"].ToString().Substring(0, 2);
                        string mm = Nominee[0]["eqs_nomineedob"].ToString().Substring(3, 2);
                        string yy = Nominee[0]["eqs_nomineedob"].ToString().Substring(6, 4);
                        msgBdy.accountNominee.dateOfBirth = yy + mm + dd;
                        msgBdy.accountNominee.nominee.phone.number = Nominee[0]["eqs_mobile"].ToString();

                        msgBdy.accountNominee.nominee.address.line1 = Nominee[0]["eqs_addressline1"].ToString();
                        msgBdy.accountNominee.nominee.address.line2 = Nominee[0]["eqs_addressline2"].ToString();
                        msgBdy.accountNominee.nominee.address.line3 = Nominee[0]["eqs_addressline3"].ToString();

                        msgBdy.accountNominee.nominee.emailId = Nominee[0]["eqs_emailid"].ToString();
                        msgBdy.accountNominee.nominee.name = Nominee[0]["eqs_nomineename"].ToString();

                        if (!string.IsNullOrEmpty(Nominee[0]["eqs_guardianname"].ToString()))
                        {
                            msgBdy.accountNominee.guardian.name = Nominee[0]["eqs_guardianname"].ToString();
                            msgBdy.accountNominee.guardian.phone.number = Nominee[0]["eqs_guardianmobile"].ToString();

                            msgBdy.accountNominee.guardian.address.line1 = Nominee[0]["eqs_guardianaddressline1"].ToString();
                            msgBdy.accountNominee.guardian.address.line2 = Nominee[0]["eqs_guardianaddressline2"].ToString();
                            msgBdy.accountNominee.guardian.address.line3 = Nominee[0]["eqs_guardianaddressline3"].ToString();

                            if (!string.IsNullOrEmpty(Nominee[0]["_eqs_guardiancity_value"].ToString()))
                            {
                                msgBdy.accountNominee.guardian.address.city = await this._commonFunc.getCityName(Nominee[0]["_eqs_guardiancity_value"].ToString());
                            }

                            if (!string.IsNullOrEmpty(Nominee[0]["_eqs_guardianstate_value"].ToString()))
                            {
                                msgBdy.accountNominee.guardian.address.state = await this._commonFunc.getStateName(Nominee[0]["_eqs_guardianstate_value"].ToString());
                            }

                            if (!string.IsNullOrEmpty(Nominee[0]["_eqs_guardiancountry_value"].ToString()))
                            {
                                msgBdy.accountNominee.guardian.address.country = await this._commonFunc.getCountryName(Nominee[0]["_eqs_guardiancountry_value"].ToString());
                            }
                            msgBdy.accountNominee.guardian.address.zip = Nominee[0]["eqs_guardianpincode"].ToString();
                        }

                        msgBdy.accountNominee.zip = Nominee[0]["eqs_pincode"].ToString();
                    }

                    List<ApplicentRelation> relationList = new List<ApplicentRelation>();
                    foreach (var item in AccApplicent)
                    {
                        ApplicentRelation applicentRelation = new ApplicentRelation();
                        applicentRelation.customerId = item["eqs_customer"].ToString();
                        applicentRelation.customerName = item["eqs_name"].ToString();
                        applicentRelation.relation = await this._commonFunc.getAccountRelation(item["_eqs_accountrelationship_value"].ToString());

                        if (item["eqs_isprimaryholder"].ToString() == "789030001")
                        {
                            msgBdy.customerID = item["eqs_customer"].ToString();
                        }
                        relationList.Add(applicentRelation);
                    }

                    msgBdy.customerAndRelation = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(relationList));
                    msgBdy.isJointHolder = (AccountDDE[0]["eqs_accountownershipcode"].ToString() == "615290001") ? "true" : "false";
                    msgBdy.productCode = await this._commonFunc.getProductCode(AccountDDE[0]["_eqs_productid_value"].ToString());

                    Request_Template.createAccountRequest.msgBdy = msgBdy;

                    string postDataParametr = await EncriptRespons(JsonConvert.SerializeObject(Request_Template));
                    string Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSCreateAccount", postDataParametr);
                    dynamic responsD = JsonConvert.DeserializeObject(Lead_details);
                    
                    if(responsD.msgHdr != null && responsD.msgHdr.result.ToString() == "ERROR")
                    {
                        accountLeadReturn.Message = responsD.msgHdr.error[0].reason.ToString();
                        accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                    }
                    else
                    {
                        Dictionary<string,string> fieldInput = new Dictionary<string,string>();
                        
                        accountLeadReturn.AccountNo = responsD.createAccountResponse.msgBdy.accountNo.ToString();
                        fieldInput.Add("eqs_accountnocreated", accountLeadReturn.AccountNo);
                        postDataParametr = JsonConvert.SerializeObject(fieldInput);

                        await this._queryParser.HttpApiCall($"eqs_ddeaccounts({AccountDDE[0]["eqs_ddeaccountid"].ToString()})", HttpMethod.Patch, postDataParametr);
                        
                        accountLeadReturn.Message = OutputMSG.Case_Success;
                        accountLeadReturn.ReturnCode = "CRM-SUCCESS";
                    }                    

                }
                else
                {
                    accountLeadReturn.Message = OutputMSG.Resource_n_Found;
                    accountLeadReturn.ReturnCode = "CRM-ERROR-101";
                }
               
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateAccountByLead", ex.Message);
                accountLeadReturn.Message = OutputMSG.Incorrect_Input;
                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
            }

            return accountLeadReturn;

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
