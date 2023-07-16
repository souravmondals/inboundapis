namespace FetchAccountLead
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

    public class FtdgAccLeadExecution : IFtdgAccLeadExecution
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

        private string Leadid, LeadAccountid;

        private List<string> applicents = new List<string>();
        private LeadAccount _accountLead;
        private LeadDetails _leadParam;
        private List<AccountApplicant> _accountApplicants;

        Dictionary<string, string> AccountType = new Dictionary<string, string>();
        Dictionary<string, string> KitOption = new Dictionary<string, string>();
        Dictionary<string, string> DepositMode = new Dictionary<string, string>();
        private ICommonFunction _commonFunc;

        public FtdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadDetails();
            _accountLead = new LeadAccount();
            _accountApplicants = new List<AccountApplicant>();

            AccountType.Add("615290000", "Single");
            AccountType.Add("615290001", "Joint");

            KitOption.Add("615290000", "Non Insta Kit");
            KitOption.Add("615290001", "Insta Kit");
            KitOption.Add("615290002", "Instant A/C No kit");

            DepositMode.Add("789030000", "Cheque");
            DepositMode.Add("789030001", "Cash");
            DepositMode.Add("789030002", "Remittance (NRI)");
            DepositMode.Add("789030003", "Cheque from Existing NRI Account");
            DepositMode.Add("789030004", "IP waiver");
            DepositMode.Add("789030005", "Fund Transfer");
        }


        public async Task<FtAccountLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            FtAccountLeadReturn ldRtPrm = new FtAccountLeadReturn();
            RequestData = await this.getRequestData(RequestData, "FetchDigiAccountLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchDigiAccountLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        
                        if (!string.IsNullOrEmpty(RequestData.AccountLeadId.ToString()))
                        {
                            ldRtPrm = await this.FetLeadAccount(RequestData.AccountLeadId.ToString());
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

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateLeadtInput", ex.Message);
                throw ex;
            }
            
        }


        private async Task<FtAccountLeadReturn> FetLeadAccount(string LeadAccountId)
        {
            FtAccountLeadReturn accountLeadReturn = new FtAccountLeadReturn();

            var _leadDetails  = await this._commonFunc.getLeadAccountDetails(LeadAccountId);
            if (_leadDetails.Count>0)
            {
                if(await getLeadAccount(_leadDetails))
                {
                    if (await getLeadData())
                    {
                        await getApplicent();
                    }
                }
            }
            else
            {
                this._logger.LogInformation("FetLeadAccount", "Input parameters are incorrect");
                accountLeadReturn.ReturnCode = "CRM-ERROR-102";
                accountLeadReturn.Message = OutputMSG.Incorrect_Input;
            }


            

            return accountLeadReturn;
        }


        private async Task<bool> getLeadData()
        {

            var _leadDetails = await this._commonFunc.getLeadDetails(this.Leadid);

            _leadParam.ucic = _leadDetails[0]["eqs_ucic"].ToString();
            _leadParam.leadid = _leadDetails[0]["eqs_crmleadid"].ToString();
            _leadParam.Firstname = _leadDetails[0]["firstname"].ToString();
            _leadParam.Lastname = _leadDetails[0]["lastname"].ToString();
            _leadParam.CustomerPhoneNumber = _leadDetails[0]["mobilephone"].ToString();
            _leadParam.CustomerEmailID = _leadDetails[0]["emailaddress1"].ToString();
            _leadParam.companynamepart1 = _leadDetails[0]["eqs_companynamepart1"].ToString();
            _leadParam.companynamepart2 = _leadDetails[0]["eqs_companynamepart2"].ToString();
            _leadParam.companynamepart3 = _leadDetails[0]["eqs_companynamepart3"].ToString();

            _leadParam.dateofincorporation = _leadDetails[0]["eqs_dateofincorporation"].ToString();
            _leadParam.DOB = _leadDetails[0]["eqs_dob"].ToString();
            _leadParam.PAN = _leadDetails[0]["eqs_pan"].ToString();
            _leadParam.gender = _leadDetails[0]["eqs_gendercode"].ToString();

            _leadParam.branchid = await this._commonFunc.getBranchCode(_leadDetails[0]["_eqs_branchid_value"].ToString());
            _leadParam.productid = await this._commonFunc.getProductCode(_leadDetails[0]["_eqs_productid_value"].ToString());
            _leadParam.productCategory = await this._commonFunc.getProductCategoryCode(_leadDetails[0]["_eqs_productcategoryid_value"].ToString());
            _leadParam.EntityType = await this._commonFunc.getEntityCode(_leadDetails[0]["_eqs_entitytypeid_value"].ToString());
            _leadParam.SubEntityType = await this._commonFunc.getSubEntityCode(_leadDetails[0]["_eqs_subentitytypeid_value"].ToString());
            

            return true;
        }

        private async Task<bool> getLeadAccount(JArray LeadData)
        {                

            this.LeadAccountid = LeadData[0]["eqs_leadaccountid"].ToString();
            this.Leadid = LeadData[0]["_eqs_lead_value"].ToString();

            _accountLead.productCode = await this._commonFunc.getProductCode(LeadData[0]["_eqs_productid_value"].ToString());

            if (!string.IsNullOrEmpty(LeadData[0]["_eqs_branchid_value"].ToString())) {
                _accountLead.sourceBranch = await this._commonFunc.getBranchCode(LeadData[0]["_eqs_branchid_value"].ToString());
            }

            _accountLead.productCategory = await this._commonFunc.getProductCategoryCode(LeadData[0]["_eqs_typeofaccountid_value"].ToString()); 

            _accountLead.LeadAccountID = LeadData[0]["eqs_crmleadaccountid"].ToString();
            _accountLead.accountType = this.AccountType[LeadData[0]["eqs_accountownershipcode"].ToString()];
            _accountLead.accountOpeningFlow = this.KitOption[LeadData[0]["eqs_instakitoptioncode"].ToString()];
            _accountLead.initialDepositType = this.DepositMode[LeadData[0]["eqs_initialdepositmodecode"].ToString()];
            _accountLead.fdAccOpeningDate = LeadData[0]["eqs_fdvaluedate"].ToString();
            _accountLead.fieldEmployeeCode = LeadData[0]["eqs_sourcebyemployeecode"].ToString();
            _accountLead.applicationDate = LeadData[0]["eqs_applicationdate"].ToString();
            _accountLead.fundsTobeDebitedFrom = LeadData[0]["eqs_fundstobedebitedfrom"].ToString();
            _accountLead.mopRemarks = LeadData[0]["eqs_modeofoperationremarks"].ToString();
            _accountLead.initialDeposit = (LeadData[0]["eqs_initialdepositamountcode"].ToString() == "789030000") ? "Up To Rs. 500000" : "Above Rs. 500000";
            _accountLead.rateOfInterest = LeadData[0]["eqs_rateofinterest"].ToString();
            _accountLead.depositAmount = LeadData[0]["eqs_depositamount"].ToString();
            _accountLead.sweepFacility = Convert.ToBoolean(LeadData[0]["eqs_sweepfacility"].ToString());


            return true;      

        }

        private async Task<bool> getApplicent()
        {           
            foreach (var applicant in _accountApplicants)
            {
                Dictionary<string, string> odatab = new Dictionary<string, string>();

              //  odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({_leadParam.LeadAccountid})");
                odatab.Add("eqs_ucic", applicant.UCIC);
                odatab.Add("eqs_customer", applicant.UCIC);
                odatab.Add("eqs_customerid@odata.bind", $"contacts({applicant.contactid})");
                odatab.Add("eqs_titleid@odata.bind", $"eqs_titles({applicant.title})");
                odatab.Add("eqs_leadchannel", "15");

                odatab.Add("eqs_name", applicant.firstname + " " + applicant.lastname);
                odatab.Add("eqs_emailaddress", applicant.customerEmailID);
                odatab.Add("eqs_mobilenumber", applicant.customerPhoneNumber);
                odatab.Add("eqs_gendercode", applicant.gender);
                odatab.Add("eqs_dob", applicant.dob);
                odatab.Add("eqs_pan", applicant.pan);
                odatab.Add("eqs_leadage", applicant.age);

                //if (!string.IsNullOrEmpty(_leadParam.branchid))
                //{
                //    odatab.Add("eqs_branchid@odata.bind", $"eqs_branchs({_leadParam.branchid})");
                //}
                    

            //    odatab.Add("eqs_leadid@odata.bind", $"leads({_leadParam.leadid})");

                odatab.Add("eqs_accountrelationship@odata.bind", $"eqs_accountrelationships({await this._commonFunc.getAccRelationshipId(applicant.customerAccountRelation)})");
                odatab.Add("eqs_isprimaryholder", (applicant.isPrimaryHolder == true) ? "789030001" : "789030000");

                odatab.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({applicant.entityType})");
                odatab.Add("eqs_subentity@odata.bind", $"eqs_subentitytypes({applicant.subentityType})");

                if (applicant.isPrimaryHolder == false && !string.IsNullOrEmpty(applicant.relationToPrimaryHolder))
                {

                    odatab.Add("eqs_relationship@odata.bind", $"eqs_relationships({await this._commonFunc.getRelationshipId(applicant.relationToPrimaryHolder)})");
                }

                string postDataParametr = JsonConvert.SerializeObject(odatab);
                var Applicent_details = await this._queryParser.HttpApiCall("eqs_accountapplicants()?$select=eqs_applicantid", HttpMethod.Post, postDataParametr);

                if (Applicent_details.Count > 0)
                {
                    dynamic respons_code = Applicent_details[0];
                    if (respons_code.responsecode == 201)
                    {
                        applicents.Add(respons_code.responsebody["eqs_applicantid"].ToString());
                        string accountapplicantid = respons_code.responsebody["eqs_accountapplicantid"];

                        Dictionary<string, string> preference = new Dictionary<string, string>();
                        Dictionary<string, bool?> preference1 = new Dictionary<string, bool?>();

                        preference.Add("eqs_applicantid@odata.bind", $"eqs_accountapplicants({accountapplicantid})");
                                               
                        preference1.Add("eqs_sms", applicant.preferences.sms);
                        preference1.Add("eqs_allsmsalerts", applicant.preferences.allSMSAlerts);
                        preference1.Add("eqs_onlytransactionalerts", applicant.preferences.onlyTransactionAlerts);
                        preference1.Add("eqs_passbook", applicant.preferences.passbook);
                        preference1.Add("eqs_physicalstatement", applicant.preferences.physicalStatement);
                        preference1.Add("eqs_emailstatement", applicant.preferences.emailStatement);
                        preference1.Add("eqs_netbanking", applicant.preferences.netBanking);
                        preference1.Add("eqs_bankguarantee", applicant.preferences.bankGuarantee);
                        preference1.Add("eqs_letterofcredit", applicant.preferences.letterofCredit);
                        preference1.Add("eqs_businessloan", applicant.preferences.businessLoan);
                        preference1.Add("eqs_doorstepbanking", applicant.preferences.doorStepBanking);
                        preference1.Add("eqs_doorstepbankingoncall", applicant.preferences.doorStepBankingOnCall);
                        preference1.Add("eqs_doorstepbankingbeat", applicant.preferences.doorStepBankingBeat);
                        preference1.Add("eqs_tradeforex", applicant.preferences.tradeForex);
                        preference1.Add("eqs_loanagainstproperty", applicant.preferences.loanAgainstProperty);
                        preference1.Add("eqs_overdraftagainstfd", applicant.preferences.overdraftsagainstFD);
                        preference1.Add("eqs_preferencescopied", applicant.preferences.preferencesCopied);
                        preference1.Add("eqs_banklevelalerts", applicant.preferences.bankLevelAlerts);

                        //preference.Add("eqs_netbankingrights", applicant.preferences.netBankingRights);
                        //preference.Add("eqs_mobilebankingnumber", applicant.preferences.mobileBankingNumber);
                       // preference.Add("eqs_internationaldclimitact", applicant.preferences.InternationalLimitActivation);

                        postDataParametr = JsonConvert.SerializeObject(preference);
                        string postDataParametr1 = JsonConvert.SerializeObject(preference1);
                        postDataParametr = await _commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                        var Apreferences_details = await this._queryParser.HttpApiCall("eqs_customerpreferences()?", HttpMethod.Post, postDataParametr);

                        if (Apreferences_details.Count < 1)
                        {
                            return false;
                        }

                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

       
            return true;

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
