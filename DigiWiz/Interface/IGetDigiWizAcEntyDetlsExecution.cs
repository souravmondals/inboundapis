namespace DigiWiz
{
    public interface IGetDigiWizAcEntyDetlsExecution
    {
        public string API_Name { set; }
        public string Input_payload { set; }
        public Task<WizAcEntyReturn> getWizAcEntyDetls(string AccountNumber);
        public Task<WizAcEntyReturn> ValidateWizAcEntyDetls(dynamic CaseData, string appkey);
        
       
    }
}
