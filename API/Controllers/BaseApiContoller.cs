using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Base Controller API parent
/// </summary>
namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BaseApiContoller : ControllerBase {
        public const int SUCCESS_CODE = 1;
        public const int ERROR_CODE = 2;
        public const int FAILURE_CODE = 3;
    }
}