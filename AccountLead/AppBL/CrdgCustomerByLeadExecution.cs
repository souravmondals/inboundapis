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
    using Microsoft.Identity.Client;
    using Microsoft.Azure.KeyVault.Models;
    using System.Security.Cryptography.X509Certificates;

    public class CrdgCustomerByLeadExecution : ICrdgCustomerByLeadExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;
        public string Bank_Code { set; get; }

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

        public CrdgCustomerByLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            _leadParam = new LeadParam();
            _accountLead = new AccountLead();
            _accountApplicants = new List<AccountApplicant>();

            
        }


        public async Task<CustomerByLeadReturn> ValidateLeadtInput(dynamic RequestData)
        {
            CustomerByLeadReturn ldRtPrm = new CustomerByLeadReturn();
            RequestData = await this.getRequestData(RequestData, "CreateDigiCustomerByLead");
            try
            { 

                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateDigiCustomerByLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.ApplicentID.ToString()))
                        {
                            ldRtPrm = await this.CreateCustomerByLead(RequestData.ApplicentID.ToString());
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


        private async Task<CustomerByLeadReturn> CreateCustomerByLead(string applicentId)
        {
            CustomerByLeadReturn customerLeadReturn = new CustomerByLeadReturn();
            try
            {               

                string Token = await this._queryParser.getAccessToken();

                string RequestTemplate = "{\"createCustomerRequest\":{\"msgHdr\":{\"channelID\":\"VFLOS\",\"transactionType\":\"create\",\"transactionSubType\":\"customer\",\"conversationID\":\"string\",\"externalReferenceId\":\"kjdcbskj9c123424\",\"isAsync\":false,\"authInfo\":{\"branchID\":\"1001\",\"userID\":\"IBUSER\",\"token\":\"1001\"}},\"msgBdy\":{\"misClass\":\"DIVISION\",\"misCode\":\"0\",\"individualCustomer\":{\"address\":{\"line1\":\"TEST1\",\"line2\":\"TEST2\",\"line3\":\"TEST3\",\"line4\":\"TEST4\",\"city\":\"CHENNAI\",\"state\":\"TAMILNADU\",\"country\":\"IN\",\"zip\":\"565556\"},\"category\":\"I\",\"cifType\":\"C\",\"countryOfResidence\":\"IN\",\"customerMobilePhone\":\"919887899899\",\"dateOfBirthOrRegistration\":\"20001205\",\"emailId\":\"emaill@e.com\",\"homeBranchCode\":9999,\"language\":\"ENG\",\"name\":{\"firstName\":\"sall\",\"lastName\":\"SWAMI\",\"midName\":\"\",\"prefix\":\"MR.\",\"shortName\":\"salllu\"},\"customerEducation\":\"5\",\"employeeId\":\"33454\",\"isStaff\":\"Y\",\"maritalStatus\":\"1\",\"motherMaidenName\":\"KOMAL\",\"professionCode\":0,\"sex\":\"M\",\"nationalIdentificationCode\":\"1293\",\"nationality\":\"IN\"}}}}";
                dynamic Request_Template = JsonConvert.DeserializeObject(RequestTemplate);
                dynamic msgHdr = Request_Template.createCustomerRequest.msgHdr;
                dynamic msgBdy = Request_Template.createCustomerRequest.msgBdy;
                Guid ReferenceId = Guid.NewGuid();
                msgHdr.conversationID = ReferenceId.ToString().Replace("-","");
                msgHdr.externalReferenceId = ReferenceId.ToString().Replace("-", "");
                Request_Template.createCustomerRequest.msgHdr = msgHdr;

                Dictionary<string, string> odatab = new Dictionary<string, string>();

                var AccountDDE = await this._commonFunc.getApplicentData(applicentId);
                if (AccountDDE.Count > 0)
                {
                    var address = await this._commonFunc.getAddressData(AccountDDE[0]["eqs_ddeindividualcustomerid"].ToString());
                    if (address.Count > 0)
                    {
                        msgBdy.individualCustomer.address.line1 = address[0]["eqs_addressline1"].ToString();
                        msgBdy.individualCustomer.address.line2 = address[0]["eqs_addressline2"].ToString();
                        msgBdy.individualCustomer.address.line3 = address[0]["eqs_addressline3"].ToString();
                        msgBdy.individualCustomer.address.line4 = address[0]["eqs_addressline4"].ToString();
                        msgBdy.individualCustomer.address.zip = address[0]["eqs_pincode"].ToString();
                        msgBdy.individualCustomer.address.city = await this._commonFunc.getCityName(address[0]["_eqs_cityid_value"].ToString());  //"CHENNAI";
                        msgBdy.individualCustomer.address.state = await this._commonFunc.getStateName(address[0]["_eqs_stateid_value"].ToString());  //"TAMIL NADU";
                        msgBdy.individualCustomer.address.country = "IN";
                    }
                   

                    string dd = AccountDDE[0]["eqs_dob"].ToString().Substring(8, 2);
                    string mm = AccountDDE[0]["eqs_dob"].ToString().Substring(5, 2);
                    string yy = AccountDDE[0]["eqs_dob"].ToString().Substring(0, 4);
                    msgBdy.individualCustomer.dateOfBirthOrRegistration = yy + mm + dd;
                    msgBdy.individualCustomer.customerMobilePhone = AccountDDE[0]["eqs_mobilenumber"].ToString();
                    msgBdy.individualCustomer.emailId = AccountDDE[0]["eqs_emailid"].ToString();

                    msgBdy.individualCustomer.name.firstName = AccountDDE[0]["eqs_firstname"].ToString();
                    msgBdy.individualCustomer.name.lastName = AccountDDE[0]["eqs_lastname"].ToString();
                    msgBdy.individualCustomer.name.midName = AccountDDE[0]["eqs_middlename"].ToString();
                    msgBdy.individualCustomer.name.shortName = AccountDDE[0]["eqs_shortname"].ToString();

                    msgBdy.individualCustomer.employeeId = "";
                    msgBdy.individualCustomer.motherMaidenName = AccountDDE[0]["eqs_mothermaidenname"].ToString();
                    msgBdy.individualCustomer.isStaff = "";
                    msgBdy.individualCustomer.sex = (AccountDDE[0]["eqs_gendercode"].ToString() == "789030000") ? "M" : "F";

                    msgBdy.individualCustomer.homeBranchCode = await this._commonFunc.getBranchCode(AccountDDE[0]["_eqs_sourcebranchid_value"].ToString());


                    Request_Template.createCustomerRequest.msgBdy = msgBdy;

                    string postDataParametr = await EncriptRespons(JsonConvert.SerializeObject(Request_Template));
                    string Lead_details = await this._queryParser.HttpCBSApiCall(Token, HttpMethod.Post, "CBSCreateCustomer", postDataParametr);
                    dynamic responsD = JsonConvert.DeserializeObject(Lead_details);
                    
                    if(responsD.msgHdr != null && responsD.msgHdr.result.ToString() == "ERROR")
                    {
                        customerLeadReturn.Message = responsD.msgHdr.error[0].reason.ToString();
                        customerLeadReturn.ReturnCode = "CRM-ERROR-102";
                    }
                    else
                    {
                        Dictionary<string,string> fieldInput = new Dictionary<string,string>();
                        
                        customerLeadReturn.customerId = responsD.createCustomerRequest.msgBdy.customerId.ToString();
                        fieldInput.Add("eqs_customeridcreated", customerLeadReturn.customerId);
                        postDataParametr = JsonConvert.SerializeObject(fieldInput);

                        var resp1 = await this._queryParser.HttpApiCall($"eqs_accountapplicants({AccountDDE[0]["_eqs_accountapplicantid_value"].ToString()})", HttpMethod.Patch, postDataParametr);

                        var resp2 = await this._queryParser.HttpApiCall($"eqs_ddeindividualcustomers({AccountDDE[0]["eqs_ddeindividualcustomerid"].ToString()})", HttpMethod.Patch, postDataParametr);

                        customerLeadReturn.Message = OutputMSG.Case_Success;
                        customerLeadReturn.ReturnCode = "CRM-SUCCESS";
                    }                    

                }
                else
                {
                    customerLeadReturn.Message = OutputMSG.Resource_n_Found;
                    customerLeadReturn.ReturnCode = "CRM-ERROR-101";
                }
               
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateAccountByLead", ex.Message);
                customerLeadReturn.Message = OutputMSG.Incorrect_Input;
                customerLeadReturn.ReturnCode = "CRM-ERROR-102";
            }

            return customerLeadReturn;

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
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID, this.Bank_Code);
        }

        private async Task<dynamic> getRequestData(dynamic inputData, string APIname)
        {

            dynamic rejusetJson;

            var EncryptedData = inputData.req_root.body.payload;
            string BankCode = inputData.req_root.header.BankCode.ToString();
            this.Bank_Code = BankCode;
            string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString(), BankCode);
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
