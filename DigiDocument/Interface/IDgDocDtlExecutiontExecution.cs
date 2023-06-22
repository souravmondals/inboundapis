namespace DigiDocument
{
    public interface IDgDocDtlExecutiontExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public string Channel_ID { set; get; }
        public string Transaction_ID { set; get; }
        public string appkey { set; get; }
        public Task<DgDocDtlReturn> getUcicToProduct(string customerID, string CategoryCode);
        public Task<DgDocDtlReturn> getApplicentToProduct(string leadId, string CategoryCode);
        public Task<DgDocDtlReturn> ValidateProductInput(dynamic CaseData);
        public Task<string> EncriptRespons(string ResponsData);
        public Task CRMLog(string InputRequest, string OutputRespons, string CallStatus);


    }
}
