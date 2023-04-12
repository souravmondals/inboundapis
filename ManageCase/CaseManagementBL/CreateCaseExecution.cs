using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Xml.Linq;

namespace ManageCase
{
    public class CreateCaseExecution
    {

        public ILogger _logger;
        public IQueryParser _queryParser;
        private readonly IKeyVaultService _keyVaultService;
        Dictionary<string, string> Channel = new Dictionary<string, string>();
        Dictionary<string, string> CaseType = new Dictionary<string, string>();
        Dictionary<string, string> _CaseType = new Dictionary<string, string>();
        Dictionary<string, int> Priority = new Dictionary<string, int>();
        Dictionary<int, string> _Priority = new Dictionary<int, string>();
        Dictionary<string, string> StatusCodes = new Dictionary<string, string>();
        
        private CommonFunction commonFunc;

        public CreateCaseExecution(ILogger logger, IQueryParser queryParser, IKeyVaultService keyVaultService)
        {
                    
            this._logger = logger;
            this._keyVaultService = keyVaultService;
            this._queryParser = queryParser;
            this.commonFunc = new CommonFunction(queryParser);
           
            this.Channel.Add("MobileBanking", "615290001");
            this.Channel.Add("InternetBanking", "615290001");
            this.Channel.Add("IVR", "700610000");

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
                            ldRtPrm.IsError = 1;
                            ldRtPrm.ErrorMessage = Error.Incorrect_Input;
                        }
                        else
                        {
                            ldRtPrm = await this.CreateCase(CaseData);
                        }                       


                    }
                    else
                    {
                        ldRtPrm.IsError = 1;
                        ldRtPrm.ErrorMessage = Error.Incorrect_Input;
                    }
                }
                else
                {
                    ldRtPrm.IsError = 1;
                    ldRtPrm.ErrorMessage = Error.Incorrect_Input;
                }

                return ldRtPrm;
            }
            catch (Exception ex)
            {
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
                        CSRtPrm.IsError = 1;
                        CSRtPrm.ErrorMessage = Error.Incorrect_Input;
                    }
                    else
                    {                       
                        string statusCodeId = await this.commonFunc.getCaseStatus(CaseData.CaseID.ToString());
                        if (statusCodeId == "" || statusCodeId == null)
                        {
                            CSRtPrm.IsError = 1;
                            CSRtPrm.ErrorMessage = Error.Incorrect_Input;
                        }
                        else
                        {
                            CSRtPrm.CaseStatus = this.StatusCodes[statusCodeId];
                        }
                        
                    }
                }
                else
                {
                    CSRtPrm.IsError = 1;
                    CSRtPrm.ErrorMessage = Error.Incorrect_Input;
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
            LeadMsdProperty case_Property = new LeadMsdProperty();
            CaseProperty csProperty = new CaseProperty();
            Dictionary<string,string> odatab= new Dictionary<string,string>();
            string postDataParametr, postDataParametr1;
            List<JObject> case_details = new List<JObject>();

            case_Property.caseorigincode = this.Channel[CaseData.ChannelType.ToString()];
            case_Property.eqs_casetype = this.CaseType[CaseData.CaseType.ToString()];
            case_Property.title = CaseData.Subject.ToString();
            case_Property.prioritycode = this.Priority[CaseData.Priority.ToString()];
            case_Property.description = CaseData.Description.ToString();

            csProperty.eqs_customerid = CaseData.UCIC.ToString();
            csProperty.customerid = await this.commonFunc.getCustomerId(csProperty.eqs_customerid);
            csProperty.Accountid = await this.commonFunc.getAccountId(CaseData.AccountNumber.ToString());
            csProperty.ccs_classification = await this.commonFunc.getclassificationId(CaseData.Classification.ToString());
            csProperty.CategoryId = await this.commonFunc.getCategoryId(CaseData.Category.ToString());
            csProperty.SubCategoryId = await this.commonFunc.getSubCategoryId(CaseData.SubCategory.ToString());

            odatab.Add("customerid_contact@odata.bind", $"contacts({csProperty.customerid})");
            if (csProperty.Accountid.Length>4)
            {
                odatab.Add("eqs_account@odata.bind", $"eqs_accounts({csProperty.Accountid})");
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

            postDataParametr = await this.commonFunc.MeargeJsonString(postDataParametr, postDataParametr1);

            case_details = await this._queryParser.HttpApiCall("incidents?$select=ticketnumber", HttpMethod.Post, postDataParametr);


            

            if (case_details.Count >0 )
            {
                dynamic respons_code = case_details[0];
                if (respons_code.responsecode == 204)
                {
                    csRtPrm.CaseID = CommonFunction.GetIdFromPostRespons(respons_code.responsebody.ToString());
                    csRtPrm.InfoMessage = Error.Case_Success;
                }
                else if (respons_code.responsecode == 201)
                {
                    csRtPrm.CaseID = CommonFunction.GetIdFromPostRespons201(respons_code.responsebody, "ticketnumber");
                    csRtPrm.InfoMessage = Error.Case_Success;
                }
                else
                {
                    csRtPrm.IsError = 1;
                    csRtPrm.ErrorMessage = Error.Resource_n_Found;
                }
            }
            else
            {
                csRtPrm.IsError = 1;
                csRtPrm.ErrorMessage = Error.Resource_n_Found;
            }


            return csRtPrm;
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
                        CSRtPrm.IsError = 1;
                        CSRtPrm.ErrorMessage = Error.Incorrect_Input;
                    }
                    else
                    {
                        string customerid = await this.commonFunc.getCustomerId(CaseData.CustomerID.ToString());
                        string query_url = $"incidents()?$select=ticketnumber,statuscode,description,eqs_casetype,title,prioritycode&$filter=_customerid_value eq '{customerid}'";
                        var caseresponsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                        var CaseList = await this.commonFunc.getDataFromResponce(caseresponsdtails);
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
                    CSRtPrm.IsError = 1;
                    CSRtPrm.ErrorMessage = Error.Incorrect_Input;
                }


                return CSRtPrm;
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
    }
}
