using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BackendREST.Controllers
{

    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SecureController : ControllerBase
    {
        [HttpGet("datos-protegidos")]
        public IActionResult GetDatosProtegidos()
        {
            return Ok(new { Mensaje = "Acceso autorizado a datos protegidos" });
        }
    }
}
