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

        public CrDgCustLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<CreateCustLeadReturn> ValidateCustLeadDetls(dynamic RequestData)
        {
            CreateCustLeadReturn ldRtPrm = new CreateCustLeadReturn();
            RequestData = await this.getRequestData(RequestData);
            try
            {
               
                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "CreateDigiCustLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID))
                    {
                        int ValidationError = 0;
                        if (string.Equals(RequestData.EntityType.ToString(), "Individual"))
                        {
                            var IndvData = RequestData.IndividualEntry;
                            if (IndvData.Title == null || string.IsNullOrEmpty(IndvData.Title.ToString()) || IndvData.Title.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (IndvData.FirstName == null || string.IsNullOrEmpty(IndvData.FirstName.ToString()) || IndvData.FirstName.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (IndvData.LastName == null || string.IsNullOrEmpty(IndvData.LastName.ToString()) || IndvData.LastName.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (IndvData.PAN == null || string.IsNullOrEmpty(IndvData.PAN.ToString()) || IndvData.PAN.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (RequestData.ProductCode == null || string.IsNullOrEmpty(RequestData.ProductCode.ToString()) || RequestData.ProductCode.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                        }
                        else if (string.Equals(RequestData.EntityType.ToString(), "Corporate"))
                        {
                            var CorpData = RequestData.CorporateEntry;
                            if (CorpData.CompanyName == null || string.IsNullOrEmpty(CorpData.CompanyName.ToString()) || CorpData.CompanyName.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (CorpData.PAN == null || string.IsNullOrEmpty(CorpData.PAN.ToString()) || CorpData.PAN.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (RequestData.ProductCode == null || string.IsNullOrEmpty(RequestData.ProductCode.ToString()) || RequestData.ProductCode.ToString() == "")
                            {
                                ValidationError = 1;
                            }
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
                            
                            ldRtPrm = (string.Equals(RequestData.EntityType.ToString(), "Corporate")) ? await this.createDigiCustLeadCorp(RequestData) : await this.createDigiCustLeadIndv(RequestData);
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

        

        public async Task<CreateCustLeadReturn> createDigiCustLeadIndv(dynamic CustLeadData)
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
                    string EntityID = await this._commonFunc.getEntityID(CustLeadData.EntityType.ToString());
                    string TitleId = await this._commonFunc.getTitleId(CustLeadData.IndividualEntry.Title.ToString());
                    string SubEntityID = await this._commonFunc.getSubentitytypeID(CustLeadData.EntityFlagType.ToString());

                    custLeadElement.leadsourcecode = 15;
                    custLeadElement.firstname = CustLeadData.IndividualEntry.FirstName;
                    custLeadElement.middlename = CustLeadData.IndividualEntry.MiddleName;
                    custLeadElement.lastname = CustLeadData.IndividualEntry.LastName;
                    custLeadElement.mobilephone = CustLeadData.IndividualEntry.MobilePhone;
                    custLeadElement.eqs_dob = CustLeadData.IndividualEntry.Dob;
                    custLeadElement.eqs_internalpan = CustLeadData.IndividualEntry.PAN;

                    CRMLeadmappingFields.Add("eqs_panform60code", "615290000");
                    CRMLeadmappingFields.Add("eqs_pan", "**********");
                    CRMLeadmappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({TitleId})");
                    CRMLeadmappingFields.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                    CRMLeadmappingFields.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                    CRMLeadmappingFields.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");
                    CRMLeadmappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMLeadmappingFields.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({SubEntityID})");
                    CRMLeadmappingFields.Add("eqs_aadhaarreference", CustLeadData.IndividualEntry.AadharReference.ToString());


                    if (CustLeadData.IndividualEntry.Pincode != null && CustLeadData.IndividualEntry.Pincode.ToString() != "")
                        custLeadElement.eqs_pincode = CustLeadData.IndividualEntry.Pincode;

                    if (CustLeadData.IndividualEntry.Voterid != null && CustLeadData.IndividualEntry.Voterid.ToString() != "")
                        custLeadElement.eqs_voterid = CustLeadData.IndividualEntry.Voterid;

                    if (CustLeadData.IndividualEntry.Drivinglicense != null && CustLeadData.IndividualEntry.Drivinglicense.ToString() != "")
                        custLeadElement.eqs_dlnumber = CustLeadData.IndividualEntry.Drivinglicense;

                    if (CustLeadData.IndividualEntry.Passport != null && CustLeadData.IndividualEntry.Passport.ToString() != "")
                        custLeadElement.eqs_passportnumber = CustLeadData.IndividualEntry.Passport;

                    if (CustLeadData.IndividualEntry.CKYCNumber != null && CustLeadData.IndividualEntry.CKYCNumber.ToString() != "")
                        custLeadElement.eqs_ckycnumber = CustLeadData.IndividualEntry.CKYCNumber;

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

                    string purpose = await this._commonFunc.getPurposeID(CustLeadData.PurposeOfCreation.ToString());

                    CRMCustomermappingFields.Add("eqs_titleid@odata.bind", $"eqs_titles({TitleId})");
                    CRMCustomermappingFields.Add("eqs_firstname", custLeadElement.firstname);
                    CRMCustomermappingFields.Add("eqs_middlename", custLeadElement.middlename);
                    CRMCustomermappingFields.Add("eqs_lastname", custLeadElement.lastname);
                    CRMCustomermappingFields.Add("eqs_name", custLeadElement.firstname + " " + custLeadElement.middlename + " " + custLeadElement.lastname);
                    CRMCustomermappingFields.Add("eqs_mobilenumber", custLeadElement.mobilephone);
                    CRMCustomermappingFields.Add("eqs_dob", custLeadElement.eqs_dob);
                    CRMCustomermappingFields.Add("eqs_panform60code", "615290000");
                    CRMCustomermappingFields.Add("eqs_pan", "**********");
                    CRMCustomermappingFields.Add("eqs_internalpan", custLeadElement.eqs_internalpan);
                    CRMCustomermappingFields.Add("eqs_passportnumber", custLeadElement.eqs_passportnumber);
                    CRMCustomermappingFields.Add("eqs_voterid", custLeadElement.eqs_voterid);
                    CRMCustomermappingFields.Add("eqs_dlnumber", custLeadElement.eqs_dlnumber);
                    CRMCustomermappingFields.Add("eqs_ckycnumber", custLeadElement.eqs_ckycnumber);
                    CRMCustomermappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMCustomermappingFields.Add("eqs_subentity@odata.bind", $"eqs_subentitytypes({SubEntityID})");
                    CRMCustomermappingFields.Add("eqs_aadhaarreference", CustLeadData.IndividualEntry.AadharReference.ToString());
                    CRMCustomermappingFields.Add("eqs_purposeofcreationid@odata.bind", $"eqs_purposeofcreations({purpose})");

                    if (Lead_details.Count > 0)
                    {
                        dynamic respons_code = Lead_details[0];
                        if (respons_code.responsecode == 201)
                        {
                            string LeadID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_crmleadid");
                            string Lead_ID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "leadid");
                            CRMCustomermappingFields.Add("eqs_leadid@odata.bind", $"leads({Lead_ID})");
                            postDataParametr = JsonConvert.SerializeObject(CRMCustomermappingFields);
                            List<JObject> Customer_details = await this._queryParser.HttpApiCall("eqs_accountapplicants?$select=eqs_applicantid", HttpMethod.Post, postDataParametr);
                            
                            if (Customer_details.Count > 0)
                            {
                                respons_code = Customer_details[0];
                                if (respons_code.responsecode == 201)
                                {
                                    string applicantID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_applicantid");
                                    csRtPrm.ReturnCode = "CRM-SUCCESS";
                                    csRtPrm.AccountapplicantID = applicantID;
                                    csRtPrm.LeadID = LeadID;
                                }
                            }
                        }
                           
                    }
                    else
                    {
                        this._logger.LogInformation("createDigiCustLeadIndv", "Input parameters are incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = OutputMSG.Incorrect_Input;
                    }

                }
                else
                {
                    this._logger.LogInformation("createDigiCustLeadIndv", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                
            }
            catch(Exception ex)
            {
                this._logger.LogError("createDigiCustLeadIndv", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            
            

            return csRtPrm;
        }

        public async Task<CreateCustLeadReturn> createDigiCustLeadCorp(dynamic CustLeadData)
        {
            CreateCustLeadReturn csRtPrm = new CreateCustLeadReturn();
            CustLeadElementCorp custLeadElement = new CustLeadElementCorp();
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
                    string EntityID = await this._commonFunc.getEntityID(CustLeadData.EntityType.ToString());
                    string SubEntityID = await this._commonFunc.getSubentitytypeID(CustLeadData.EntityFlagType.ToString());
                    custLeadElement.leadsourcecode = 15;
                   // custLeadElement.eqs_companynamepart1 = CustLeadData.CorporateEntry.CompanyName;
                   // custLeadElement.eqs_companynamepart2 = CustLeadData.CorporateEntry.CompanyName2;
                   // custLeadElement.eqs_companynamepart3 = CustLeadData.CorporateEntry.CompanyName3;
                    custLeadElement.eqs_contactmobile = CustLeadData.CorporateEntry.PocNumber;
                    custLeadElement.eqs_contactperson = CustLeadData.CorporateEntry.PocName;

                    custLeadElement.eqs_cinnumber = CustLeadData.CorporateEntry.CinNumber;
                    custLeadElement.eqs_tannumber = CustLeadData.CorporateEntry.TanNumber;
                    custLeadElement.eqs_gstnumber = CustLeadData.CorporateEntry.GstNumber;
                    custLeadElement.eqs_cstvatnumber = CustLeadData.CorporateEntry.CstNumber;
                    custLeadElement.eqs_internalpan = CustLeadData.CorporateEntry.PAN;

                    CRMLeadmappingFields.Add("firstname", CustLeadData.CorporateEntry.CompanyName.ToString());
                    CRMLeadmappingFields.Add("lastname", CustLeadData.CorporateEntry.CompanyName2.ToString());
                   // CRMLeadmappingFields.Add("yomifullname", CustLeadData.eqs_companynamepart1 + " " + CustLeadData.eqs_companynamepart2);
                    CRMLeadmappingFields.Add("eqs_panform60code", "615290000");
                    CRMLeadmappingFields.Add("eqs_pan", "**********");
                    CRMLeadmappingFields.Add("eqs_productid@odata.bind", $"eqs_products({ProductId})");
                    CRMLeadmappingFields.Add("eqs_productcategoryid@odata.bind", $"eqs_productcategories({Productcategoryid})");
                    CRMLeadmappingFields.Add("eqs_businesscategoryid@odata.bind", $"eqs_businesscategories({Businesscategoryid})");
                    CRMLeadmappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMLeadmappingFields.Add("eqs_subentitytypeid@odata.bind", $"eqs_subentitytypes({SubEntityID})");

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

                    string purpose = await this._commonFunc.getPurposeID(CustLeadData.CorporateEntry.PurposeOfCreation.ToString());

                    CRMCustomermappingFields.Add("eqs_entitytypeid@odata.bind", $"eqs_entitytypes({EntityID})");
                    CRMCustomermappingFields.Add("eqs_subentity@odata.bind", $"eqs_subentitytypes({SubEntityID})");
                    CRMCustomermappingFields.Add("eqs_companynamepart1", CustLeadData.eqs_companynamepart1);                    
                    CRMCustomermappingFields.Add("eqs_companynamepart2", CustLeadData.eqs_companynamepart2);                    
                    CRMCustomermappingFields.Add("eqs_companynamepart3", CustLeadData.eqs_companynamepart3);
                    CRMCustomermappingFields.Add("eqs_contactperson", CustLeadData.eqs_contactperson);
                    CRMCustomermappingFields.Add("eqs_contactmobilenumber", CustLeadData.eqs_contactmobile);

                    CRMCustomermappingFields.Add("eqs_cinnumber", CustLeadData.eqs_cinnumber);
                    CRMCustomermappingFields.Add("eqs_tannumber", CustLeadData.eqs_tannumber);
                    CRMCustomermappingFields.Add("eqs_gstnumber", CustLeadData.eqs_gstnumber);
                    CRMCustomermappingFields.Add("eqs_cstvatnumber", CustLeadData.eqs_cstvatnumber);

                    CRMCustomermappingFields.Add("eqs_dateofincorporation", CustLeadData.CorporateEntry.DateOfIncorporation.ToString());
                    CRMCustomermappingFields.Add("eqs_panform60code", "615290000");
                    CRMCustomermappingFields.Add("eqs_pan", "**********");
                    CRMCustomermappingFields.Add("eqs_internalpan", custLeadElement.eqs_internalpan);
                    
                   
                    CRMCustomermappingFields.Add("eqs_purposeofcreationid@odata.bind", $"eqs_purposeofcreations({purpose})");

                    if (Lead_details.Count > 0)
                    {
                        dynamic respons_code = Lead_details[0];
                        if (respons_code.responsecode == 201)
                        {
                            string LeadID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_crmleadid");
                            string Lead_ID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "leadid");
                            CRMCustomermappingFields.Add("eqs_leadid@odata.bind", $"leads({Lead_ID})");
                            postDataParametr = JsonConvert.SerializeObject(CRMCustomermappingFields);
                            List<JObject> Customer_details = await this._queryParser.HttpApiCall("eqs_accountapplicants?$select=eqs_applicantid", HttpMethod.Post, postDataParametr);

                            if (Customer_details.Count > 0)
                            {
                                respons_code = Customer_details[0];
                                if (respons_code.responsecode == 201)
                                {
                                    string applicantID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "eqs_applicantid");
                                    csRtPrm.ReturnCode = "CRM-SUCCESS";
                                    csRtPrm.AccountapplicantID = applicantID;
                                    csRtPrm.LeadID = LeadID;
                                }
                            }
                        }

                    }
                    else
                    {
                        this._logger.LogInformation("createDigiCustLeadIndv", "Input parameters are incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("createDigiCustLeadIndv", "Input parameters are incorrect");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                }

            }
            catch(Exception ex)
            {
                this._logger.LogError("createDigiCustLeadCorp", ex.Message);
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
            
            dynamic rejusetJson;
            
            var EncryptedData = inputData.req_root.body.payload;
            string xmlData = await this._queryParser.PayloadDecryption(EncryptedData.ToString());
            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(xmlData);
            string xpath = "PIDBlock/payload";
            var nodes = xmlDoc.SelectSingleNode(xpath);
            foreach (XmlNode childrenNode in nodes)
            {
                rejusetJson = JsonConvert.DeserializeObject(childrenNode.Value);

                var payload = rejusetJson.CreateDigiCustLead;
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
