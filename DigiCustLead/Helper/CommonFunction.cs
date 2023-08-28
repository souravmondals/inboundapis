namespace DigiCustLead
{

    using Microsoft.Extensions.Caching.Memory;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Identity.Client;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json.Linq;
    using System.Net;
    using System.Diagnostics.Metrics;
    using CRMConnect;


    public class CommonFunction : ICommonFunction
    {
        public IQueryParser _queryParser;
        private ILoggers _logger;
        public IMemoryCache _cache;

        public CommonFunction(IMemoryCache cache, ILoggers logger, IQueryParser queryParser)
        {
            this._queryParser = queryParser;
            this._logger = logger;
            this._cache = cache;
        }
        public async Task<string> AcquireNewTokenAsync()
        {
            try
            {
                CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
                //string tenantID = "62c7002c-da9e-4e54-86e7-4c53c52518ad";
                //string ClientID = "6cded931-9ddc-47d4-a31b-c4c135036c3b";
                //string ClientSecrets = "OQC8Q~gx9aRsgmf.Q-v.2nfsrgwsdhF1VadXCc_H";

                string tenantID = "e22e4eaa-623f-4164-abb8-ac89a5a17e13";
                string ClientID = "fa5dddfc-4e66-4522-93aa-9e46e78d2c00";
                string ClientSecrets = "Aym8Q~LzrJIqAGQ9CtkmMWkYU3~-ACdfoDFdubjX";

                var authority = $"https://login.microsoftonline.com/{tenantID}";
                var app =
                    ConfidentialClientApplicationBuilder.Create(ClientID)
                                                        .WithClientSecret(ClientSecrets)
                                                        .WithAuthority(authority)
                                                        .Build();

                var authResult = await app.AcquireTokenForClient(new[] { "https://orgc39e5cd7.crm8.dynamics.com/.default" })
                    .ExecuteAsync(cancellationTokenSource.Token).ConfigureAwait(true);

                string bearerHeaderValue = authResult.AccessToken;
                cancellationTokenSource.Cancel();
                return bearerHeaderValue;
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            
        }

        public static string GetIdFromPostRespons(string PResponseData)
        {
           
           string Respons_Id = PResponseData.Substring(PResponseData.IndexOf('(') + 1, PResponseData.IndexOf(')') - PResponseData.IndexOf('(') - 1);
            return Respons_Id;
        }

        public static string GetIdFromPostRespons201(dynamic PResponseData, string datakey)
        {
            string Respons_Id = PResponseData[datakey];
            return Respons_Id;
        }

        public async Task<string> getIDFromGetResponce(string primaryField ,List<JObject> RsponsData)
        {
            string resourceID = "";
            foreach (JObject item in RsponsData)
            {
                if (Enum.TryParse(item["responsecode"].ToString(), out HttpStatusCode responseStatus) && responseStatus == HttpStatusCode.OK)
                {
                    dynamic responseValue = item["responsebody"];
                    JArray resArray = new JArray();
                    string urlMetaData = string.Empty;
                    if (responseValue?.value != null)
                    {
                        resArray = (JArray)responseValue?.value;
                        urlMetaData = responseValue["@odata.context"];
                    }
                    else if (responseValue is JArray)
                    {
                        resArray = responseValue;

                    }
                    else
                    {
                        resArray.Add(responseValue);
                        urlMetaData = responseValue["@odata.context"];
                    }

                    if (resArray != null && resArray.Any())
                    {
                        //int startIndex = urlMetaData.IndexOf("metadata#", StringComparison.Ordinal) + "metadata#".Length;
                        //int endIndex = urlMetaData.IndexOf("(", StringComparison.Ordinal);
                        //string entityName = urlMetaData.Substring(startIndex, endIndex - startIndex);

                        foreach (var record in resArray)
                        {
                            resourceID = record[primaryField]?.ToString();
                        }

                    }
                }
            }
                return resourceID;
        }

        public async Task<JArray> getDataFromResponce(List<JObject> RsponsData)
        {
            string resourceID = "";
            foreach (JObject item in RsponsData)
            {
                if (Enum.TryParse(item["responsecode"].ToString(), out HttpStatusCode responseStatus) && responseStatus == HttpStatusCode.OK)
                {
                    dynamic responseValue = item["responsebody"];
                    JArray resArray = new JArray();
                    string urlMetaData = string.Empty;
                    if (responseValue?.value != null)
                    {
                        resArray = (JArray)responseValue?.value;
                        urlMetaData = responseValue["@odata.context"];
                    }
                    else if (responseValue is JArray)
                    {
                        resArray = responseValue;

                    }
                    else
                    {
                        resArray.Add(responseValue);
                        urlMetaData = responseValue["@odata.context"];
                    }

                    if (resArray != null && resArray.Any())
                    {

                        return resArray;

                    }
                }
            }
            return new JArray();
        }

        public async Task<string> getIDfromMSDTable(string tablename, string idfield, string filterkey, string filtervalue)
        {
            string Table_Id;
            string TableId;
            if (!this.GetMvalue<string>(tablename + filtervalue, out Table_Id))
            {
                string query_url = $"{tablename}()?$select={idfield}&$filter={filterkey} eq '{filtervalue}'";
                var responsdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                TableId = await this.getIDFromGetResponce(idfield, responsdtails);

                this.SetMvalue<string>(tablename + filtervalue, 1400, TableId);
            }
            else
            {
                TableId = Table_Id;
            }
            return TableId;
        }

        public async Task<Dictionary<string, string>> getProductId(string ProductCode)
        {
            string query_url = $"eqs_products()?$select=eqs_productid,_eqs_businesscategoryid_value,_eqs_productcategory_value,eqs_crmproductcategorycode&$filter=eqs_productcode eq '{ProductCode}'";
            var productdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
            string ProductId = await this.getIDFromGetResponce("eqs_productid", productdtails);
            string businesscategoryid = await this.getIDFromGetResponce("_eqs_businesscategoryid_value", productdtails);
            string productcategory = await this.getIDFromGetResponce("_eqs_productcategory_value", productdtails);
            string crmproductcategorycode = await this.getIDFromGetResponce("eqs_crmproductcategorycode", productdtails);
            Dictionary<string, string> ProductData = new Dictionary<string, string>() {
                { "ProductId", ProductId },
                { "businesscategoryid", businesscategoryid },
                { "productcategory", productcategory },
                { "crmproductcategorycode", crmproductcategorycode },
            };
            return ProductData;
        }

        public async Task<string> getBranchId(string BranchCode)
        {
            return await this.getIDfromMSDTable("eqs_branchs", "eqs_branchid", "eqs_branchidvalue", BranchCode);
        } 
        
        public async Task<string> getTitleId(string Title)
        {
            return await this.getIDfromMSDTable("eqs_titles", "eqs_titleid", "eqs_name", Title);
        }
        public async Task<string> getEntityID(string Entity)
        {
            return await this.getIDfromMSDTable("eqs_entitytypes", "eqs_entitytypeid", "eqs_name", Entity);
        }
        public async Task<string> getEntityName(string EntityId)
        {
            return await this.getIDfromMSDTable("eqs_entitytypes", "eqs_name", "eqs_entitytypeid", EntityId);
        }
        public async Task<string> getSubentitytypeID(string Subentitytype)
        {
            return await this.getIDfromMSDTable("eqs_subentitytypes", "eqs_subentitytypeid", "eqs_key", Subentitytype);
        }

        public async Task<string> getRelationshipID(string relationshipCode)
        {
            return await this.getIDfromMSDTable("eqs_relationships", "eqs_relationshipid", "eqs_relationship", relationshipCode);
        }
        public async Task<string> getAccRelationshipID(string accrelationshipCode)
        {
            return await this.getIDfromMSDTable("eqs_accountrelationshipses", "eqs_accountrelationshipsid", "eqs_key", accrelationshipCode);
        }
        public async Task<string> getCountryID(string CountryCode)
        {
            return await this.getIDfromMSDTable("eqs_countries", "eqs_countryid", "eqs_countrycode", CountryCode);
        }
        public async Task<string> getStateID(string StateCode)
        {
            return await this.getIDfromMSDTable("eqs_states", "eqs_stateid", "eqs_statecode", StateCode);
        }
        public async Task<string> getCityID(string CityCode)
        {
            return await this.getIDfromMSDTable("eqs_cities", "eqs_cityid", "eqs_citycode", CityCode);
        }
        public async Task<string> getPincodeID(string PincodeCode)
        {
            return await this.getIDfromMSDTable("eqs_pincodes", "eqs_pincodeid", "eqs_pincode", PincodeCode);
        }
        public async Task<string> getPurposeID(string Purpose)
        {
            return await this.getIDfromMSDTable("eqs_purposeofcreations", "eqs_purposeofcreationid", "eqs_name", Purpose);
        }


        public async Task<string> getAddressID(string DDEID)
        {
            return await this.getIDfromMSDTable("eqs_leadaddresses", "eqs_leadaddressid", "_eqs_individualdde_value", DDEID);
        }
        public async Task<string> getFatcaID(string DDEID)
        {
            return await this.getIDfromMSDTable("eqs_customerfactcaothers", "eqs_customerfactcaotherid", "_eqs_indivapplicantddeid_value", DDEID);
        }

        public async Task<string> getDDEFinalAccountIndvData(string AccountNumber)
        {
            string finalValue = await this._queryParser.getOptionSetTextToValue("eqs_ddeindividualcustomer", "eqs_dataentrystage", "Final");
            return await this.getIDfromMSDTable("eqs_ddeindividualcustomers", "eqs_ddeindividualcustomerid", $"eqs_dataentrystage eq {finalValue} and _eqs_accountapplicantid_value", AccountNumber);
            
        }

        public async Task<string> getDDEFinalAccountCorpData(string AccountNumber)
        {
            string finalValue = await this._queryParser.getOptionSetTextToValue("eqs_ddecorporatecustomer", "eqs_dataentrystage", "Final");
            return await this.getIDfromMSDTable("eqs_ddecorporatecustomers", "eqs_ddecorporatecustomerid", $"eqs_dataentrystage eq {finalValue} and _eqs_accountapplicantid_value", AccountNumber);
                       
        }

        public async Task<JArray> getApplicentData(string Applicent_id)
        {
            try
            {
                string query_url = $"eqs_accountapplicants()?$filter=eqs_applicantid eq '{Applicent_id}'";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getContactData", ex.Message);
                throw ex;
            }
        }

        public async Task<JArray> getContactData(string contact_id)
        {
            try
            {
                string query_url = $"contacts({contact_id})?$select=createdon,eqs_entityflag,eqs_subentitytypeid,mobilephone,eqs_customerid";
                var Accountdtails = await this._queryParser.HttpApiCall(query_url, HttpMethod.Get, "");
                var Account_dtails = await this.getDataFromResponce(Accountdtails);
                return Account_dtails;
            }
            catch (Exception ex)
            {
                this._logger.LogError("getContactData", ex.Message);
                throw ex;
            }
        }

        public async Task<string> MeargeJsonString(string json1, string json2)
        {
            string first = json1.Remove(json1.Length - 1, 1);
            string second = json2.Substring(1);
            return first + ", " + second;
        }

        public bool GetMvalue<T>(string keyname, out T? Outvalue)
        {
            if (!this._cache.TryGetValue<T>(keyname, out Outvalue))
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public void SetMvalue<T>(string keyname, double timevalid, T inputvalue)
        {
            var cacheEntryOptions = new MemoryCacheEntryOptions()
                .SetAbsoluteExpiration(DateTimeOffset.Now.AddMinutes(timevalid));

            this._cache.Set<T>(keyname, inputvalue, cacheEntryOptions);
        }

    }
}
