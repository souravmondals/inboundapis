﻿namespace DigiLead
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
    using Swashbuckle.AspNetCore.SwaggerGen;

    public class FtchDgLdStsExecution : IFtchDgLdStsExecution
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

        Dictionary<int, string> Status_Code = new Dictionary<int, string>();
        Dictionary<string, string> IdentityType = new Dictionary<string, string>();

        private readonly IKeyVaultService _keyVaultService;

                
        private ICommonFunction _commonFunc;

        public FtchDgLdStsExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;

            this.Status_Code.Add(7, "Duplicate Lead");
            this.Status_Code.Add(615290000, "Pushed to LOS");
            this.Status_Code.Add(615290001, "Onboarding");
            this.Status_Code.Add(615290002, "Clousre");
            this.Status_Code.Add(8, "Not Qualified");
            this.Status_Code.Add(3, "Onboarded");
            this.Status_Code.Add(4, "Lead Not Interested");
            this.Status_Code.Add(5, "Lead Not Eligible");
            this.Status_Code.Add(0, "Identification");
            this.Status_Code.Add(1, "Qualification");
            this.Status_Code.Add(6, "Lead Rejected");
            this.Status_Code.Add(2, "Doc Verification");

            this.IdentityType.Add("615290000", "PAN Card");
            this.IdentityType.Add("615290001", "8Form 60 + Form 49 A");
            this.IdentityType.Add("615290002", "Form 60");
            this.IdentityType.Add("615290003", "Minor -NA");
            this.IdentityType.Add("615290004", "Not Applicable");

        }


        public async Task<FtchDgLdStsReturn> ValidateFtchDgLdSts(dynamic RequestData)
        {
            FtchDgLdStsReturn ldRtPrm = new FtchDgLdStsReturn();
            RequestData = await this.getRequestData(RequestData);
            try
            {
                string LeadID = RequestData.LeadID;
                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "FetchDigiLeadStatusappkey"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID) && !string.IsNullOrEmpty(LeadID) && LeadID != "")
                    {                       
                        
                       ldRtPrm = await this.getDigiLeadStatus(RequestData);

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

        

        public async Task<FtchDgLdStsReturn> getDigiLeadStatus(dynamic RequestData)
        {
            FtchDgLdStsReturn csRtPrm = new FtchDgLdStsReturn();
            try
            {
                var Lead_data = await getLeadData(RequestData.LeadID.ToString());
                if (Lead_data.Count > 0)
                {
                    dynamic LeadData = Lead_data[0];
                    string Entity_type = await this._commonFunc.getLeadType(LeadData._eqs_entitytypeid_value.ToString());
                    csRtPrm.LeadID = RequestData.LeadID;
                    
                    csRtPrm.Status = this.Status_Code[Convert.ToInt32(LeadData.statuscode.ToString())];
                    csRtPrm.EntityType = await this._commonFunc.getLeadType(LeadData._eqs_entitytypeid_value.ToString());
                    csRtPrm.SubEntityType = await this._commonFunc.getLeadType(LeadData._eqs_subentitytypeid_value.ToString());

                    if (Entity_type == "Individual")
                    {
                        csRtPrm.individualDetails = new IndividualDetails();
                        csRtPrm.individualDetails.firstName = LeadData.firstname;
                        csRtPrm.individualDetails.lastName = LeadData.lastname;
                        csRtPrm.individualDetails.middleName = LeadData.middlename;
                       //csRtPrm.individualDetails.shortName = LeadData.eqs_shortname;
                        csRtPrm.individualDetails.mobilePhone = LeadData.mobilephone;
                        csRtPrm.individualDetails.dob = LeadData.eqs_dob;
                        csRtPrm.individualDetails.aadhar = LeadData.eqs_aadhaarreference;                     
                        csRtPrm.individualDetails.PAN = LeadData.eqs_internalpan;
                        csRtPrm.individualDetails.motherMaidenName = LeadData.eqs_mothermaidenname;
                        csRtPrm.individualDetails.identityType = this.IdentityType[LeadData.eqs_panform60code.ToString()];
                        csRtPrm.individualDetails.NLFound = LeadData.eqs_nlmatchcode;
                        csRtPrm.individualDetails.reasonNotApplicable = LeadData.eqs_reasonforna;
                        csRtPrm.individualDetails.voterid = LeadData.eqs_voterid;
                        csRtPrm.individualDetails.drivinglicense = LeadData.eqs_dlnumber;
                        csRtPrm.individualDetails.passport = LeadData.eqs_passportnumber;
                        csRtPrm.individualDetails.ckycnumber = LeadData.eqs_ckycnumber;

                     
                        if (LeadData._eqs_titleid_value != null)
                        {
                            csRtPrm.individualDetails.title = await this._commonFunc.getTitle(LeadData._eqs_titleid_value.ToString());
                        }
                        if (LeadData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.individualDetails.purposeOfCreation = await this._commonFunc.getPurposeOfCreation(LeadData._eqs_purposeofcreationid_value.ToString());
                            csRtPrm.individualDetails.OtherPurpose = LeadData.eqs_otherpurpose;
                        }

                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }

                    if (Entity_type == "Corporate")
                    {
                        csRtPrm.individualDetails = null;
                        csRtPrm.corporateDetails = new CorporateDetails();
                        csRtPrm.corporateDetails.companyName = LeadData.eqs_companynamepart1;
                        csRtPrm.corporateDetails.companyName2 = LeadData.eqs_companynamepart2;
                        csRtPrm.corporateDetails.companyName3 = LeadData.eqs_companynamepart3;
                        csRtPrm.corporateDetails.companyPhone = LeadData.mobilephone;
                       
                        csRtPrm.corporateDetails.pocNumber = LeadData.eqs_contactpersonmobile;
                        csRtPrm.corporateDetails.pocName = LeadData.eqs_contactperson;
                        csRtPrm.corporateDetails.cinNumber = LeadData.eqs_cinnumber;
                        csRtPrm.corporateDetails.dateOfIncorporation = LeadData.eqs_dateofincorporation;                
                        csRtPrm.corporateDetails.tanNumber = LeadData.eqs_tannumber;
                        csRtPrm.corporateDetails.NLFound = LeadData.eqs_nlmatchcode;
                        csRtPrm.corporateDetails.identityType = this.IdentityType[LeadData.eqs_panform60code.ToString()];
                        csRtPrm.corporateDetails.gstNumber = LeadData.eqs_gstnumber;
                        csRtPrm.corporateDetails.alternateMandatoryCheck = (LeadData.eqs_deferalcode.ToString()== "615290000") ? "Yes" : "No";
                        csRtPrm.corporateDetails.cstNumber = LeadData.eqs_cstvatnumber;
                        csRtPrm.corporateDetails.ckycnumber = LeadData.eqs_ckycnumber;


                      
                        if (LeadData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.corporateDetails.purposeOfCreation = await this._commonFunc.getPurposeOfCreation(LeadData._eqs_purposeofcreationid_value.ToString());
                            csRtPrm.corporateDetails.OtherPurpose = LeadData.eqs_otherpurpose;
                        }


                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else if(csRtPrm.ReturnCode==null)
                    {
                        this._logger.LogInformation("getDigiLeadStatus", "Input parameters are incorrect");
                        csRtPrm.ReturnCode = "CRM-ERROR-102";
                        csRtPrm.Message = OutputMSG.Incorrect_Input;
                    }


                }
                else
                {
                    this._logger.LogInformation("getDigiLeadStatus", "Input parameters are incorrect");
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
                      
        private async Task<JArray> getLeadData(string LeadID)
        {
            try
            {
                string query_url = $"leads()?$filter=eqs_crmleadid eq '{LeadID}'";
                var Leaddtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Lead_dtails = await this._commonFunc.getDataFromResponce(Leaddtails);
                return Lead_dtails;
            }
            catch(Exception ex) {
                this._logger.LogError("getLeadData", ex.Message);
                throw ex; 
            }
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

                var payload = rejusetJson.FetchDigiLeadStatus;
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
