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
using System.Threading.Channels;

namespace ManageCase
{
    public class CreateCaseExecution : ICreateCaseExecution
    {

        private ILoggers _logger;
        private IQueryParser _queryParser;
        public string _transactionID { set; get; }
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

       
        Dictionary<string, string> CaseType = new Dictionary<string, string>();
        Dictionary<string, string> _CaseType = new Dictionary<string, string>();
        Dictionary<string, int> Priority = new Dictionary<string, int>();
        Dictionary<int, string> _Priority = new Dictionary<int, string>();
        Dictionary<string, string> StatusCodes = new Dictionary<string, string>();
        
        private ICommonFunction _commonFunc;

        public CreateCaseExecution(ILoggers logger, IQueryParser queryParser, IKeyVaultService keyVaultService, ICommonFunction commonFunction)
        {
                    
            this._logger = logger;
            
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this._commonFunc = commonFunction;
           
            
            this.CaseType.Add("Request", "789030001");
            this.CaseType.Add("Complaint", "789030003");
            this.CaseType.Add("Query", "1");
            this.CaseType.Add("Suggestion", "789030002");

            this._CaseType.Add("789030001", "Request");
            this._CaseType.Add("789030003", "Complaint");
            this._CaseType.Add("1", "Query");
            this._CaseType.Add("789030002", "Suggestion");

            this.Priority.Add("High", 1);
            this.Priority.Add("Normal", 2);
            this.Priority.Add("Low", 3);

            this._Priority.Add(1, "High");
            this._Priority.Add(2, "Normal");
            this._Priority.Add(3, "Low");

            this.StatusCodes.Add("5", "Problem Solved");
            this.StatusCodes.Add("1000", "Information Provided");
            this.StatusCodes.Add("2000", "Merged");
            this.StatusCodes.Add("1", "In Progress");
            this.StatusCodes.Add("2", "On Hold");
            this.StatusCodes.Add("3", "Waiting for Details");
            this.StatusCodes.Add("4", "Researching");
            this.StatusCodes.Add("6", "Cancelled");
            this.StatusCodes.Add("615290000", "Auto Closed");

        }


        public async Task<CaseReturnParam> ValidateCreateCase(dynamic CaseData, string appkey)
        {
           
            CaseReturnParam ldRtPrm = new CaseReturnParam();
            ldRtPrm.TransactionID = _transactionID;
            try
            {
                string channel = CaseData.ChannelType;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "CreateCaseappkey"))
                {
                    if (!string.IsNullOrEmpty(channel) && channel != "")
                    {
                        int ValidationError = 0;

                        if (string.Equals(CaseData.ChannelType.ToString(), "InternetBanking") || string.Equals(CaseData.ChannelType.ToString(), "MobileBanking") || string.Equals(CaseData.ChannelType.ToString(), "IVR"))
                        {
                            if (CaseData.UCIC == null || string.IsNullOrEmpty(CaseData.UCIC.ToString()) || CaseData.UCIC.ToString() == "")
                            {
                                ValidationError = 1;                                
                            }
                            if (CaseData.Classification == null || string.IsNullOrEmpty(CaseData.Classification.ToString()) || CaseData.Classification.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                            if (CaseData.CaseType == null || string.IsNullOrEmpty(CaseData.CaseType.ToString()) || CaseData.CaseType.ToString() == "")
                            {
                                ValidationError = 1;
                            }
                           

                            if (string.Equals(CaseData.CaseType.ToString(), "Request"))
                            {
                                if (CaseData.AccountNumber == null || string.IsNullOrEmpty(CaseData.AccountNumber.ToString()) || CaseData.AccountNumber.ToString() == "")
                                {
                                    ValidationError = 1;
                                }
                                if (CaseData.Category == null || string.IsNullOrEmpty(CaseData.Category.ToString()) || CaseData.Category.ToString() == "")
                                {
                                    ValidationError = 1;
                                }
                                if (CaseData.SubCategory == null || string.IsNullOrEmpty(CaseData.SubCategory.ToString()) || CaseData.SubCategory.ToString() == "")
                                {
                                    ValidationError = 1;
                                }
                            }

                            if (string.Equals(CaseData.CaseType.ToString(), "Query"))
                            {
                                if (CaseData.Category == null || string.IsNullOrEmpty(CaseData.Category.ToString()) || CaseData.Category.ToString() == "")
                                {
                                    ValidationError = 1;
                                }
                                if (CaseData.SubCategory == null || string.IsNullOrEmpty(CaseData.SubCategory.ToString()) || CaseData.SubCategory.ToString() == "")
                                {
                                    ValidationError = 1;
                                }
                            }                            

                        }
                        


                        if (ValidationError == 1)
                        {
                            this._logger.LogInformation("ValidateCreateCase", "Input parameters are incorrect");
                            ldRtPrm.ReturnCode = "CRM-ERROR-102";
                            ldRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                        else
                        {
                            ldRtPrm = await this.CreateCase(CaseData);
                        }                       


                    }
                    else
                    {
                        this._logger.LogInformation("ValidateCreateCase", "Input parameters are incorrect");
                        ldRtPrm.ReturnCode = "CRM-ERROR-102";
                        ldRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidateCreateCase", "Input parameters are incorrect");
                    ldRtPrm.ReturnCode = "CRM-ERROR-102";
                    ldRtPrm.Message = OutputMSG.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCase", ex.Message);
                ldRtPrm.ReturnCode = "CRM-ERROR-101";
                ldRtPrm.Message = OutputMSG.Resource_n_Found;
                return ldRtPrm;
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

        public async Task<CaseStatusRtParam> ValidategetCaseStatus(dynamic CaseData, string appkey)
        {
            CaseStatusRtParam CSRtPrm = new CaseStatusRtParam();
                   

            int ValidationError = 0;
            try
            {
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetCaseStatusappkey"))
                {
                    if (CaseData.CaseID == null || string.IsNullOrEmpty(CaseData.CaseID.ToString()) || CaseData.CaseID.ToString() == "")
                    {
                        ValidationError = 1;
                    }

                    if (ValidationError == 1)
                    {                       
                        CSRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                    else
                    {                       
                        string statusCodeId = await this._commonFunc.getCaseStatus(CaseData.CaseID.ToString());
                        if (statusCodeId == "" || statusCodeId == null)
                        {                          
                            CSRtPrm.Message = OutputMSG.Incorrect_Input;
                        }
                        else
                        {
                            CSRtPrm.CaseStatus = this.StatusCodes[statusCodeId];
                        }
                        
                    }
                }
                else
                {
                    this._logger.LogInformation("ValidategetCaseStatus", "Input parameters are incorrect");
                    CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    CSRtPrm.Message = OutputMSG.Incorrect_Input;
                }


                return CSRtPrm;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        public async Task<CaseReturnParam> CreateCase(dynamic CaseData)
        {
            CaseReturnParam csRtPrm = new CaseReturnParam();
            try
            {               
                LeadMsdProperty case_Property = new LeadMsdProperty();
                CaseProperty csProperty = new CaseProperty();
                Dictionary<string, string> odatab = new Dictionary<string, string>();
                string postDataParametr, postDataParametr1;
                List<JObject> case_details = new List<JObject>();

                case_Property.eqs_casetype = this.CaseType[CaseData.CaseType.ToString()];
                case_Property.title = CaseData.Subject.ToString();
                case_Property.prioritycode = this.Priority[CaseData.Priority.ToString()];
                case_Property.description = CaseData.Description.ToString();

                csProperty.eqs_customerid = CaseData.UCIC.ToString();
                csProperty.channelId = await this._commonFunc.getChannelId(CaseData.ChannelType.ToString());
                csProperty.customerid = await this._commonFunc.getCustomerId(csProperty.eqs_customerid);
                csProperty.Accountid = await this._commonFunc.getAccountId(CaseData.AccountNumber.ToString());
                csProperty.ccs_classification = await this._commonFunc.getclassificationId(CaseData.Classification.ToString());
                csProperty.CategoryId = await this._commonFunc.getCategoryId(CaseData.Category.ToString());
                csProperty.SubCategoryId = await this._commonFunc.getSubCategoryId(CaseData.SubCategory.ToString(), csProperty.CategoryId);

                if (csProperty.SubCategoryId == "" || csProperty.SubCategoryId.Length < 4)
                {
                    this._logger.LogError("CreateCase", "Mandatory fields are required " + case_details.ToString());
                    csRtPrm.ReturnCode = "CRM-ERROR-102";
                    csRtPrm.Message = OutputMSG.Incorrect_Input;
                    return csRtPrm;
                }
                else
                {
                    JObject items_J_Arr = (JObject)CaseData.AdditionalField;
                    int no_Itemes = (items_J_Arr.Count) / 2;
                    var mandatoryFields = await this._commonFunc.getMandatoryFields(csProperty.SubCategoryId);
                    for (int i = 1; i <= no_Itemes; i++)
                    {
                        string keyName = CaseData.AdditionalField["Field" + i];
                        string ValName = CaseData.AdditionalField["value" + i];
                        mandatoryFields.Where(x => (x.InputField == keyName)).Single().CRMValue = ValName;
                    }
                    foreach (var field in mandatoryFields)
                    {
                        if (field.CRMValue != "" || !string.IsNullOrEmpty(field.CRMValue))
                        {
                            odatab.Add(field.CRMField, field.CRMValue);
                        }
                        else
                        {
                            this._logger.LogError("CreateCase", "Mandatory fields are required " + case_details.ToString());
                            csRtPrm.ReturnCode = "CRM-ERROR-102";
                            csRtPrm.Message = OutputMSG.Incorrect_Input;
                            return csRtPrm;
                        }

                    }
                }

                odatab.Add("customerid_contact@odata.bind", $"contacts({csProperty.customerid})");
                if (csProperty.Accountid.Length > 4)
                {
                    odatab.Add("eqs_account@odata.bind", $"eqs_accounts({csProperty.Accountid})");
                }
                if (csProperty.channelId.Length > 4)
                {
                    odatab.Add("eqs_casechannel@odata.bind", $"eqs_casechannels({csProperty.channelId})");
                }
                if (csProperty.ccs_classification.Length > 4)
                {
                    odatab.Add("ccs_classification@odata.bind", $"ccs_classifications({csProperty.ccs_classification})");
                }
                if (csProperty.CategoryId.Length > 4)
                {
                    odatab.Add("ccs_category@odata.bind", $"ccs_categories({csProperty.CategoryId})");
                }
                if (csProperty.SubCategoryId.Length > 4)
                {
                    odatab.Add("ccs_subcategory@odata.bind", $"ccs_subcategories({csProperty.SubCategoryId})");
                }


                postDataParametr = JsonConvert.SerializeObject(case_Property);
                postDataParametr1 = JsonConvert.SerializeObject(odatab);

                postDataParametr = await this._commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

                case_details = await this._queryParser.HttpApiCall("incidents?$select=ticketnumber", HttpMethod.Post, postDataParametr);




                if (case_details.Count > 0)
                {
                    dynamic respons_code = case_details[0];
                    if (respons_code.responsecode == 204)
                    {
                        csRtPrm.CaseID = CommonFunction.GetIdFromPostRespons(respons_code.responsebody.ToString());
                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else if (respons_code.responsecode == 201)
                    {
                        csRtPrm.CaseID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "ticketnumber");
                        csRtPrm.ReturnCode = "CRM-SUCCESS";
                        csRtPrm.Message = OutputMSG.Case_Success;
                    }
                    else
                    {
                        this._logger.LogError("CreateCase", case_details.ToString());
                        csRtPrm.ReturnCode = "CRM-ERROR-101";
                        csRtPrm.Message = OutputMSG.Resource_n_Found;
                    }
                }
                else
                {
                    this._logger.LogError("CreateCase", case_details.ToString());
                    csRtPrm.ReturnCode = "CRM-ERROR-101";
                    csRtPrm.Message = OutputMSG.Resource_n_Found;
                }


                return csRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCase", ex.Message);
                csRtPrm.ReturnCode = "CRM-ERROR-101";
                csRtPrm.Message = OutputMSG.Resource_n_Found;
                return csRtPrm;
            }
        }
                      

        public async Task<CaseListParam> getCaseList(dynamic CaseData, string appkey)
        {
            CaseListParam CSRtPrm = new CaseListParam();
            CSRtPrm.AllCases = new List<CaseDetails>();


            int ValidationError = 0;
            try
            {
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey, "GetCaseListappkey"))
                {
                    if (CaseData.CustomerID == null || string.IsNullOrEmpty(CaseData.CustomerID.ToString()) || CaseData.CustomerID.ToString() == "")
                    {
                        ValidationError = 1;
                    }

                    if (ValidationError == 1)
                    {
                        this._logger.LogInformation("getCaseList", "Input parameters are incorrect");                       
                        CSRtPrm.Message = OutputMSG.Incorrect_Input;
                    }
                    else
                    {
                        string customerid = await this._commonFunc.getCustomerId(CaseData.CustomerID.ToString());
                        string query_url = $"incidents()?$select=ticketnumber,statuscode,description,eqs_casetype,title,prioritycode&$filter=_customerid_value eq '{customerid}'";
                        var caseresponsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                        var CaseList = await this._commonFunc.getDataFromResponce(caseresponsdtails);
                        foreach (var caseDetails in CaseList)
                        {
                            CaseDetails case_details = new CaseDetails();
                            case_details.CaseID = caseDetails["ticketnumber"].ToString();
                            case_details.CaseStatus = this.StatusCodes[caseDetails["statuscode"].ToString()];
                            case_details.Description = caseDetails["description"].ToString();

                            if (!string.IsNullOrEmpty(caseDetails["eqs_casetype"].ToString()))
                            {
                                case_details.Casetype = this._CaseType[caseDetails["eqs_casetype"].ToString()];
                            }
                                

                            case_details.Subject = caseDetails["title"].ToString();

                            if (!string.IsNullOrEmpty(caseDetails["prioritycode"].ToString()))
                            {
                                case_details.Priority = this._Priority[Convert.ToInt32(caseDetails["prioritycode"].ToString())];
                            }
                                

                            CSRtPrm.AllCases.Add(case_details);
                        }
                    }
                }
                else
                {
                    this._logger.LogInformation("getCaseList", "Input parameters are incorrect");
                    CSRtPrm.ReturnCode = "CRM-ERROR-102";
                    CSRtPrm.Message = OutputMSG.Incorrect_Input;
                }


                return CSRtPrm;
            }
            catch (Exception ex)
            {
                this._logger.LogError("CreateCase", ex.Message);
                CSRtPrm.ReturnCode = "CRM-ERROR-101";
                CSRtPrm.Message = OutputMSG.Resource_n_Found;
                return CSRtPrm;
            }

        }
    }
}
