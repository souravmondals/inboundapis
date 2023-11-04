using Newtonsoft.Json;
using System.Text.Json.Serialization;

namespace DigiCustLead
{

    public class CustLeadElement
    {
        public string eqs_crmproductcategorycode { get; set; }
        public int leadsourcecode { get; set; }
        public string firstname { get; set; }
        public string middlename { get; set; }
        public string lastname { get; set; }
        public string mobilephone { get; set; }
        public string yomifullname
        {
            get
            {
                return firstname + " " + middlename + " " + lastname;
            }

        }

        public string eqs_pincode { get; set; }
        public string  eqs_dob { get; set; }
        public string eqs_internalpan { get; set; }
        public string eqs_voterid { get; set; }
        public string eqs_dlnumber { get; set; }
        public string eqs_passportnumber { get; set; }
        public string eqs_ckycnumber { get; set; }

    }

    public class CustLeadElementCorp
    {
        public string eqs_crmproductcategorycode { get; set; }
        public int leadsourcecode { get; set; }
        public string eqs_companynamepart1 { get; set; }
        public string eqs_companynamepart2 { get; set; }
        public string eqs_companynamepart3 { get; set; }
        public string eqs_contactmobile { get; set; }
        public string eqs_contactperson { get; set; }
        public string eqs_cinnumber { get; set; }
        public string eqs_tannumber { get; set; }
        public string eqs_gstnumber { get; set; }
        public string eqs_cstvatnumber { get; set; }
 
        public string eqs_internalpan { get; set; }
    }

    public class CreateCustLeadReturn
    {
        public string AccountapplicantID { get; set; }
        public string LeadID { get; set; }

        public string ReturnCode { get; set; } 
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class UpdateCustLeadReturn
    {
      
        public string FATCAID { get; set; }
        public List<string> Address { get; set; }
        public string BOID { get; set; }
        public string CPID { get; set; }
        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }

    }

    public class FetchCustLeadReturn
    {
        public Individual individual { get; set; }
        public Corporate corporate { get; set; }

        public string ReturnCode { get; set; }
        public string Message { get; set; }
        public string TransactionID { get; set; }
        public string ExecutionTime { get; set; }
    }

    public class Individual
    {
        public General general { get; set; }
        public Prospect prospect { get; set; }
        public Identification identification { get; set; }
        public Address address { get; set; }
        public RM rm { get; set; }
        public FATCA fatca { get; set; }
        public Document document { get; set; }
    }
    public class Corporate
    {
        public General general { get; set; }
        public Prospect prospect { get; set; }
        public Identification identification { get; set; }
        public Address address { get; set; }
        public RM rm { get; set; }
        public FATCA fatca { get; set; }
        public Document document { get; set; }
        public PointOfContact pointOfContact { get; set; }
        public BO bo { get; set; }
        public CP cp { get; set; }
    }

    public class General
    {
        public string DDEId { get; set; }
        public string ResidencyType { get; set; }
        public string SourceBranch { get; set; }
        public string CustomerspreferredBranch { get; set; }
        public string LGCode { get; set; }
        public string LCCode { get; set; }
        public string RelationshiptoPrimaryHolder { get; set; }
        public string AccountRelationship { get; set; }
        public string PhysicalAOFnumber { get; set; }
        public string IsMFICustomer { get; set; }
        public string IsDeferral { get; set; }
        public string PurposeofCreation { get; set; }
        public string InstaKitCustomerId { get; set; }
        public string Deferral { get; set; }
        public string PAN { get; set; }
        public string IsPrimaddCurraddSame { get; set; }
    }
    public class Prospect
    {
        public string Gender { get; set; }
        public string ShortName { get; set; }
        public string EmailID { get; set; }
        public string Nationality { get; set; }
        public string Countryofbirth { get; set; }
        public string CityofBirth { get; set; }
        public string FathersName { get; set; }
        public string MothersMaidenName { get; set; }
        public string SpouseName { get; set; }
        public string Country { get; set; }
        public string Program { get; set; }
        public string Education { get; set; }
        public string MaritalStatus { get; set; }
        public string Profession { get; set; }
        public string AnnualIncomeBand { get; set; }
        public string EmployerType { get; set; }
        public string EmployerName { get; set; }
        public string OfficePhone { get; set; }
        public string EstimatedAgriculturalIncome { get; set; }
        public string EstimatedNonAgriculturalIncome { get; set; }
        public string IsStaff { get; set; }
        public string EquitasStaffCode { get; set; }
        public string Language { get; set; }
        public string PolitcallyExposedPerson { get; set; }
        public string LOBCode { get; set; }
        public string AOBusinessOperation { get; set; }
        public string PreferredLanguage { get; set; }
        public string FaxNumber { get; set; }       
        public string NPOPO { get; set; }
        public string IsMFIcustomer { get; set; }
        public string UCICCreatedOn { get; set; }
        public string BusinessType { get; set; }
        public string Industry { get; set; }
        public string CompanyTurnover { get; set; }
        public string CompanyTurnoverValue { get; set; }
        public string NoofBranchesRegionaOffices { get; set; }
        public string CurrentEmployeeStrength { get; set; }
        public string AverageSalarytoEmployee { get; set; }
        public string MinimumSalarytoEmployee { get; set; }
    }
    public class Identification
    {
        public string PassportNumber { get; set; }
        public string CKYCreferenceNumber { get; set; }
        public string KYCVerificationMode { get; set; }
        public string VerificationDate { get; set; }
        public string CKYCUpdatedDate { get; set; }
        public string KYCVerificationDate { get; set; }
    }
    public class Address
    {
        public string Name { get; set; }
        public string ApplicantAddress { get; set; }
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string AddressLine3 { get; set; }
        public string AddressLine4 { get; set; }
        public string MobileNumber { get; set; }
        public string FaxNumber { get; set; }
        public string OverseasMobileNumber { get; set; }      
        public string City { get; set; }
        public string District { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string PinCode { get; set; }
        public string POBox { get; set; }
        public string Landmark { get; set; }
        public string LandlineNumber { get; set; }
        public string AlternateMobileNumber { get; set; }
        public string Overseas { get; set; }
      
        public string IndividualDDE { get; set; }
        public string ApplicantFatca { get; set; }
        public string CorporateDDE { get; set; }
        public string Nominee { get; set; }
    }
    public class RM
    {
        public string ServiceRMCode { get; set; }
        public string ServiceRMName { get; set; }
        public string ServiceRMRole { get; set; }
        public string BusinessRMCode { get; set; }
        public string BusinessRMName { get; set; }
        public string BusinessRMRole { get; set; }
    }
    public class FATCA
    {

        public string Country { get; set; }
        public string OtherIdentificationNumber { get; set; }
        public string TaxIdentificationNumber { get; set; }
        public string AddressType { get; set; }
        public string NameofNonFinancialEntity { get; set; }
        public string CustomerDOB { get; set; }
        public string BeneficiaryInterest { get; set; }
        public string TaxIdentificationNumberNF { get; set; }
        public string OtherIdentificationNumberNF { get; set; }
        public string Customer { get; set; }
        public string CustomerName { get; set; }
        public string CustomerUCIC { get; set; }
        public string CountryofTaxResidency { get; set; }
        public string PanofNonFinancialEntity { get; set; }
        public string NFOccupationType { get; set; }
        public string Name { get; set; }
        public string TypeofNonFinancialEntity { get; set; }
        public string ListingType { get; set; }
        public string NameofStockExchange { get; set; }
        public string NameofListingCompany { get; set; }
        public string IndivApplicantDDE { get; set; }
    } 
    public class Document
    {
        public string DocumentType { get; set; }
        public string DocumentCategory { get; set; }
        public string DocumentSubCategory { get; set; }
        public string D0Comment { get; set; }
        public string DVUComment { get; set; }
        public string DocumentStatus { get; set; }
    }
    public class PointOfContact
    {
        public string ContactPersonName { get; set; }
        public string POClookUp { get; set; }
        public string POCEmailId { get; set; }
        public string ContactMobilePhone { get; set; }
        public string POCUCIC { get; set; }
        public string POCPhoneNumber { get; set; }
    }
    public class BO
    {
        public string BOType { get; set; }
        public string BOListingType { get; set; }
        public string BOUCIC { get; set; }
        public string Name { get; set; }
        public string BOListingDetails { get; set; }
        public string Holding { get; set; }
    }
    public class CP
    {
        public string NameofCP { get; set; }
        public string CPUCIC { get; set; }
        public string Holding { get; set; }
    }


}
