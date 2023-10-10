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
    using System.Reflection.Emit;
    using System.Reflection.Metadata;
    using System.Reflection;
    using static Azure.Core.HttpHeader;
    using System.Diagnostics.Metrics;

    public class FhDgCustLeadExecution : IFhDgCustLeadExecution
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
        public string AddressID,FatcaId;
                
        private ICommonFunction _commonFunc;

        public FhDgCustLeadExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
           
        }


        public async Task<FetchCustLeadReturn> ValidateFetchLeadDetls(dynamic RequestData)
        {
            FetchCustLeadReturn ldRtPrm = new FetchCustLeadReturn();
            
            try
            {
                RequestData = await this.getRequestData(RequestData, "FetchDigiCustLead");
               
                if (!string.IsNullOrEmpty(this.appkey) && this.appkey != "" && checkappkey(this.appkey, "FetchDigiCustLeadappkey"))
                {
                    if (!string.IsNullOrEmpty(this.Transaction_ID) && !string.IsNullOrEmpty(this.Channel_ID))
                    {
                        if (!string.IsNullOrEmpty(RequestData.AccountapplicantID.ToString()))
                        {
                            ldRtPrm = await this.getDDEFinal(RequestData.AccountapplicantID.ToString());
                        }                        
                        else
                        {
                            this._logger.LogInformation("ValidateFetchLeadDetls", "Account applicant ID is incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = "Account applicant ID is incorrect";
                            return ldRtPrm;
                        }
                                                                    

                    }
                    else
                    {
                        this._logger.LogInformation("ValidateFetchLeadDetls", "Transaction_ID or  Channel_ID is incorrect.");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = "Transaction_ID or  Channel_ID is incorrect.";
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateFetchLeadDetls", "appkey is incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = "Appkey is incorrect";
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("ValidateFetchLeadDetls", ex.Message);
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

        public async Task<FetchCustLeadReturn> getDDEFinal(string AccountapplicantID)
        {
            FetchCustLeadReturn csRtPrm = new FetchCustLeadReturn();
            try
            {               
                var Applicant_Data = await this._commonFunc.getApplicentData(AccountapplicantID);
                if (Applicant_Data.Count>0)
                {
                    string EntityType = await this._commonFunc.getEntityName(Applicant_Data[0]["_eqs_entitytypeid_value"].ToString());
                    if (EntityType == "Individual")
                    {
                         csRtPrm.individual = await this.getDigiCustLeadIndv(Applicant_Data[0]["eqs_accountapplicantid"].ToString());
                    }
                    else
                    {
                         csRtPrm.corporate = await this.getDigiCustLeadCorp(Applicant_Data[0]["eqs_accountapplicantid"].ToString());
                    }

                    csRtPrm.Message = OutputMSG.Case_Success;
                    csRtPrm.ReturnCode = "CRM-SUCCESS";
                }
                else
                {
                    this._logger.LogInformation("getDDEFinal", "No Applicant data found.");
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = "No Applicant data found.";                    
                }
                
            }
            catch (Exception ex)
            {
                this._logger.LogError("getDDEFinal", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-101";
                csRtPrm.Message = OutputMSG.Resource_n_Found;
            }
            return csRtPrm;
        }


        public async Task<Individual> getDigiCustLeadIndv(string applicentID)
        {
            Individual csRtPrm = new Individual();

            try
            {
                dynamic DDEDetails = await this._commonFunc.getDDEFinalIndvDetail(applicentID);

                string dd, mm, yyyy;
                /*********** General *********/
                General general = new General();
                general.DDEId = DDEDetails[0].eqs_dataentryoperator.ToString();
                general.ResidencyType = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_residencytypecode", DDEDetails[0].eqs_residencytypecode.ToString());
                general.LGCode = DDEDetails[0].eqs_lgcode.ToString();
                general.LCCode = DDEDetails[0].eqs_lccode.ToString();

                general.SourceBranch = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_sourcebranchid_value.ToString());
                general.CustomerspreferredBranch = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_custpreferredbranchid_value.ToString());
                
                general.RelationshiptoPrimaryHolder = await this._commonFunc.getRelationshipText(DDEDetails[0]._eqs_relationshiptoprimaryholder_value.ToString());
                general.AccountRelationship = await this._commonFunc.getAccRelationshipText(DDEDetails[0]._eqs_accountrelationship_value.ToString());

                general.PhysicalAOFnumber = DDEDetails[0].eqs_physicalaornumber.ToString();
                general.IsMFICustomer = DDEDetails[0].eqs_ismficustomer.ToString();
                general.IsDeferral = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_deferralcode", DDEDetails[0].eqs_deferralcode.ToString());
                general.PurposeofCreation = await this._commonFunc.getPurposeText(DDEDetails[0]._eqs_purposeofcreationid_value.ToString());
                general.InstaKitCustomerId = DDEDetails[0].eqs_instakitcustomerid.ToString();
                csRtPrm.general = general;

                /*********** Prospect Details *********/
                Prospect prospect = new Prospect();

                prospect.Gender = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_gendercode", DDEDetails[0].eqs_gendercode.ToString());
                prospect.ShortName = DDEDetails[0].eqs_shortname.ToString();
                prospect.EmailID = DDEDetails[0].eqs_emailid.ToString();
                prospect.Nationality = await this._commonFunc.getCountryText(DDEDetails[0]._eqs_nationalityid_value.ToString());
                prospect.Countryofbirth = await this._commonFunc.getCountryText(DDEDetails[0]._eqs_countryofbirthid_value.ToString());
               
                prospect.FathersName = DDEDetails[0].eqs_fathername.ToString();
                prospect.MothersMaidenName = DDEDetails[0].eqs_mothermaidenname.ToString();
                prospect.SpouseName = DDEDetails[0].eqs_spousename.ToString();

                prospect.Country = await this._commonFunc.getCountryText(DDEDetails[0]._eqs_countryid_value.ToString());
                prospect.CityofBirth = DDEDetails[0].eqs_cityofbirth.ToString();
                prospect.Program = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_programcode", DDEDetails[0].eqs_programcode.ToString());
                prospect.Education = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_educationcode", DDEDetails[0].eqs_educationcode.ToString());
                prospect.MaritalStatus = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_maritalstatuscode", DDEDetails[0].eqs_maritalstatuscode.ToString());
                prospect.Profession = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_professioncode", DDEDetails[0].eqs_professioncode.ToString());
                prospect.AnnualIncomeBand = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_annualincomebandcode", DDEDetails[0].eqs_annualincomebandcode.ToString());
                prospect.EmployerType = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_employertypecode", DDEDetails[0].eqs_employertypecode.ToString());
                prospect.EmployerName = DDEDetails[0].eqs_empname.ToString();
                prospect.OfficePhone = DDEDetails[0].eqs_officephone.ToString();

                prospect.EstimatedAgriculturalIncome = DDEDetails[0].eqs_agriculturalincome.ToString();
                prospect.EstimatedNonAgriculturalIncome = DDEDetails[0].eqs_nonagriculturalincome.ToString();
                prospect.IsStaff = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_isstaffcode", DDEDetails[0].eqs_isstaffcode.ToString());
                prospect.EquitasStaffCode = DDEDetails[0].eqs_equitasstaffcode.ToString();
                prospect.Language = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_languagecode", DDEDetails[0].eqs_languagecode.ToString());
                prospect.PolitcallyExposedPerson = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_ispep", DDEDetails[0].eqs_ispep.ToString());
                prospect.LOBCode = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_lobcode", DDEDetails[0].eqs_lobcode.ToString());
                prospect.AOBusinessOperation = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_aobocode", DDEDetails[0].eqs_aobocode.ToString());

                csRtPrm.prospect = prospect;


                /*********** Identification Details *********/
                Identification identification = new Identification();

                identification.PassportNumber = DDEDetails[0].eqs_pannumber.ToString();
                identification.CKYCreferenceNumber = DDEDetails[0].eqs_ckycreferencenumber.ToString();
                identification.KYCVerificationMode = await this._queryParser.getOptionSetValuToText("eqs_ddeindividualcustomer", "eqs_kycverificationmodecode", DDEDetails[0].eqs_kycverificationmodecode.ToString());
                identification.VerificationDate = DDEDetails[0].eqs_verificationdate.ToString();

                csRtPrm.identification = identification;

                /*********** FATCA *********/
                FATCA fATCAobj = new FATCA();
                dynamic fatcaDetail = await this._commonFunc.getDDEFinalFatcaDetail(DDEDetails[0].eqs_ddeindividualcustomerid.ToString(), "indv");
                if (fatcaDetail.Count > 0)
                {
                    fATCAobj.Country = await this._commonFunc.getCountryText(fatcaDetail[0]._eqs_countryid_value.ToString());
                    fATCAobj.OtherIdentificationNumber = fatcaDetail[0].eqs_otheridentificationnumber.ToString();
                    fATCAobj.TaxIdentificationNumber = fatcaDetail[0].eqs_taxidentificationnumber.ToString();
                    fATCAobj.AddressType = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_addresstype", fatcaDetail[0].eqs_addresstype.ToString());
                    fATCAobj.NameofNonFinancialEntity = fatcaDetail[0].eqs_nameofnonfinancialentity.ToString();
                    fATCAobj.CustomerDOB = fatcaDetail[0].eqs_customerdob.ToString();
                    fATCAobj.BeneficiaryInterest = fatcaDetail[0].eqs_beneficiaryinterest.ToString();
                    fATCAobj.TaxIdentificationNumberNF = fatcaDetail[0].eqs_taxidentificationnumbernf.ToString();
                    fATCAobj.OtherIdentificationNumberNF = fatcaDetail[0].eqs_otheridentificationnumbernf.ToString();
                    fATCAobj.Customer = await this._commonFunc.getCustomerText(fatcaDetail[0]._eqs_customerlookup_value.ToString());
                    fATCAobj.CustomerName = fatcaDetail[0].eqs_customername.ToString();
                    fATCAobj.CustomerUCIC = fatcaDetail[0].eqs_customerucic.ToString();
                    fATCAobj.CountryofTaxResidency = await this._commonFunc.getCountryText(fatcaDetail[0]._eqs_countryoftaxresidencyid_value.ToString());
                    fATCAobj.PanofNonFinancialEntity = fatcaDetail[0].eqs_panofnonfinancialentity.ToString();
                    fATCAobj.NFOccupationType = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_nfoccupationtype", fatcaDetail[0].eqs_nfoccupationtype.ToString());
                    fATCAobj.Name = fatcaDetail[0].eqs_name.ToString();

                    csRtPrm.fatca = fATCAobj;
                }
                    

                /*********** RM Details *********/
                RM rm = new RM();

                rm.ServiceRMCode = DDEDetails[0].eqs_servicermcode.ToString();
                rm.ServiceRMName = DDEDetails[0].eqs_servicermname.ToString();
                rm.ServiceRMRole = DDEDetails[0].eqs_servicermrole.ToString();
                rm.BusinessRMCode = DDEDetails[0].eqs_businessrmcode.ToString();
                rm.BusinessRMName = DDEDetails[0].eqs_businessrmname.ToString();
                rm.BusinessRMRole = DDEDetails[0].eqs_businessrmrole.ToString();

                csRtPrm.rm = rm;
                /*********** Address *********/

                Address address = new Address();
                dynamic addressDetail = await this._commonFunc.getDDEFinalAddressDetail(DDEDetails[0].eqs_ddeindividualcustomerid.ToString(), "indv");

                if (addressDetail.Count > 0)
                {
                    address.Name = addressDetail[0].eqs_name.ToString();
                    address.ApplicantAddress = addressDetail[0].eqs_applicantaddressid.ToString();
                    address.AddressType = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_addresstypecode", addressDetail[0].eqs_addresstypecode.ToString());
                    address.AddressLine1 = addressDetail[0].eqs_addressline1.ToString();
                    address.AddressLine2 = addressDetail[0].eqs_addressline2.ToString();
                    address.AddressLine3 = addressDetail[0].eqs_addressline3.ToString();
                    address.AddressLine4 = addressDetail[0].eqs_addressline4.ToString();
                    address.MobileNumber = addressDetail[0].eqs_mobilenumber.ToString();
                    address.FaxNumber = addressDetail[0].eqs_faxnumber.ToString();
                    address.OverseasMobileNumber = addressDetail[0].eqs_overseasmobilenumber.ToString();

                    address.City = await this._commonFunc.getCityText(addressDetail[0]._eqs_cityid_value.ToString());
                    address.District = addressDetail[0].eqs_district.ToString();
                    address.State = await this._commonFunc.getStateText(addressDetail[0]._eqs_stateid_value.ToString());
                    address.Country = await this._commonFunc.getCountryText(addressDetail[0]._eqs_countryid_value.ToString());

                    address.PinCode = addressDetail[0].eqs_zipcode.ToString();
                    address.POBox = addressDetail[0].eqs_pobox.ToString();
                    address.Landmark = addressDetail[0].eqs_landmark.ToString();
                    address.LandlineNumber = addressDetail[0].eqs_landlinenumber.ToString();
                    address.AlternateMobileNumber = addressDetail[0].eqs_alternatemobilenumber.ToString();
                    address.Overseas = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_localoverseas", addressDetail[0].eqs_localoverseas.ToString());

                    address.IndividualDDE = await this._commonFunc.getIndividualDDEText(addressDetail[0]._eqs_individualdde_value.ToString());
                    address.ApplicantFatca = await this._commonFunc.getFatcaText(addressDetail[0]._eqs_applicantfatca_value.ToString());
                    address.CorporateDDE = await this._commonFunc.getCorporateDDEText(addressDetail[0]._eqs_corporatedde_value.ToString());
                    address.Nominee = await this._commonFunc.getNomineeText(addressDetail[0]._eqs_nomineeid_value.ToString());

                    csRtPrm.address = address;
                }
                    



                /*********** Document *********/
                Document document = new Document();
                dynamic docDetail = await this._commonFunc.getDDEFinalDocumentDetail(DDEDetails[0].eqs_ddeindividualcustomerid.ToString(), "indv");

                if (docDetail.Count > 0)
                {
                    document.DocumentType = await this._commonFunc.getDocTypeText(docDetail[0]._eqs_doctype_value.ToString());
                    document.DocumentCategory = await this._commonFunc.getDocCategoryText(docDetail[0]._eqs_doccategory_value.ToString());
                    document.DocumentSubCategory = await this._commonFunc.getDocSubCategoryText(docDetail[0]._eqs_docsubcategory_value.ToString());

                    document.D0Comment = docDetail[0].eqs_d0comment.ToString();
                    document.DVUComment = docDetail[0].eqs_rejectreason.ToString();
                    document.DocumentStatus = await this._queryParser.getOptionSetValuToText("eqs_leaddocument", "eqs_docstatuscode", docDetail[0].eqs_docstatuscode.ToString());

                    csRtPrm.document = document;
                }
                    



            }
            catch (Exception ex)
            {
                this._logger.LogError("getDigiCustLeadIndv", ex.Message);
                throw;
            }



            return csRtPrm;
        }

        public async Task<Corporate> getDigiCustLeadCorp(string applicentID)
        {
            Corporate csRtPrm = new Corporate();
           
            try
            {               
                dynamic DDEDetails = await this._commonFunc.getDDEFinalCorpDetail(applicentID);
                string dd, mm, yyyy;
                /*********** General *********/
                General general = new General();
                general.DDEId = DDEDetails[0].eqs_dataentryoperator.ToString();
                
                general.LGCode = DDEDetails[0].eqs_lgcode.ToString();
                general.LCCode = DDEDetails[0].eqs_lccode.ToString();

                general.SourceBranch = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_sourcebranchterritoryid_value.ToString());
                general.CustomerspreferredBranch = await this._commonFunc.getBranchText(DDEDetails[0]._eqs_preferredhomebranchid_value.ToString());

                
                general.PhysicalAOFnumber = DDEDetails[0].eqs_aofnumber.ToString();

                general.IsDeferral = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_isdeferral", DDEDetails[0].eqs_isdeferral.ToString());
                general.Deferral = await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_deferralcode", DDEDetails[0].eqs_deferralcode.ToString());
                general.PAN = await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_panform60code", DDEDetails[0].eqs_panform60code.ToString());
                general.IsPrimaddCurraddSame = DDEDetails[0].eqs_ispermaddrandcurraddrsame.ToString();
                general.PurposeofCreation = await this._commonFunc.getPurposeText(DDEDetails[0]._eqs_purposeofcreationid_value.ToString());
                csRtPrm.general = general;

                /***********About Prospect *********/
                Prospect prospect = new Prospect();

                prospect.PreferredLanguage = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_preferredlanguagecode", DDEDetails[0].eqs_preferredlanguagecode.ToString());
                prospect.ShortName = DDEDetails[0].eqs_shortname.ToString();
                prospect.EmailID = DDEDetails[0].eqs_emailid.ToString();
                prospect.FaxNumber = DDEDetails[0].eqs_faxnumber.ToString();
                prospect.Program = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_programcode", DDEDetails[0].eqs_programcode.ToString());
                prospect.NPOPO = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_npopocode", DDEDetails[0].eqs_npopocode.ToString());
                prospect.IsMFIcustomer = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_ismficustomer", DDEDetails[0].eqs_ismficustomer.ToString());
                prospect.UCICCreatedOn = DDEDetails[0].eqs_uciccreatedon.ToString();

                prospect.BusinessType = await this._commonFunc.getBusinessTypeText(DDEDetails[0]._eqs_businesstypeid_value.ToString());
                prospect.Industry = await this._commonFunc.getIndustryText(DDEDetails[0]._eqs_industryid_value.ToString());
                prospect.CompanyTurnover = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_companyturnovercode", DDEDetails[0].eqs_companyturnovercode.ToString());
                prospect.CompanyTurnoverValue = DDEDetails[0].eqs_companyturnovervalue.ToString();

                prospect.NoofBranchesRegionaOffices = DDEDetails[0].eqs_noofbranchesregionaloffices.ToString();
                prospect.CurrentEmployeeStrength = DDEDetails[0].eqs_currentemployeestrength.ToString();
                prospect.AverageSalarytoEmployee = DDEDetails[0].eqs_averagesalarytoemployee.ToString();
                prospect.MinimumSalarytoEmployee = DDEDetails[0].eqs_minimumsalarytoemployee.ToString();


                csRtPrm.prospect = prospect;



                /*********** Identification Details *********/
                Identification identification = new Identification();
               
                identification.CKYCreferenceNumber = DDEDetails[0].eqs_ckycnumber.ToString();
                identification.KYCVerificationMode = await this._queryParser.getOptionSetValuToText("eqs_ddecorporatecustomer", "eqs_kycverificationmodecode", DDEDetails[0].eqs_kycverificationmodecode.ToString());                
                identification.CKYCUpdatedDate = DDEDetails[0].eqs_ckycupdateddate.ToString();
                identification.KYCVerificationDate = DDEDetails[0].eqs_verificationdate.ToString();

                csRtPrm.identification = identification;


                /*********** RM Details *********/

                RM rm = new RM();

                rm.ServiceRMCode = DDEDetails[0].eqs_servicermcode.ToString();
                rm.ServiceRMName = DDEDetails[0].eqs_servicermname.ToString();
                rm.ServiceRMRole = DDEDetails[0].eqs_servicermrole.ToString();
                rm.BusinessRMCode = DDEDetails[0].eqs_businessrmcode.ToString();
                rm.BusinessRMName = DDEDetails[0].eqs_businessrmname.ToString();
                rm.BusinessRMRole = DDEDetails[0].eqs_businessrmrole.ToString();

                csRtPrm.rm = rm;

                /*********** Point Of Contact *********/
                PointOfContact pointOfContact = new PointOfContact();

                pointOfContact.ContactPersonName = DDEDetails[0].eqs_contactpersonname.ToString();
                pointOfContact.POClookUp = await this._commonFunc.getCustomerText(DDEDetails[0]._eqs_poclookupid_value.ToString());
                pointOfContact.POCEmailId = DDEDetails[0].eqs_pocemailid.ToString();
                pointOfContact.ContactMobilePhone = DDEDetails[0].eqs_contactmobilenumber.ToString();
                pointOfContact.POCUCIC = DDEDetails[0].eqs_pocucic.ToString();
                pointOfContact.POCPhoneNumber = DDEDetails[0].eqs_pocphonenumber.ToString();

                csRtPrm.pointOfContact = pointOfContact;

                /*********** FATCA *********/
                FATCA fATCAobj = new FATCA();
                dynamic fatcaDetail = await this._commonFunc.getDDEFinalFatcaDetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString(),"corp");
                if (fatcaDetail.Count > 0)
                {
                    fATCAobj.Country = await this._commonFunc.getCountryText(fatcaDetail[0]._eqs_countryid_value.ToString());
                    fATCAobj.OtherIdentificationNumber = fatcaDetail[0].eqs_otheridentificationnumber.ToString();
                    fATCAobj.TaxIdentificationNumber = fatcaDetail[0].eqs_taxidentificationnumber.ToString();
                    fATCAobj.AddressType = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_addresstype", fatcaDetail[0].eqs_addresstype.ToString());
                    fATCAobj.NameofNonFinancialEntity = fatcaDetail[0].eqs_nameofnonfinancialentity.ToString();
                    fATCAobj.CustomerDOB = fatcaDetail[0].eqs_customerdob.ToString();
                    fATCAobj.BeneficiaryInterest = fatcaDetail[0].eqs_beneficiaryinterest.ToString();
                    fATCAobj.TaxIdentificationNumberNF = fatcaDetail[0].eqs_taxidentificationnumbernf.ToString();
                    fATCAobj.OtherIdentificationNumberNF = fatcaDetail[0].eqs_otheridentificationnumbernf.ToString();
                    fATCAobj.Customer = await this._commonFunc.getCustomerText(fatcaDetail[0]._eqs_customerlookup_value.ToString());
                    fATCAobj.CustomerName = fatcaDetail[0].eqs_customername.ToString();
                    fATCAobj.CustomerUCIC = fatcaDetail[0].eqs_customerucic.ToString();
                    fATCAobj.CountryofTaxResidency = await this._commonFunc.getCountryText(fatcaDetail[0]._eqs_countryoftaxresidencyid_value.ToString());
                    fATCAobj.PanofNonFinancialEntity = fatcaDetail[0].eqs_panofnonfinancialentity.ToString();
                    fATCAobj.NFOccupationType = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_nfoccupationtype", fatcaDetail[0].eqs_nfoccupationtype.ToString());
                    fATCAobj.Name = fatcaDetail[0].eqs_name.ToString();

                    fATCAobj.TypeofNonFinancialEntity = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_typeofnonfinancialentitycode", fatcaDetail[0].eqs_typeofnonfinancialentitycode.ToString());
                    fATCAobj.ListingType = await this._queryParser.getOptionSetValuToText("eqs_customerfactcaother", "eqs_listingtypecode", fatcaDetail[0].eqs_listingtypecode.ToString());
                    fATCAobj.NameofStockExchange = fatcaDetail[0].eqs_nameofstockexchange.ToString();
                    fATCAobj.NameofListingCompany = fatcaDetail[0].eqs_nameoflistingcompany.ToString();
                    fATCAobj.IndivApplicantDDE = await this._commonFunc.getIndividualDDEText(fatcaDetail[0]._eqs_indivapplicantddeid_value.ToString());

                    csRtPrm.fatca = fATCAobj;
                }
                

                /*********** Address *********/

                Address address = new Address();
                dynamic addressDetail = await this._commonFunc.getDDEFinalAddressDetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString(), "corp");

                if (addressDetail.Count > 0)
                {
                    address.Name = addressDetail[0].eqs_name.ToString();
                    address.ApplicantAddress = addressDetail[0].eqs_applicantaddressid.ToString();
                    address.AddressType = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_addresstypecode", addressDetail[0].eqs_addresstypecode.ToString());
                    address.AddressLine1 = addressDetail[0].eqs_addressline1.ToString();
                    address.AddressLine2 = addressDetail[0].eqs_addressline2.ToString();
                    address.AddressLine3 = addressDetail[0].eqs_addressline3.ToString();
                    address.AddressLine4 = addressDetail[0].eqs_addressline4.ToString();
                    address.MobileNumber = addressDetail[0].eqs_mobilenumber.ToString();
                    address.FaxNumber = addressDetail[0].eqs_faxnumber.ToString();
                    address.OverseasMobileNumber = addressDetail[0].eqs_overseasmobilenumber.ToString();

                    address.City = await this._commonFunc.getCityText(addressDetail[0]._eqs_cityid_value.ToString());
                    address.District = addressDetail[0].eqs_district.ToString();
                    address.State = await this._commonFunc.getStateText(addressDetail[0]._eqs_stateid_value.ToString());
                    address.Country = await this._commonFunc.getCountryText(addressDetail[0]._eqs_countryid_value.ToString());

                    address.PinCode = addressDetail[0].eqs_zipcode.ToString();
                    address.POBox = addressDetail[0].eqs_pobox.ToString();
                    address.Landmark = addressDetail[0].eqs_landmark.ToString();
                    address.LandlineNumber = addressDetail[0].eqs_landlinenumber.ToString();
                    address.AlternateMobileNumber = addressDetail[0].eqs_alternatemobilenumber.ToString();
                    address.Overseas = await this._queryParser.getOptionSetValuToText("eqs_leadaddress", "eqs_localoverseas", addressDetail[0].eqs_localoverseas.ToString());

                    address.IndividualDDE = await this._commonFunc.getIndividualDDEText(addressDetail[0]._eqs_individualdde_value.ToString());
                    address.ApplicantFatca = await this._commonFunc.getFatcaText(addressDetail[0]._eqs_applicantfatca_value.ToString());
                    address.CorporateDDE = await this._commonFunc.getCorporateDDEText(addressDetail[0]._eqs_corporatedde_value.ToString());
                    address.Nominee = await this._commonFunc.getNomineeText(addressDetail[0]._eqs_nomineeid_value.ToString());

                    csRtPrm.address = address;
                }
                    


                /*********** Document  *********/

                Document document = new Document();
                dynamic docDetail = await this._commonFunc.getDDEFinalDocumentDetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString(), "corp");

                if (docDetail.Count > 0)
                {
                    document.DocumentType = await this._commonFunc.getDocTypeText(docDetail[0]._eqs_doctype_value.ToString());
                    document.DocumentCategory = await this._commonFunc.getDocCategoryText(docDetail[0]._eqs_doccategory_value.ToString());
                    document.DocumentSubCategory = await this._commonFunc.getDocSubCategoryText(docDetail[0]._eqs_docsubcategory_value.ToString());

                    document.D0Comment = docDetail[0].eqs_d0comment.ToString();
                    document.DVUComment = docDetail[0].eqs_rejectreason.ToString();
                    document.DocumentStatus = await this._queryParser.getOptionSetValuToText("eqs_leaddocument", "eqs_docstatuscode", docDetail[0].eqs_docstatuscode.ToString());

                    csRtPrm.document = document;
                }


                /*********** BO *********/
                CP cp = new CP();
                dynamic cpDetail = await this._commonFunc.getDDEFinalCPDetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString());

                if (docDetail.Count > 0)
                {
                    cp.NameofCP = cpDetail[0].eqs_name.ToString();
                    cp.CPUCIC = cpDetail[0].eqs_cpucic.ToString();
                    cp.Holding = cpDetail[0].eqs_holding.ToString();

                    csRtPrm.cp = cp; 
                }


                /*********** CP *********/

                BO bo = new BO();
                dynamic boDetail = await this._commonFunc.getDDEFinalBODetail(DDEDetails[0].eqs_ddecorporatecustomerid.ToString());

                if (docDetail.Count > 0)
                {                 
                    bo.BOType = await this._queryParser.getOptionSetValuToText("eqs_customerbo", "eqs_botypecode", boDetail[0].eqs_botypecode.ToString());
                    bo.BOListingType = await this._queryParser.getOptionSetValuToText("eqs_customerbo", "eqs_bolistingtypecode", boDetail[0].eqs_bolistingtypecode.ToString());

                    bo.BOUCIC = boDetail[0].eqs_boucic.ToString();
                    bo.Name = boDetail[0].eqs_name.ToString();
                    bo.BOListingDetails = boDetail[0].eqs_bolistingdetails.ToString();
                    bo.Holding = boDetail[0].eqs_holding.ToString();

                    csRtPrm.bo = bo; 
                }




            }
            catch (Exception ex)
            {
                this._logger.LogError("getDigiCustLeadCorp", ex.Message);
                throw;
            }



            return csRtPrm;
        }


        public async Task<string> EncriptRespons(string ResponsData)
        {
            return await _queryParser.PayloadEncryption(ResponsData, Transaction_ID, this.Bank_Code);
        }

       

        private async Task<dynamic> getRequestData(dynamic inputData, string APIname)
        {

            dynamic rejusetJson;
            try
            {
                var EncryptedData = inputData.req_root.body.payload;
                string BankCode = inputData.req_root.header.cde.ToString();
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
            }
            catch (Exception ex)
            {
                this._logger.LogError("getRequestData", ex.Message);
            }

            return "";

        }

    }
}
