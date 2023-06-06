namespace DigiCustLead
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

    public class CrDgCustLeadExecution : ICrDgCustLeadExecution
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

        public CrDgCustLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<CreateCustLeadReturn> ValidateCustLeadDetls(dynamic RequestData, string appkey)
        {
            CreateCustLeadReturn ldRtPrm = new CreateCustLeadReturn();
           // RequestData = await this.getRequestData(RequestData);
            try
            {
               
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateDigiCustLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(Transaction_ID) && !string.IsNullOrEmpty(Channel_ID))
                    {
                        int ValidationError = 0;
                        if (string.Equals(RequestData.EntityType.ToString(), "Individual") && string.Equals(RequestData.EntityFlagType.ToString(), "I"))
                        {
                            if (RequestData.Title == null || string.IsNullOrEmpty(RequestData.Title.ToString()) || RequestData.Title.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (RequestData.FirstName == null || string.IsNullOrEmpty(RequestData.FirstName.ToString()) || RequestData.FirstName.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (RequestData.LastName == null || string.IsNullOrEmpty(RequestData.LastName.ToString()) || RequestData.LastName.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (RequestData.PAN == null || string.IsNullOrEmpty(RequestData.PAN.ToString()) || RequestData.PAN.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (RequestData.ProductCode == null || string.IsNullOrEmpty(RequestData.ProductCode.ToString()) || RequestData.ProductCode.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                        }
                        else if (string.Equals(RequestData.EntityType.ToString(), "Corporate") && string.Equals(RequestData.EntityFlagType.ToString(), "C"))
                        {

                        }
                        else
                        {
                            this._logger.LogInformation("ValidateCustLeadDetls", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                            return ldRtPrm;
                        }

                        
                        if (ValidationError == 1)
                        {
                            this._logger.LogInformation("ValidateCustLeadDetls", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                        else
                        {
                            ldRtPrm = await this.createDigiCustLead(RequestData);
                        }

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

        

        public async Task<CreateCustLeadReturn> createDigiCustLead(dynamic CustLeadData)
        {
            CreateCustLeadReturn csRtPrm = new CreateCustLeadReturn();
            CustLeadElement custLeadElement = new CustLeadElement();
            Dictionary<string, string> CRMLeadmappingFields = new Dictionary<string, string>();
            Dictionary<string, string> CRMCustomermappingFields = new Dictionary<string, string>();
            try
            {
                var productDetails = await this._commonFunc.getProductId(CustLeadData.ProductCode.ToString());
                string ProductId = productDetails["ProductId"];
                string Businesscategoryid = productDetails["businesscategoryid"];
                string Productcategoryid = productDetails["productcategory"];
                custLeadElement.eqs_crmproductcategorycode = productDetails["crmproductcategorycode"];

                if (ProductId != "")
                {
                    string TitleId = await this._commonFunc.getTitleId(CustLeadData.Title.ToString());
                    custLeadElement.leadsourcecode = 15;
                    custLeadElement.firstname = CustLeadData.FirstName;
                    custLeadElement.middlename = CustLeadData.MiddleName;
                    custLeadElement.lastname = CustLeadData.LastName;
                    custLeadElement.mobilephone = CustLeadData.MobilePhone;
                    custLeadElement.eqs_dob = CustLeadData.Dob;
                    custLeadElement.eqs_internalpan = CustLeadData.PAN;

                    CRMLeadmappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({ProductId})");
                    CRMLeadmappingFields.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                    CRMLeadmappingFields.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                    CRMLeadmappingFields.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");

                    if (CustLeadData.Pincode != null && CustLeadData.Pincode.ToString() != "")
                        custLeadElement.eqs_pincode = CustLeadData.Pincode;

                    if (CustLeadData.Voterid != null && CustLeadData.Voterid.ToString() != "")
                        custLeadElement.eqs_voterid = CustLeadData.Voterid;

                    if (CustLeadData.Drivinglicense != null && CustLeadData.Drivinglicense.ToString() != "")
                        custLeadElement.eqs_dlnumber = CustLeadData.Drivinglicense;

                    if (CustLeadData.Passport != null && CustLeadData.Passport.ToString() != "")
                        custLeadElement.eqs_passportnumber = CustLeadData.Passport;

                    if (CustLeadData.CKYCNumber != null && CustLeadData.CKYCNumber.ToString() != "")
                        custLeadElement.eqs_ckycnumber = CustLeadData.CKYCNumber;

                    string BranchId = await this._commonFunc.getBranchId(CustLeadData.BranchCode.ToString());
                    if (BranchId != null && BranchId != "")
                    {
                        CRMLeadmappingFields.Add("eqs_branchid@odata.bind", $"eqs_branchs({BranchId})");
                        CRMCustomermappingFields.Add("eqs_branchid@odata.bind", $"eqs_branchs({BranchId})");
                    }

                    string postDataParametr = JsonConvert.SerializeObject(custLeadElement);
                    string postDataParametr1 = JsonConvert.SerializeObject(CRMLeadmappingFields);

                    postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                    List<JObject> Lead_details = await this._queryParser.HttpApiCall("leads?$select=eqs_crmleadid", HttpMethod.Post, postDataParametr);

                    CRMCustomermappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({ProductId})");
                    CRMCustomermappingFields.Add("firstname", custLeadElement.firstname);
                    CRMCustomermappingFields.Add("middlename", custLeadElement.middlename);
                    CRMCustomermappingFields.Add("lastname", custLeadElement.lastname);
                    CRMCustomermappingFields.Add("mobilephone", custLeadElement.mobilephone);
                    CRMCustomermappingFields.Add("birthdate", custLeadElement.eqs_dob);
                    CRMCustomermappingFields.Add("eqs_pan", custLeadElement.eqs_internalpan);
                    CRMCustomermappingFields.Add("eqs_customerleadid", Lead_details[0]["eqs_crmleadid"].ToString());

                    postDataParametr = JsonConvert.SerializeObject(CRMCustomermappingFields);

                    List<JObject> Customer_details = await this._queryParser.HttpApiCall("contacts", HttpMethod.Post, postDataParametr);

                }
                else
                {
                    this._logger.LogInformation("ValidateLeade", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                
            }
            catch(Exception ex)
            {
                this._logger.LogError("getDigiLeadStatus", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            
            

            return csRtPrm;
        }


        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID);
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

        private async Task<dynamic> getRequestData(dynamic inputData)
        {
            var EncryptedData = inputData.req_root.body.payload;
            string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);
            string xpath = "PIDBlock/payload";
            var nodes = xmlDoc.SelectSingleNode(xpath);
            foreach (XmlNode childrenNode in nodes)
            {
                dynamic rejusetJson = JsonConvert.DeserializeObject(childrenNode.Value);
                return rejusetJson;
            }

            return "";
        }

    }
}
