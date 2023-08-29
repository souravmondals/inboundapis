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

    public class UpDgCustLeadExecution : IUpDgCustLeadExecution
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
        public string DDEId, AddressID,FatcaId;
                
        private ICommonFunction _commonFunc;

        public UpDgCustLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<UpdateCustLeadReturn> ValidateCustLeadDetls(dynamic RequestData)
        {
            UpdateCustLeadReturn ldRtPrm = new UpdateCustLeadReturn();
            RequestData = await this.getRequestData(RequestData, "UpdateDigiCustLead");
            try
            {
               
                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "UpdateDigiCustLead"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.ApplicantId.ToString()))
                        {
                            ldRtPrm = await this.createDDE(RequestData);
                        }                        
                        else
                        {
                            this._logger.LogInformation("ValidateCustLeadDetls", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                            return ldRtPrm;
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

        public async Task<UpdateCustLeadReturn> createDDE(dynamic CustLeadData)
        {
            UpdateCustLeadReturn csRtPrm = new UpdateCustLeadReturn();
            try
            {               
                var Applicant_Data = await this._commonFunc.getApplicentData(CustLeadData.ApplicantId.ToString());
                Applicant_Data = Applicant_Data[0];
                string EntityType = await this._commonFunc.getEntityName(Applicant_Data["_eqs_entitytypeid_value"].ToString());
                if (EntityType == "Individual")
                {
                    csRtPrm = await this.createDigiCustLeadIndv(CustLeadData.IndividualDDE, Applicant_Data);
                }
                else
                {
                    csRtPrm = await this.createDigiCustLeadCorp(CustLeadData.corporateDDE, Applicant_Data);
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateFtchDgLdSts", ex.Message);
                throw;
            }
            return csRtPrm;
        }

        public async Task<UpdateCustLeadReturn> createDigiCustLeadIndv(dynamic CustIndvData, dynamic Applicant_Data)
        {
            UpdateCustLeadReturn csRtPrm = new UpdateCustLeadReturn();
            
            Dictionary<string, string> CRMDDEmappingFields = new Dictionary<string, string>();
            Dictionary<string, bool> CRMDDEmappingFields1 = new Dictionary<string, bool>();
            Dictionary<string, int> CRMDDEmappingFields2 = new Dictionary<string, int>();
           
            try
            {
                this.DDEId = await this._commonFunc.getDDEFinalAccountIndvData(Applicant_Data["eqs_accountapplicantid"].ToString());
                string dd, mm, yyyy;
                /*********** General *********/
                CRMDDEmappingFields.Add("eqs_dataentryoperator", Applicant_Data.eqs_name.ToString());
                CRMDDEmappingFields.Add("eqs_entitytypeId@odata.bind", $"eqs_entitytypes({Applicant_Data._eqs_entitytypeid_value.ToString()})");
                CRMDDEmappingFields.Add("eqs_subentitytypeId@odata.bind", $"eqs_subentitytypes({Applicant_Data._eqs_subentity_value.ToString()})");

                CRMDDEmappingFields.Add("eqs_sourcebranchId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustIndvData.General.SourceBranch.ToString())})");
                CRMDDEmappingFields.Add("eqs_custpreferredbranchId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustIndvData.General.CustomerspreferredBranch.ToString())})");
                CRMDDEmappingFields.Add("eqs_lgcode", CustIndvData.General.LGCode.ToString());
                CRMDDEmappingFields.Add("eqs_lccode", CustIndvData.General.LCCode.ToString());
                CRMDDEmappingFields.Add("eqs_residencytypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_residencytypecode", CustIndvData.General.ResidencyType.ToString()));
                CRMDDEmappingFields.Add("eqs_dataentrystage", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final"));
                CRMDDEmappingFields.Add("eqs_relationshiptoprimaryholder@odata.bind", $"eqs_relationships({await this._commonFunc.getRelationshipID(CustIndvData.General.RelationshiptoPrimaryHolder.ToString())})");
                CRMDDEmappingFields.Add("eqs_AccountRelationship@odata.bind", $"eqs_accountrelationshipses({await this._commonFunc.getAccRelationshipID(CustIndvData.General.AccountRelationship.ToString())})");
                CRMDDEmappingFields.Add("eqs_purposeofcreationId@odata.bind", $"eqs_purposeofcreations({await this._commonFunc.getPurposeID(CustIndvData.General.PurposeofCreation.ToString())})");

                CRMDDEmappingFields.Add("eqs_physicalaornumber", CustIndvData.General.PhysicalAOFnumber.ToString());

                CRMDDEmappingFields1.Add("eqs_ismficustomer", Convert.ToBoolean(CustIndvData.General.IsMFICustomer.ToString()));
                CRMDDEmappingFields.Add("eqs_deferralcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_deferralcode", CustIndvData.General.IsDeferral.ToString())); 


                /*********** Prospect Details *********/
                CRMDDEmappingFields.Add("eqs_titleId@odata.bind", $"eqs_titles({Applicant_Data["_eqs_titleid_value"].ToString()})");
                CRMDDEmappingFields.Add("eqs_firstname", Applicant_Data["eqs_firstname"].ToString());
                CRMDDEmappingFields.Add("eqs_middlename", Applicant_Data["eqs_middlename"].ToString());
                CRMDDEmappingFields.Add("eqs_lastname", Applicant_Data["eqs_lastname"].ToString());

                dd = Applicant_Data["eqs_dob"].ToString().Substring(0, 2);
                mm = Applicant_Data["eqs_dob"].ToString().Substring(3, 2);
                yyyy = Applicant_Data["eqs_dob"].ToString().Substring(6, 4);
                CRMDDEmappingFields.Add("eqs_dob", yyyy + "-" + mm + "-" + dd);
                CRMDDEmappingFields.Add("eqs_age", Applicant_Data["eqs_leadage"].ToString());
                CRMDDEmappingFields.Add("eqs_gendercode", Applicant_Data["eqs_gendercode"].ToString());
                CRMDDEmappingFields.Add("eqs_shortname", CustIndvData.ProspectDetails.ShortName.ToString());
                CRMDDEmappingFields.Add("eqs_mobilenumber", Applicant_Data["eqs_mobilenumber"].ToString());
                CRMDDEmappingFields.Add("eqs_emailid", CustIndvData.ProspectDetails.EmailID.ToString());
                CRMDDEmappingFields.Add("eqs_nationalityId@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.ProspectDetails.NationalityID.ToString())})");
                CRMDDEmappingFields.Add("eqs_countryofbirthId@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.ProspectDetails.CountryIDofbirth.ToString())})");
                CRMDDEmappingFields.Add("eqs_fathername", CustIndvData.ProspectDetails.FathersName.ToString());
                CRMDDEmappingFields.Add("eqs_mothermaidenname", CustIndvData.ProspectDetails.MothersMaidenName.ToString());
                CRMDDEmappingFields.Add("eqs_spousename", CustIndvData.ProspectDetails.SpouseName.ToString());
                CRMDDEmappingFields.Add("eqs_countryId@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.ProspectDetails.CountryID.ToString())})");
                CRMDDEmappingFields.Add("eqs_programcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_programcode", CustIndvData.ProspectDetails.Program.ToString()));
                CRMDDEmappingFields.Add("eqs_educationcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_educationcode", CustIndvData.ProspectDetails.Education.ToString()));
                CRMDDEmappingFields.Add("eqs_maritalstatuscode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_maritalstatuscode", CustIndvData.ProspectDetails.MaritalStatus.ToString()));
                CRMDDEmappingFields.Add("eqs_professioncode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_professioncode", CustIndvData.ProspectDetails.Profession.ToString()));
                CRMDDEmappingFields.Add("eqs_annualincomebandcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_annualincomebandcode", CustIndvData.ProspectDetails.AnnualIncomeBand.ToString()));
                CRMDDEmappingFields.Add("eqs_employertypecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_employertypecode", CustIndvData.ProspectDetails.EmployerType.ToString()));
                CRMDDEmappingFields.Add("eqs_empname", CustIndvData.ProspectDetails.EmployerName.ToString());
                CRMDDEmappingFields.Add("eqs_officephone", CustIndvData.ProspectDetails.OfficePhone.ToString());
                CRMDDEmappingFields.Add("eqs_agriculturalincome", CustIndvData.ProspectDetails.EstimatedAgriculturalIncome.ToString());
                CRMDDEmappingFields.Add("eqs_nonagriculturalincome", CustIndvData.ProspectDetails.EstimatedNonAgriculturalIncome.ToString());
                CRMDDEmappingFields.Add("eqs_isstaffcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_isstaffcode", CustIndvData.ProspectDetails.IsStaff.ToString()));
                CRMDDEmappingFields.Add("eqs_equitasstaffcode", CustIndvData.ProspectDetails.EquitasStaffCode.ToString());
                CRMDDEmappingFields.Add("eqs_languagecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_languagecode", CustIndvData.ProspectDetails.Language.ToString()));
                CRMDDEmappingFields1.Add("eqs_ispep",  Convert.ToBoolean(CustIndvData.ProspectDetails.PolitcallyExposedPerson.ToString()));
                CRMDDEmappingFields.Add("eqs_lobcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_lobcode", CustIndvData.ProspectDetails.LOBCode.ToString()));
                CRMDDEmappingFields.Add("eqs_aobocode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_aobocode", CustIndvData.ProspectDetails.AOBusinessOperation.ToString()));
                CRMDDEmappingFields.Add("eqs_communitycode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_communitycode", CustIndvData.ProspectDetails.Category.ToString()));
                CRMDDEmappingFields.Add("eqs_additionalinformationcode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_additionalinformationcode", CustIndvData.ProspectDetails.AdditionalInformation.ToString()));
          

                /*********** Identification Details *********/
                CRMDDEmappingFields.Add("eqs_panform60code", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_panform60code", CustIndvData.IdentificationDetails.Pan.ToString()));
                CRMDDEmappingFields.Add("eqs_pannumber", CustIndvData.IdentificationDetails.PanNumber.ToString());
                CRMDDEmappingFields.Add("eqs_passportnumber", CustIndvData.IdentificationDetails.PassportNumber.ToString());
                CRMDDEmappingFields.Add("eqs_voterid", CustIndvData.IdentificationDetails.VoterID.ToString());
                CRMDDEmappingFields.Add("eqs_drivinglicensenumber", CustIndvData.IdentificationDetails.DrivinglicenseNumber.ToString());
                CRMDDEmappingFields.Add("eqs_aadharreference", CustIndvData.IdentificationDetails.AadharReference.ToString());
                CRMDDEmappingFields.Add("eqs_ckycreferencenumber", CustIndvData.IdentificationDetails.CKYCreferenceNumber.ToString());
                CRMDDEmappingFields.Add("eqs_kycverificationmodecode", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_kycverificationmodecode", CustIndvData.IdentificationDetails.KYCVerificationMode.ToString()));
                
                dd = CustIndvData.IdentificationDetails.VerificationDate.ToString().Substring(0, 2);
                mm = CustIndvData.IdentificationDetails.VerificationDate.ToString().Substring(3, 2);
                yyyy = CustIndvData.IdentificationDetails.VerificationDate.ToString().Substring(6, 4);
                CRMDDEmappingFields.Add("eqs_verificationdate", yyyy + "-" + mm + "-" + dd);


                /*********** FATCA *********/
                CRMDDEmappingFields1.Add("eqs_taxresident", (await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_taxresident", CustIndvData.FATCA.TaxResident.ToString()) == "1") ? true : false);
                CRMDDEmappingFields.Add("eqs_cityofbirth", CustIndvData.FATCA.CityofBirth.ToString());

                /*********** RM Details *********/
                CRMDDEmappingFields.Add("eqs_servicermcode", CustIndvData.RMDetails.ServiceRMCode.ToString());
                CRMDDEmappingFields.Add("eqs_servicermname", CustIndvData.RMDetails.ServiceRMName.ToString());
                CRMDDEmappingFields.Add("eqs_servicermrole", CustIndvData.RMDetails.ServiceRMRole.ToString());
                CRMDDEmappingFields.Add("eqs_businessrmcode", CustIndvData.RMDetails.BusinessRMCode.ToString());
                CRMDDEmappingFields.Add("eqs_businessrmname", CustIndvData.RMDetails.BusinessRMName.ToString());
                CRMDDEmappingFields.Add("eqs_businessrmrole", CustIndvData.RMDetails.BusinessRMRole.ToString());

                CRMDDEmappingFields.Add("eqs_accountapplicantid@odata.bind", $"eqs_accountapplicants({Applicant_Data["eqs_accountapplicantid"].ToString()})");


                string postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                string postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);
                postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields2);
                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_ddeindividualcustomers()?$select=eqs_ddeindividualcustomerid", HttpMethod.Post, postDataParametr);
                    var ddeid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_ddeindividualcustomer");
                    this.DDEId = ddeid;
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_ddeindividualcustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                }

                /*********** Address *********/
                CRMDDEmappingFields = new Dictionary<string, string>();
                CRMDDEmappingFields1 = new Dictionary<string, bool>();

                CRMDDEmappingFields.Add("eqs_name", CustIndvData.Address.Name.ToString());
                CRMDDEmappingFields.Add("eqs_applicantaddressid", CustIndvData.Address.ApplicantAddress.ToString());
                CRMDDEmappingFields.Add("eqs_addresstypecode", await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_addresstypecode", CustIndvData.Address.AddressType.ToString()));
                CRMDDEmappingFields.Add("eqs_addressline1", CustIndvData.Address.AddressLine1.ToString());
                CRMDDEmappingFields.Add("eqs_addressline2", CustIndvData.Address.AddressLine2.ToString());
                CRMDDEmappingFields.Add("eqs_addressline3", CustIndvData.Address.AddressLine3.ToString());
                CRMDDEmappingFields.Add("eqs_addressline4", CustIndvData.Address.AddressLine4.ToString());
                CRMDDEmappingFields.Add("eqs_mobilenumber", CustIndvData.Address.MobileNumber.ToString());
                CRMDDEmappingFields.Add("eqs_faxnumber", CustIndvData.Address.FaxNumber.ToString());
                CRMDDEmappingFields.Add("eqs_overseasmobilenumber", CustIndvData.Address.OverseasMobileNumber.ToString());
                CRMDDEmappingFields.Add("eqs_pincodemaster@odata.bind", $"eqs_pincodes({await this._commonFunc.getPincodeID(CustIndvData.Address.PinCodeMaster.ToString())})");
                CRMDDEmappingFields.Add("eqs_cityid@odata.bind", $"eqs_cities({await this._commonFunc.getCityID(CustIndvData.Address.CityId.ToString())})");
                CRMDDEmappingFields.Add("eqs_district", CustIndvData.Address.District.ToString());
                CRMDDEmappingFields.Add("eqs_stateid@odata.bind", $"eqs_states({await this._commonFunc.getStateID(CustIndvData.Address.StateId.ToString())})");
                CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.Address.CountryId.ToString())})");
                CRMDDEmappingFields.Add("eqs_pincode", CustIndvData.Address.PinCode.ToString());
                CRMDDEmappingFields.Add("eqs_pobox", CustIndvData.Address.POBox.ToString());
                CRMDDEmappingFields.Add("eqs_landmark", CustIndvData.Address.Landmark.ToString());
                CRMDDEmappingFields.Add("eqs_landlinenumber", CustIndvData.Address.LandlineNumber.ToString());
                CRMDDEmappingFields.Add("eqs_alternatemobilenumber", CustIndvData.Address.AlternateMobileNumber.ToString());
                CRMDDEmappingFields1.Add("eqs_localoverseas", (await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_localoverseas", CustIndvData.Address.LocalOverseas.ToString()) == "1") ? true : false);
                CRMDDEmappingFields.Add("eqs_individualdde@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                this.AddressID = await this._commonFunc.getAddressID(this.DDEId);

                if (string.IsNullOrEmpty(this.AddressID))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leadaddresses()?$select=eqs_leadaddressid", HttpMethod.Post, postDataParametr);
                    var addressid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_leadaddressid");
                    this.AddressID = addressid;
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_leadaddresses({this.AddressID})?", HttpMethod.Patch, postDataParametr);
                }



                /*********** FATCA Link *********/
                CRMDDEmappingFields = new Dictionary<string, string>();

                CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustIndvData.FATCA.CountryCode.ToString())})");
                CRMDDEmappingFields.Add("eqs_otheridentificationnumber", CustIndvData.FATCA.OtherIdentificationNumber.ToString());
                CRMDDEmappingFields.Add("eqs_taxidentificationnumber", CustIndvData.FATCA.TaxIdentificationNumber.ToString());
                CRMDDEmappingFields.Add("eqs_addresstype", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_addresstype", CustIndvData.FATCA.AddressType.ToString()));
                CRMDDEmappingFields.Add("eqs_indivapplicantddeid@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);

                this.FatcaId = await this._commonFunc.getFatcaID(this.DDEId);

                if (string.IsNullOrEmpty(this.FatcaId))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_customerfactcaothers()?$select=eqs_customerfactcaotherid", HttpMethod.Post, postDataParametr);
                    var fatcaid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_customerfactcaotherid");
                    this.FatcaId = fatcaid;
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_customerfactcaothers({this.AddressID})?", HttpMethod.Patch, postDataParametr);
                }

                /*********** Document Link *********/
                if (CustIndvData.Documents.Count>0)
                {
                    csRtPrm.Documents = new List<string>();
                    foreach (var docitem in CustIndvData.Documents)
                    {
                        CRMDDEmappingFields = new Dictionary<string, string>();

                        string Document_id = await this._commonFunc.getDocumentId(docitem.DocID.ToString());

                        CRMDDEmappingFields.Add("eqs_doctype@odata.bind", $"eqs_doctypes({await this._commonFunc.getDocTypeId(docitem.DocType.ToString())})");
                        CRMDDEmappingFields.Add("eqs_doccategory@odata.bind", $"eqs_doccategories({await this._commonFunc.getDocCategoryId(docitem.DocCategoryCode.ToString())})");
                        CRMDDEmappingFields.Add("eqs_docsubcategory@odata.bind", $"eqs_docsubcategories({await this._commonFunc.getDocSubCategoryId(docitem.DocSubCategoryCode.ToString())})");

                        CRMDDEmappingFields.Add("eqs_d0comment", docitem.D0Comment.ToString());
                        CRMDDEmappingFields.Add("eqs_rejectreason", docitem.DVUComment.ToString());

                        CRMDDEmappingFields.Add("eqs_docstatuscode", await this._queryParser.getOptionSetTextToValue("eqs_leaddocument", "eqs_docstatuscode", docitem.Status.ToString()));

                        CRMDDEmappingFields.Add("eqs_individualddefinal@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);

                       
                        if (string.IsNullOrEmpty(Document_id))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leaddocuments()?$select=eqs_leaddocumentid", HttpMethod.Post, postDataParametr);
                            Document_id = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_leaddocumentid");
                            csRtPrm.Documents.Add(Document_id);
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_leaddocuments({Document_id})?", HttpMethod.Patch, postDataParametr);
                            csRtPrm.Documents.Add(Document_id);
                        }

                    }
                }


                csRtPrm.IndividualDDEID = this.DDEId;
                csRtPrm.AddressID = this.AddressID;
                csRtPrm.FATCAID = this.FatcaId;
                csRtPrm.Message = OutputMSG.Case_Success;
                csRtPrm.ReturnCode = "CRM-SUCCESS";

            }
            catch (Exception ex)
            {
                this._logger.LogError("createDigiCustLeadIndv", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-102";
                csRtPrm.Message = OutputMSG.Incorrect_Input;
            }
            
            

            return csRtPrm;
        }

        public async Task<UpdateCustLeadReturn> createDigiCustLeadCorp(dynamic CustCorpData, dynamic Applicant_Data)
        {
            UpdateCustLeadReturn csRtPrm = new UpdateCustLeadReturn();
            CustLeadElement custLeadElement = new CustLeadElement();
            Dictionary<string, string> CRMDDEmappingFields = new Dictionary<string, string>();
            Dictionary<string, bool> CRMDDEmappingFields1 = new Dictionary<string, bool>();
            Dictionary<string, int> CRMDDEmappingFields2 = new Dictionary<string, int>();
            Dictionary<string, string> CRMCustomermappingFields = new Dictionary<string, string>();
            try
            {
                this.DDEId = await this._commonFunc.getDDEFinalAccountCorpData(Applicant_Data["eqs_accountapplicantid"].ToString());
                string dd, mm, yyyy;
                /*********** General *********/
                CRMDDEmappingFields.Add("eqs_dataentryoperator", Applicant_Data.eqs_name.ToString());
                CRMDDEmappingFields.Add("eqs_dataentrystage", await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final"));
                CRMDDEmappingFields.Add("eqs_entitytypeId@odata.bind", $"eqs_entitytypes({Applicant_Data._eqs_entitytypeid_value.ToString()})");
                CRMDDEmappingFields.Add("eqs_subentitytypeId@odata.bind", $"eqs_subentitytypes({Applicant_Data._eqs_subentity_value.ToString()})");

                CRMDDEmappingFields.Add("eqs_sourcebranchterritoryId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustCorpData.General.SourceBranch.ToString())})");
                CRMDDEmappingFields.Add("eqs_preferredhomebranchId@odata.bind", $"eqs_branchs({await this._commonFunc.getBranchId(CustCorpData.General.CustomerpreferredHomebranch.ToString())})");
                CRMDDEmappingFields.Add("eqs_lccode", CustCorpData.General.LGCode.ToString());
                CRMDDEmappingFields.Add("eqs_lgcode", CustCorpData.General.LCCode.ToString());
                CRMDDEmappingFields.Add("eqs_aofnumber", CustCorpData.General.PhysicalAOFnumber.ToString());

                CRMDDEmappingFields.Add("eqs_isprimaryholder", Applicant_Data.eqs_isprimaryholder.ToString());
                CRMDDEmappingFields.Add("eqs_deferralcode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_deferralcode", CustCorpData.General.Deferral.ToString()));
                CRMDDEmappingFields1.Add("eqs_isdeferral", (await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_isdeferral", CustCorpData.General.Isdeferral.ToString()) == "1") ? true : false);
                CRMDDEmappingFields.Add("eqs_panform60code", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_panform60code", CustCorpData.General.PAN.ToString()));
                CRMDDEmappingFields.Add("eqs_purposeofcreationId@odata.bind", $"eqs_purposeofcreations({await this._commonFunc.getPurposeID(CustCorpData.General.PurposeofCreation.ToString())})");
                CRMDDEmappingFields1.Add("eqs_ispermaddrandcurraddrsame", Convert.ToBoolean(CustCorpData.General.IsPermAddrAndCurrAddrSame.ToString()));

                /***********About Prospect * ********/
                CRMDDEmappingFields.Add("eqs_titleId@odata.bind", $"eqs_titles({Applicant_Data["_eqs_titleid_value"].ToString()})");
                CRMDDEmappingFields.Add("eqs_companyname1", Applicant_Data["eqs_companynamepart1"].ToString());
                CRMDDEmappingFields.Add("eqs_companyname2", Applicant_Data["eqs_companynamepart2"].ToString());
                CRMDDEmappingFields.Add("eqs_companyname3", Applicant_Data["eqs_companynamepart3"].ToString());
                CRMDDEmappingFields.Add("eqs_shortname", CustCorpData.AboutProspect.ShortName.ToString());

                dd = Applicant_Data["eqs_dateofincorporation"].ToString().Substring(0, 2);
                mm = Applicant_Data["eqs_dateofincorporation"].ToString().Substring(3, 2);
                yyyy = Applicant_Data["eqs_dateofincorporation"].ToString().Substring(6, 4);
                CRMDDEmappingFields.Add("eqs_dateofincorporation", yyyy + "-" + mm + "-" + dd);
                dd = CustCorpData.AboutProspect.UCICCreatedOn.ToString().Substring(0, 2);
                mm = CustCorpData.AboutProspect.UCICCreatedOn.ToString().Substring(3, 2);
                yyyy = CustCorpData.AboutProspect.UCICCreatedOn.ToString().Substring(6, 4);
                CRMDDEmappingFields.Add("eqs_uciccreatedon", yyyy + "-" + mm + "-" + dd);

                CRMDDEmappingFields.Add("eqs_preferredlanguagecode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_preferredlanguagecode", CustCorpData.AboutProspect.PreferredLanguage.ToString()));
                CRMDDEmappingFields.Add("eqs_emailid", CustCorpData.AboutProspect.EmailId.ToString());
                CRMDDEmappingFields.Add("eqs_faxnumber", CustCorpData.AboutProspect.FaxNumber.ToString());                
                CRMDDEmappingFields.Add("eqs_companyturnovervalue", CustCorpData.AboutProspect.CompanyTurnoverValue.ToString());
                CRMDDEmappingFields.Add("eqs_noofbranchesregionaloffices", CustCorpData.AboutProspect.NoofBranchesRegionaOffices.ToString());
                CRMDDEmappingFields.Add("eqs_currentemployeestrength", CustCorpData.AboutProspect.CurrentEmployeeStrength.ToString());
                CRMDDEmappingFields.Add("eqs_averagesalarytoemployee", CustCorpData.AboutProspect.AverageSalarytoEmployee.ToString());
                CRMDDEmappingFields.Add("eqs_minimumsalarytoemployee", CustCorpData.AboutProspect.MinimumSalarytoEmployee.ToString());
                CRMDDEmappingFields1.Add("eqs_ismficustomer", Convert.ToBoolean(CustCorpData.AboutProspect.IsMFIcustomer.ToString()));
                CRMDDEmappingFields.Add("eqs_programcode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_programcode", CustCorpData.AboutProspect.Program.ToString()));
                CRMDDEmappingFields.Add("eqs_npopocode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_npopocode", CustCorpData.AboutProspect.NPOPO.ToString()));
                CRMDDEmappingFields.Add("eqs_companyturnovercode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_companyturnovercode", CustCorpData.AboutProspect.CompanyTurnover.ToString()));

                CRMDDEmappingFields.Add("eqs_businesstypeId@odata.bind", $"eqs_businesstypes({await this._commonFunc.getBusinessTypeId(CustCorpData.AboutProspect.BusinessType.ToString())})");
                CRMDDEmappingFields.Add("eqs_industryId@odata.bind", $"eqs_businessnatures({await this._commonFunc.getIndustryId(CustCorpData.AboutProspect.Industry.ToString())})");

                /*********** Identification Details *********/

                CRMDDEmappingFields.Add("eqs_pocpanform60code", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_pocpanform60code", CustCorpData.IdentificationDetails.Pan.ToString()));
                CRMDDEmappingFields.Add("eqs_pannumber", CustCorpData.IdentificationDetails.PanNumber.ToString());
                CRMDDEmappingFields.Add("eqs_gstnumber", CustCorpData.IdentificationDetails.GSTNumber.ToString());
                CRMDDEmappingFields.Add("eqs_ckycnumber", CustCorpData.IdentificationDetails.CKYCRefenceNumber.ToString());
                CRMDDEmappingFields.Add("eqs_tannumber", CustCorpData.IdentificationDetails.TANNumber.ToString());
                CRMDDEmappingFields.Add("eqs_cstvatnumber", CustCorpData.IdentificationDetails.VATNumber.ToString());
                CRMDDEmappingFields.Add("eqs_cinregisterednumber", CustCorpData.IdentificationDetails.RegisteredNumber.ToString());

                CRMDDEmappingFields.Add("eqs_kycverificationmodecode", await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_kycverificationmodecode", CustCorpData.IdentificationDetails.KYCVerificationMode.ToString()));

                dd = CustCorpData.IdentificationDetails.CKYCUpdatedDate.ToString().Substring(0, 2);
                mm = CustCorpData.IdentificationDetails.CKYCUpdatedDate.ToString().Substring(3, 2);
                yyyy = CustCorpData.IdentificationDetails.CKYCUpdatedDate.ToString().Substring(6, 4);
                CRMDDEmappingFields.Add("eqs_ckycupdateddate", yyyy + "-" + mm + "-" + dd);
                dd = CustCorpData.IdentificationDetails.KYCVerificationDate.ToString().Substring(0, 2);
                mm = CustCorpData.IdentificationDetails.KYCVerificationDate.ToString().Substring(3, 2);
                yyyy = CustCorpData.IdentificationDetails.KYCVerificationDate.ToString().Substring(6, 4);
                CRMDDEmappingFields.Add("eqs_verificationdate", yyyy + "-" + mm + "-" + dd);

                /*********** RM Details *********/
                CRMDDEmappingFields.Add("eqs_servicermcode", CustCorpData.RMDetails.ServiceRMCode.ToString());
                CRMDDEmappingFields.Add("eqs_servicermname", CustCorpData.RMDetails.ServiceRMName.ToString());
                CRMDDEmappingFields.Add("eqs_servicermrole", CustCorpData.RMDetails.ServiceRMRole.ToString());
                CRMDDEmappingFields.Add("eqs_businessrmcode", CustCorpData.RMDetails.BusinessRMCode.ToString());
                CRMDDEmappingFields.Add("eqs_businessrmname", CustCorpData.RMDetails.BusinessRMName.ToString());
                CRMDDEmappingFields.Add("eqs_businessrmrole", CustCorpData.RMDetails.BusinessRMRole.ToString());


                CRMDDEmappingFields.Add("eqs_accountapplicantid@odata.bind", $"eqs_accountapplicants({Applicant_Data["eqs_accountapplicantid"].ToString()})");

                string postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);                
                string postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                if (string.IsNullOrEmpty(this.DDEId))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_ddecorporatecustomers()?$select=eqs_ddecorporatecustomerid", HttpMethod.Post, postDataParametr);
                    var ddeid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_ddecorporatecustomerid");
                    this.DDEId = ddeid;
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_ddecorporatecustomers({this.DDEId})?", HttpMethod.Patch, postDataParametr);
                }

                /*********** Address *********/
                CRMDDEmappingFields = new Dictionary<string, string>();
                CRMDDEmappingFields1 = new Dictionary<string, bool>();

                CRMDDEmappingFields.Add("eqs_name", CustCorpData.Address.Name.ToString());
                CRMDDEmappingFields.Add("eqs_applicantaddressid", CustCorpData.Address.ApplicantAddress.ToString());
                CRMDDEmappingFields.Add("eqs_addresstypecode", await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_addresstypecode", CustCorpData.Address.AddressType.ToString()));
                CRMDDEmappingFields.Add("eqs_addressline1", CustCorpData.Address.AddressLine1.ToString());
                CRMDDEmappingFields.Add("eqs_addressline2", CustCorpData.Address.AddressLine2.ToString());
                CRMDDEmappingFields.Add("eqs_addressline3", CustCorpData.Address.AddressLine3.ToString());
                CRMDDEmappingFields.Add("eqs_addressline4", CustCorpData.Address.AddressLine4.ToString());
                CRMDDEmappingFields.Add("eqs_mobilenumber", CustCorpData.Address.MobileNumber.ToString());
                CRMDDEmappingFields.Add("eqs_faxnumber", CustCorpData.Address.FaxNumber.ToString());
                CRMDDEmappingFields.Add("eqs_overseasmobilenumber", CustCorpData.Address.OverseasMobileNumber.ToString());
                CRMDDEmappingFields.Add("eqs_pincodemaster@odata.bind", $"eqs_pincodes({await this._commonFunc.getPincodeID(CustCorpData.Address.PinCodeMaster.ToString())})");
                CRMDDEmappingFields.Add("eqs_cityid@odata.bind", $"eqs_cities({await this._commonFunc.getCityID(CustCorpData.Address.CityId.ToString())})");
                CRMDDEmappingFields.Add("eqs_district", CustCorpData.Address.District.ToString());
                CRMDDEmappingFields.Add("eqs_stateid@odata.bind", $"eqs_states({await this._commonFunc.getStateID(CustCorpData.Address.StateId.ToString())})");
                CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustCorpData.Address.CountryId.ToString())})");
                CRMDDEmappingFields.Add("eqs_pincode", CustCorpData.Address.PinCode.ToString());
                CRMDDEmappingFields.Add("eqs_pobox", CustCorpData.Address.POBox.ToString());
                CRMDDEmappingFields.Add("eqs_landmark", CustCorpData.Address.Landmark.ToString());
                CRMDDEmappingFields.Add("eqs_landlinenumber", CustCorpData.Address.LandlineNumber.ToString());
                CRMDDEmappingFields.Add("eqs_alternatemobilenumber", CustCorpData.Address.AlternateMobileNumber.ToString());
                CRMDDEmappingFields1.Add("eqs_localoverseas", (await this._queryParser.getOptionSetTextToValue("eqs_leadaddress", "eqs_localoverseas", CustCorpData.Address.LocalOverseas.ToString()) == "1") ? true : false);

                CRMDDEmappingFields.Add("eqs_corporatedde@odata.bind", $"eqs_ddecorporatecustomers({this.DDEId})");

                postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);
                postDataParametr1 = JsonConvert.SerializeObject(CRMDDEmappingFields1);
                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                this.AddressID = await this._commonFunc.getAddressID(this.DDEId);

                if (string.IsNullOrEmpty(this.AddressID))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leadaddresses()?$select=eqs_leadaddressid", HttpMethod.Post, postDataParametr);
                    var addressid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_leadaddressid");
                    this.AddressID = addressid;
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_leadaddresses({this.AddressID})?", HttpMethod.Patch, postDataParametr);
                }



                /*********** FATCA Link *********/
                CRMDDEmappingFields = new Dictionary<string, string>();

                CRMDDEmappingFields.Add("eqs_countryid@odata.bind", $"eqs_countries({await this._commonFunc.getCountryID(CustCorpData.FATCA.CountryCode.ToString())})");
                CRMDDEmappingFields.Add("eqs_otheridentificationnumber", CustCorpData.FATCA.OtherIdentificationNumber.ToString());
                CRMDDEmappingFields.Add("eqs_taxidentificationnumber", CustCorpData.FATCA.TaxIdentificationNumber.ToString());
                CRMDDEmappingFields.Add("eqs_addresstype", await this._queryParser.getOptionSetTextToValue("eqs_customerfactcaother", "eqs_addresstype", CustCorpData.FATCA.AddressType.ToString()));
                CRMDDEmappingFields.Add("eqs_indivapplicantddeid@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);

                this.FatcaId = await this._commonFunc.getFatcaID(this.DDEId);

                if (string.IsNullOrEmpty(this.FatcaId))
                {
                    List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_customerfactcaothers()?$select=eqs_customerfactcaotherid", HttpMethod.Post, postDataParametr);
                    var fatcaid = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_customerfactcaotherid");
                    this.FatcaId = fatcaid;
                }
                else
                {
                    var response = await this._queryParser.HttpApiCall($"eqs_customerfactcaothers({this.AddressID})?", HttpMethod.Patch, postDataParametr);
                }

                /*********** Document Link *********/
                if (CustCorpData.Documents.Count > 0)
                {
                    csRtPrm.Documents = new List<string>();
                    foreach (var docitem in CustCorpData.Documents)
                    {
                        CRMDDEmappingFields = new Dictionary<string, string>();

                        string Document_id = await this._commonFunc.getDocumentId(docitem.DocID.ToString());

                        CRMDDEmappingFields.Add("eqs_doctype@odata.bind", $"eqs_doctypes({await this._commonFunc.getDocTypeId(docitem.DocType.ToString())})");
                        CRMDDEmappingFields.Add("eqs_doccategory@odata.bind", $"eqs_doccategories({await this._commonFunc.getDocCategoryId(docitem.DocCategoryCode.ToString())})");
                        CRMDDEmappingFields.Add("eqs_docsubcategory@odata.bind", $"eqs_docsubcategories({await this._commonFunc.getDocSubCategoryId(docitem.DocSubCategoryCode.ToString())})");

                        CRMDDEmappingFields.Add("eqs_d0comment", docitem.D0Comment.ToString());
                        CRMDDEmappingFields.Add("eqs_rejectreason", docitem.DVUComment.ToString());

                        CRMDDEmappingFields.Add("eqs_docstatuscode", await this._queryParser.getOptionSetTextToValue("eqs_leaddocument", "eqs_docstatuscode", docitem.Status.ToString()));

                        CRMDDEmappingFields.Add("eqs_individualddefinal@odata.bind", $"eqs_ddeindividualcustomers({this.DDEId})");

                        postDataParametr = JsonConvert.SerializeObject(CRMDDEmappingFields);


                        if (string.IsNullOrEmpty(Document_id))
                        {
                            List<JObject> DDE_details = await this._queryParser.HttpApiCall("eqs_leaddocuments()?$select=eqs_leaddocumentid", HttpMethod.Post, postDataParametr);
                            Document_id = CommonFunction.GetIdFromPostRespons201(DDE_details[0]["responsebody"], "eqs_leaddocumentid");
                            csRtPrm.Documents.Add(Document_id);
                        }
                        else
                        {
                            var response = await this._queryParser.HttpApiCall($"eqs_leaddocuments({Document_id})?", HttpMethod.Patch, postDataParametr);
                            csRtPrm.Documents.Add(Document_id);
                        }

                    }
                }


                csRtPrm.IndividualDDEID = this.DDEId;
                csRtPrm.AddressID = this.AddressID;
                csRtPrm.FATCAID = this.FatcaId;
                csRtPrm.Message = OutputMSG.Case_Success;
                csRtPrm.ReturnCode = "CRM-SUCCESS";

            }
            catch (Exception ex)
            {
                this._logger.LogError("createDigiCustLeadIndv", ex.Message);
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
