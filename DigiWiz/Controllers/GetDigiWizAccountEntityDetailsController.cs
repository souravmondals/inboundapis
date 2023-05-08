namespace DigiWiz.Controllers
{

    using Microsoft.AspNetCore.Http;
    using Microsoft.AspNetCore.Mvc;
    using System.Net.Http;
    using Newtonsoft.Json;
    using System.Text;
    using System.Net.Http.Headers;
    using Newtonsoft.Json.Linq;
    using System.Reflection.PortableExecutable;
    using System.Linq;
    using Microsoft.VisualBasic;
    using Microsoft.Extensions.Caching.Memory;
    using CRMConnect;



    [Route("[controller]")]
    [ApiController]
    public class GetDigiWizAccountEntityDetailsController : ControllerBase
    {

        private readonly IGetDigiWizAcEntyDetlsExecution _getDigiWizAcEntyDetlsExecution;
        
        public GetDigiWizAccountEntityDetailsController(IGetDigiWizAcEntyDetlsExecution getDigiWizAcEntyDetlsExecution)
        {
            this._getDigiWizAcEntyDetlsExecution = getDigiWizAcEntyDetlsExecution;
        }



        [HttpPost]
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());

                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("appkey", out var headerValues))
                {
                    Header_Value = headerValues;
                }
                _getDigiWizAcEntyDetlsExecution.API_Name = "GetDigiWizAccountEntityDetails";
                _getDigiWizAcEntyDetlsExecution.Input_payload = request.ToString();
                WizAcEntyReturn Casetatus = await _getDigiWizAcEntyDetlsExecution.ValidateWizAcEntyDetls(request, Header_Value);

                return Ok(Casetatus);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
