namespace DigiLead
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

    public class FtchDgLdStsExecution : IFtchDgLdStsExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;
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

                
        private CommonFunction commonFunc;

        public FtchDgLdStsExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this.commonFunc = new CommonFunction(queryParser);
           
           
        }


        public async Task<FtchDgLdStsReturn> ValidateFtchDgLdSts(dynamic RequestData, string appkey)
        {
            FtchDgLdStsReturn ldRtPrm = new FtchDgLdStsReturn();
            try
            {
                string LeadID = RequestData.LeadID;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "FetchDigiLeadStatusappkey"))
                {
                    if (!string.IsNullOrEmpty(LeadID) && LeadID != "")
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
                    csRtPrm.LeadID = RequestData.LeadID;

                    if (LeadData.firstname.ToString().Length > 1)
                    {
                        csRtPrm.individualDetails = new IndividualDetails();
                        csRtPrm.individualDetails.firstName = LeadData.firstname;
                        csRtPrm.individualDetails.lastName = LeadData.lastname;
                        csRtPrm.individualDetails.middleName = LeadData.middlename;
                        csRtPrm.individualDetails.shortName = LeadData.eqs_shortname;
                        csRtPrm.individualDetails.mobilePhone = LeadData.mobilephone;
                        csRtPrm.individualDetails.dob = LeadData.eqs_dob;
                        csRtPrm.individualDetails.aadhar = LeadData.eqs_aadhaarreference;
                        csRtPrm.individualDetails.PAN = LeadData.eqs_pan;
                        csRtPrm.individualDetails.motherMaidenName = LeadData.eqs_mothermaidenname;
                        csRtPrm.individualDetails.identityType = LeadData.eqs_panform60code;
                        csRtPrm.individualDetails.NLFound = LeadData.eqs_nlmatchcode;
                        csRtPrm.individualDetails.reasonNotApplicable = LeadData.eqs_reasonforna;
                        csRtPrm.individualDetails.voterid = LeadData.eqs_voterid;
                        csRtPrm.individualDetails.drivinglicense = LeadData.eqs_dlnumber;
                        csRtPrm.individualDetails.passport = LeadData.eqs_passportnumber;
                        csRtPrm.individualDetails.ckycnumber = LeadData.eqs_ckycnumber;

                        // csRtPrm.individualDetails.reason = TBC;
                        if (LeadData._eqs_titleid_value != null)
                        {
                            csRtPrm.individualDetails.title = await this.commonFunc.getTitle(LeadData._eqs_titleid_value.ToString());
                        }
                        if (LeadData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.individualDetails.purposeOfCreation = await this.commonFunc.getPurposeOfCreation(Lead_data._eqs_purposeofcreationid_value.ToString());
                        }

                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }

                    if (LeadData.eqs_companynamepart1.ToString().Length > 1)
                    {
                        csRtPrm.individualDetails = null;
                        csRtPrm.corporateDetails = new CorporateDetails();
                        csRtPrm.corporateDetails.companyName = LeadData.eqs_companynamepart1;
                        csRtPrm.corporateDetails.companyName2 = LeadData.eqs_companynamepart2;
                        csRtPrm.corporateDetails.companyName3 = LeadData.eqs_companynamepart3;
                        csRtPrm.corporateDetails.companyPhone = LeadData.mobilephone;
                        csRtPrm.corporateDetails.aadhar = LeadData.eqs_aadhaarreference;
                        csRtPrm.corporateDetails.pocNumber = LeadData.eqs_contactmobile;
                        csRtPrm.corporateDetails.pocName = LeadData.eqs_contactperson;
                        csRtPrm.corporateDetails.cinNumber = LeadData.eqs_cinnumber;
                        csRtPrm.corporateDetails.dateOfIncorporation = LeadData.eqs_dateofregistration;
                        csRtPrm.corporateDetails.pan = LeadData.eqs_pan;
                        csRtPrm.corporateDetails.tanNumber = LeadData.eqs_tannumber;
                        csRtPrm.corporateDetails.NLFound = LeadData.eqs_nlmatchcode;
                        csRtPrm.corporateDetails.identityType = LeadData.eqs_panform60code;
                        csRtPrm.corporateDetails.gstNumber = LeadData.eqs_gstnumber;
                        csRtPrm.corporateDetails.alternateMandatoryCheck = LeadData.eqs_deferalcode;
                        csRtPrm.corporateDetails.cstNumber = LeadData.eqs_cstvatnumber;

                        //csRtPrm.corporateDetails.tinNumber = TBC;
                        //csRtPrm.corporateDetails.reason = TBC;
                        if (LeadData._eqs_purposeofcreationid_value != null)
                        {
                            csRtPrm.corporateDetails.purposeOfCreation = await this.commonFunc.getPurposeOfCreation(Lead_data._eqs_purposeofcreationid_value.ToString());
                        }


                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else
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
                var Lead_dtails = await this.commonFunc.getDataFromResponce(Leaddtails);
                return Lead_dtails;
            }
            catch(Exception ex) {
                this._logger.LogError("getLeadData", ex.Message);
                throw ex; 
            }
        }
        
    }
}
