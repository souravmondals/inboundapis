namespace DigiLead.Controllers
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



    [Route("[controller]")]
    [ApiController]
    public class FetchDigiLeadStatusController : ControllerBase
    {

        private readonly ICreateCaseExecution _createCaseExecution;
        
        public FetchDigiLeadStatusController(ICreateCaseExecution createCaseExecution)
        {
            this._createCaseExecution = createCaseExecution;
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
                _createCaseExecution.API_Name = "FetchDigiLeadStatus";
                _createCaseExecution.Input_payload = request.ToString();
                CaseReturnParam Casetatus = await _createCaseExecution.ValidateCreateCase(request, Header_Value);

                return Ok(Casetatus);


            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }
    }
}
