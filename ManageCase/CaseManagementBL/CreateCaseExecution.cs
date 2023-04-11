using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Net.Http;
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
        Dictionary<string, int> Priority = new Dictionary<string, int>();
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

            this.Priority.Add("High", 1);
            this.Priority.Add("Normal", 2);
            this.Priority.Add("Low", 3);

        }


        public async Task<CaseReturnParam> ValidateLeade(dynamic CaseData, string appkey)
        {
            CaseReturnParam ldRtPrm = new CaseReturnParam();
            try
            {
                string channel = CaseData.ChannelType;
                if (!string.IsNullOrEmpty(appkey) && appkey != "" && checkappkey(appkey))
                {
                    if (!string.IsNullOrEmpty(channel) && channel != "")
                    {
                        int ValidationError = 0;

                        if (string.Equals(CaseData.ChannelType.ToString(), "InternetBanking") || string.Equals(CaseData.ChannelType.ToString(), "MobileBanking") || string.Equals(CaseData.ChannelType.ToString(), "ESFBWebsite"))
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


        public bool checkappkey(string appkey)
        {
            if (this._keyVaultService.ReadSecret("CreateCaseappkey") == appkey)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<CaseReturnParam> ValidateCaseStatus(dynamic LeadStatus)
        {
            CaseReturnParam ldRtPrm = new CaseReturnParam();
            int ValidationError = 0;
            try
            {

                if (LeadStatus.LeadId == null || string.IsNullOrEmpty(LeadStatus.LeadId.ToString()) || LeadStatus.LeadId.ToString() == "")
                {
                    ValidationError = 1;
                }

                if (LeadStatus.Status == null || string.IsNullOrEmpty(LeadStatus.Status.ToString()) || LeadStatus.Status.ToString() == "")
                {
                    ValidationError = 1;
                }

                if (ValidationError == 1)
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
            odatab.Add("eqs_account@odata.bind", $"eqs_accounts({csProperty.Accountid})");
            odatab.Add("ccs_classification@odata.bind", $"ccs_classifications({csProperty.ccs_classification})");
            odatab.Add("ccs_category@odata.bind", $"ccs_categories({csProperty.CategoryId})");
            odatab.Add("ccs_subcategory@odata.bind", $"ccs_subcategories({csProperty.SubCategoryId})");

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
                    csRtPrm.InfoMessage = Error.Lead_Success;
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


        

        public List<JObject> getLeads()
        {
            
            try
            {
                var output = this._queryParser.HttpApiCall("leads", HttpMethod.Get, "").Result;
                return output;
            }
            catch(Exception ex) 
            {
                throw ex;
            }
            
        }
    }
}
