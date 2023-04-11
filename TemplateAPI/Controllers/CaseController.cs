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

namespace ManageCase.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class CaseController : ControllerBase
    {

        private readonly ILogger<CaseController> _log;
        private readonly IQueryParser _queryp;
        private readonly IKeyVaultService _keyVaultService;


        public CaseController(ILogger<CaseController> log, IQueryParser queryParser, IKeyVaultService keyVaultService) 
        {
            this._log = log;
            this._queryp = queryParser;
            this._keyVaultService = keyVaultService;
        }

        [HttpPost("CreateCase")]        
        public async Task<IActionResult> Post()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateCaseExecution createleadEx = new CreateCaseExecution(this._log, this._queryp, this._keyVaultService);

                string Header_Value = string.Empty;
                if (Request.Headers.TryGetValue("appkey", out var headerValues))
                {
                    Header_Value = headerValues;
                }


                LeadReturnParam Leadstatus = await createleadEx.ValidateLeade(request, Header_Value);

                
                return Ok(Leadstatus);
                
                    
            }
            catch (Exception ex)
            {

                return BadRequest();

            }
        }

        [HttpPost("UpdateCaseStatus")]
        public async Task<IActionResult> UpdateCaseStatus()
        {
            try
            {
                StreamReader requestReader = new StreamReader(Request.Body);
                dynamic request = JObject.Parse(await requestReader.ReadToEndAsync());
                CreateCaseExecution createleadEx = new CreateCaseExecution(this._log, this._queryp, this._keyVaultService);
                LeadReturnParam Leadstatus = await createleadEx.ValidateLeadeStatus(request);
                return Ok(Leadstatus);
            }
            catch (Exception ex)
            {

                return BadRequest();

            }

        }


    }
}
