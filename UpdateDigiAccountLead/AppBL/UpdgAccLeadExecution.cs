namespace UpdateAccountLead
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

    public class UpdgAccLeadExecution : IUpdgAccLeadExecution
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

        private string Leadid, LeadAccountid, DDEId;

        private List<string> applicents = new List<string>();
        private LeadAccount _accountLead;
        private LeadDetails _leadParam;
        private List<AccountApplicant> _accountApplicants;

        Dictionary<string, string> AccountType = new Dictionary<string, string>();
        Dictionary<string, string> KitOption = new Dictionary<string, string>();
        Dictionary<string, string> DepositMode = new Dictionary<string, string>();
        Dictionary<string, string> Operations = new Dictionary<string, string>();
        Dictionary<string, string> LOB = new Dictionary<string, string>();
        Dictionary<string, string> AOBO = new Dictionary<string, string>();
        Dictionary<string, string> purpose = new Dictionary<string, string>();
        Dictionary<string, string> FundSource = new Dictionary<string, string>();
        Dictionary<string, string> currency = new Dictionary<string, string>();
        Dictionary<string, string> ChequeLeaves = new Dictionary<string, string>();

        private ICommonFunction _commonFunc;

        public UpdgAccLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadDetails();
            _accountLead = new LeadAccount();
            _accountApplicants = new List<AccountApplicant>();

            AccountType.Add("Single", "615290000");
            AccountType.Add("Joint", "615290001");

            KitOption.Add("Non Insta Kit", "615290000");
            KitOption.Add("Insta Kit", "615290001");
            KitOption.Add("Instant A/C No kit", "615290002");

            DepositMode.Add("Cheque", "789030000");
            DepositMode.Add("Cash", "789030001");
            DepositMode.Add("Remittance (NRI)", "789030002");
            DepositMode.Add("Cheque from Existing NRI Account", "789030003");
            DepositMode.Add("IP waiver", "789030004");
            DepositMode.Add("Fund Transfer", "789030005");

            Operations.Add("Jointly", "615290004");
            Operations.Add("Either or survivor", "615290005");
            Operations.Add("Anyone", "615290006");
            Operations.Add("Former or Survivor", "615290007");

            LOB.Add("Not Applicable", "615290000");
            LOB.Add("Equitas LOB", "615290001");
            LOB.Add("Business Banking", "615290002");
            LOB.Add("Emerging Enterprise Banking", "615290003");
            LOB.Add("Outreach Banking", "615290004");
            LOB.Add("Liability Sales", "615290005");
            LOB.Add("Alternate Channels", "615290006");
            LOB.Add("Treasury", "615290007");
            LOB.Add("Centralised Processing Center Assets", "615290008");
            LOB.Add("Branch Banking", "615290009");
            LOB.Add("SME Banking", "615290010");
            LOB.Add("Centralised Processing Center Liabilities", "615290011");
            LOB.Add("Corporate Banking", "615290012");
            LOB.Add("Head Office", "615290013");
            LOB.Add("AgriMicroEnterprise and Inclusive Banking", "615290014");

            AOBO.Add("Not Applicable", "789030000");
            AOBO.Add("MSE", "789030001");
            AOBO.Add("CPB", "789030002");

            purpose.Add("Saving","789030000");
            purpose.Add("Repayment Of Loan","789030001");
            purpose.Add("Business Collection Of Instruments", "789030002");
            purpose.Add("Others", "789030003");
            purpose.Add("School/College Fee", "789030004");
            purpose.Add("Daughter/Son Marriage", "789030005");
            purpose.Add("Construction/Renovation Of House", "789030006");
            purpose.Add("Purchase Of Vehicle", "789030007");
            purpose.Add("Festival", "789030008");
            purpose.Add("Buying Home Needs", "789030009");
            purpose.Add("Purchase Of Gold", "789030010");

            FundSource.Add("Salary", "789030000");
            FundSource.Add("Savings", "789030001");
            FundSource.Add("Parents", "789030002");
            FundSource.Add("ental / Dividends", "789030003");
            FundSource.Add("Others", "789030004");

            currency.Add("INR", "615290000");
            currency.Add("USD", "615290001");

            ChequeLeaves.Add("10", "615290000");
            ChequeLeaves.Add("25", "615290001");
            ChequeLeaves.Add("50", "615290002");

        }


        public async Task<UpAccountLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            UpAccountLeadReturn ldRtPrm = new UpAccountLeadReturn();
            RequestData = await this.getRequestData(RequestData, "UpdateDigiAccountLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "UpdateDigiAccountLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        
                        if (!string.IsNullOrEmpty(RequestData.LeadAccountId.ToString()))
                        {
                            ldRtPrm = await this.CreateDDELeadAccount(RequestData);
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


        private async Task<UpAccountLeadReturn> CreateDDELeadAccount(dynamic RequestData)
        {
            UpAccountLeadReturn accountLeadReturn = new UpAccountLeadReturn();

            var _leadDetails  = await this._commonFunc.getLeadAccountDetails(RequestData.LeadAccountId.ToString());
            if (_leadDetails.Count>0)
            {
                if(await SetLeadAccountDDE(_leadDetails, RequestData))
                {
                    
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


    

        private async Task<bool> SetLeadAccountDDE(JArray LeadAccount, dynamic ddeData)
        {
            try
            {
                Dictionary<string, string> odatab = new Dictionary<string, string>();
                Dictionary<string, double> odatab1 = new Dictionary<string, double>();

                // odatab.Add("eqs_applicationdate", LeadAccount[0]["eqs_applicationdate"].ToString("yyyy-MM-dd"));
                if (!string.IsNullOrEmpty(LeadAccount[0]["eqs_ddefinalid"].ToString()))
                {
                    this.DDEId = LeadAccount[0]["eqs_ddefinalid"].ToString();
                }
                
                odatab.Add("eqs_leadaccountid@odata.bind", $"eqs_leadaccounts({LeadAccount[0]["eqs_leadaccountid"].ToString()})");
                this.LeadAccountid = LeadAccount[0]["eqs_leadaccountid"].ToString();

                if (!string.IsNullOrEmpty(ddeData.productCategory.ToString()))
                {
                    string catId = await this._commonFunc.getProductCategoryId(ddeData.productCategory.ToString());
                    odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({catId})");
                }
                else
                {
                    odatab.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({LeadAccount[0]["_eqs_typeofaccountid_value"].ToString()})");
                }

                if (!string.IsNullOrEmpty(ddeData.productCode.ToString()))
                {
                    string proId = await this._commonFunc.getProductId(ddeData.productCode.ToString());
                    odatab.Add("eqs_productid@odata.bind", $"eqs_productcategories({proId})");
                }
                else
                {
                    odatab.Add("eqs_productid@odata.bind", $"eqs_products({LeadAccount[0]["_eqs_productid_value"].ToString()})");
                }

                

                odatab.Add("eqs_ddeoperatorname", "Final");

                odatab.Add("eqs_isnomineedisplay", (Convert.ToBoolean(ddeData.IsNomineeDisplay)) ? "789030001" : "789030000");
                odatab.Add("eqs_isnomineebankcustomer", (Convert.ToBoolean(ddeData.IsNomineeBankCustomer)) ? "789030001" : "789030000");
                odatab.Add("eqs_sweepfacility", (Convert.ToBoolean(ddeData.SweepFacility)) ? "true" : "false");
                odatab.Add("eqs_producteligibilityflag", (Convert.ToBoolean(ddeData.ProductEligibilityFlag)) ? "true" : "false");

                odatab.Add("eqs_lgcode", ddeData.LGCode.ToString());
                odatab.Add("eqs_lccode", ddeData.LCCode.ToString());

                odatab.Add("eqs_leadchannelcode", "789030011");
                odatab.Add("eqs_dataentrystage", "615290002");
                odatab.Add("eqs_purposeofopeningaccountcode", this.purpose[ddeData.OpeningAccountPurpose.ToString()]);

                odatab.Add("eqs_instakitcode", this.KitOption[ddeData.InstaKit.ToString()]);
                odatab.Add("eqs_accountownershipcode", this.AccountType[ddeData.accountType.ToString()]);
                odatab.Add("eqs_initialdepositmodecode", this.DepositMode[ddeData.initialDepositType.ToString()]);
                odatab.Add("eqs_accounttitle", ddeData.AccountTitle.ToString());
                odatab.Add("eqs_modeofoperationcode", this.Operations[ddeData.ModeofOperation.ToString()]);
                odatab.Add("eqs_lobtypecode", this.LOB[ddeData.LOB.ToString()]);
                odatab.Add("eqs_predefinedaccountnumber", ddeData.PredefinedAcNumber.ToString());
                odatab1.Add("eqs_depositamount", Convert.ToDouble(ddeData.DepositAmount.ToString()));

                odatab.Add("eqs_sourceoffundcode", this.FundSource[ddeData.SourceofFund.ToString()]);
                odatab.Add("eqs_currencyofdepositcode", this.currency[ddeData.Currency.ToString()]);

                odatab.Add("eqs_issuedinstakit", (Convert.ToBoolean(ddeData.IssuedInstaKit)) ? "true" : "false");
                odatab.Add("eqs_chequebookrequired", (Convert.ToBoolean(ddeData.ChequeBookRequired)) ? "true" : "false");
                odatab.Add("eqs_numberofchequebook", ddeData.NumberofChequeBook.ToString());
                odatab.Add("eqs_numberofchequeleavescode", this.ChequeLeaves[ddeData.NumberofChequeLeaves.ToString()]);
                odatab.Add("eqs_dispatchmodecode", (ddeData.IsNomineeBankCustomer.ToString()== "Dispatch to Customer") ? "615290000" : "615290001");
                odatab.Add("eqs_aobotypecode", this.AOBO[ddeData.AOBO.ToString()]);

                var leadDetails = await this._commonFunc.getLeadDetails(LeadAccount[0]["_eqs_lead_value"].ToString());
                odatab.Add("eqs_leadnumber", leadDetails[0]["eqs_crmleadid"].ToString());
                if (!string.IsNullOrEmpty(leadDetails[0]["_eqs_leadsourceid_value"].ToString()))
                {
                    odatab.Add("eqs_leadsourceid@odata.bind", $"eqs_leadsources({leadDetails[0]["_eqs_leadsourceid_value"].ToString()})");
                }
                
                odatab.Add("eqs_sourcebranchterritoryid@odata.bind", $"eqs_branchs({leadDetails[0]["_eqs_branchid_value"].ToString()})");
                odatab.Add("eqs_accountopeningbranchid@odata.bind", $"eqs_branchs({leadDetails[0]["_eqs_branchid_value"].ToString()})");



                string postDataParametr = JsonConvert.SerializeObject(odatab);
                string postDataParametr1 = JsonConvert.SerializeObject(odatab1);
                postDataParametr = await _commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    var LeadAccount_details = await this._queryParser.HttpApiCall("eqs_ddeaccounts()?$select=eqs_ddeaccountid", HttpMethod.Post, postDataParametr);
                    odatab = new Dictionary<string, string>();
                    var ddeid = CommonFunction.GetIdFromPostRespons201(LeadAccount_details[0]["responsebody"], "eqs_ddeaccountid");
                    odatab.Add("eqs_ddefinalid", ddeid);
                    postDataParametr = JsonConvert.SerializeObject(odatab);
                    await this._queryParser.HttpApiCall($"eqs_leadaccounts({this.LeadAccountid})", HttpMethod.Patch, postDataParametr);
                }
                else
                {
                    var LeadAccount_details = await this._queryParser.HttpApiCall($"eqs_ddeaccounts({this.DDEId})?$select=eqs_ddeaccountid", HttpMethod.Patch, postDataParametr);
                }
                
                

                return true;
            }
            catch (Exception ex)
            {
                return false;
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
