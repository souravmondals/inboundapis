namespace AccountLead
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

        private AccountLead _accountLead;
        private List<AccountApplicant> _accountApplicants;
        private ICommonFunction _commonFunc;

        public CrdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _accountLead = new AccountLead();
            _accountApplicants = new List<AccountApplicant>();
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
                            if (this.ValidateUCIC(_accountApplicants))
                            {
                                ldRtPrm = await this.CreateAccountLead();
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


        private AccountLeadReturn CreateAccountLead()
        {
            AccountLeadReturn accountLeadReturn = new AccountLeadReturn();
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

        

       
        private bool ValidateUCIC(List<AccountApplicant> accountApplicants)
        {

            return true;
        }

        private bool ValidateAccountLead(dynamic AccountData)
        {
            if (string.IsNullOrEmpty(AccountData.accountType.ToString()))
            {
                return false;                
            }
            else
            {
                _accountLead.accountType = AccountData.accountType.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.productCategory.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.productCategory = AccountData.productCategory.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.sourceBranch.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.sourceBranch = AccountData.sourceBranch.ToString();
            }
            
            if (string.IsNullOrEmpty(AccountData.accountOpeningFlow.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.accountOpeningFlow = AccountData.accountOpeningFlow.ToString();
            }

            if (string.IsNullOrEmpty(AccountData.depositAmount.ToString()))
            {
                return false;
            }
            else
            {
                _accountLead.depositAmount = AccountData.depositAmount.ToString();
            }

            _accountLead.productCategory = AccountData.productCategory;
            _accountLead.initialDepositType = AccountData.initialDepositType;
            _accountLead.fieldEmployeeCode = AccountData.fieldEmployeeCode;
            _accountLead.applicationDate = AccountData.applicationDate;
            _accountLead.tenureInMonths = AccountData.tenureInMonths;
            _accountLead.tenureInDays = AccountData.tenureInDays;
            _accountLead.rateOfInterest = AccountData.rateOfInterest;
            _accountLead.fundsTobeDebitedFrom = AccountData.fundsTobeDebitedFrom;
            _accountLead.mopRemarks = AccountData.mopRemarks;
            _accountLead.fdAccOpeningDate = AccountData.fdAccOpeningDate;
            _accountLead.sweepFacility = AccountData.sweepFacility;
            

            return true;
        }

        private bool ValidateAccountApplicent(dynamic ApplicentData)
        {
            if (ApplicentData.Count>0) 
            {
                foreach (var item in ApplicentData)
                {
                    AccountApplicant accountApplicant = new AccountApplicant(); 
                    if (string.IsNullOrEmpty(item.UCIC.ToString()))
                    {
                        return false;
                    }
                    else
                    {
                        accountApplicant.UCIC = item.UCIC.ToString();
                    }
                    
                    if (string.IsNullOrEmpty(item.isPrimaryHolder.ToString()))
                    {
                        return false;
                    }
                    else
                    {
                        accountApplicant.isPrimaryHolder = item.isPrimaryHolder;
                    }
                    
                    if (string.IsNullOrEmpty(item.customerPhoneNumber.ToString()))
                    {
                        return false;
                    }
                    else
                    {
                        accountApplicant.customerPhoneNumber = item.customerPhoneNumber;
                    }

                    accountApplicant.customerAccountRelation = item.customerAccountRelation;
                    accountApplicant.customerAccountRelationTitle = item.customerAccountRelationTitle;
                    accountApplicant.relationToPrimaryHolder = item.relationToPrimaryHolder;
                    accountApplicant.entityType = item.entityType;
                    accountApplicant.subentityType = item.subentityType;
                    accountApplicant.customerName = item.customerName;
                    accountApplicant.age = item.age;
                    accountApplicant.isStaff = item.isStaff;
                    accountApplicant.customerEmailID = item.customerEmailID;
                    accountApplicant.gender = item.gender;
                    accountApplicant.pan = item.pan;
                    accountApplicant.dob = item.dob;

                    var preferencest = JsonConvert.SerializeObject(item.CustomerPreferences);
                    accountApplicant.preferences = JsonConvert.DeserializeObject<Preferences>(preferencest);
                    _accountApplicants.Add(accountApplicant);
                }
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
