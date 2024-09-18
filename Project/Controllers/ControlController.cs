using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using static Test_Project.Controllers.ControlController;

namespace Test_Project.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ControlController : ControllerBase
    {
      
        private readonly PeriodicTaskService _periodicTaskService;
        //// Constructor with dependency injection
        //public ControlController(ControlService controlService, PeriodicTaskService periodicTaskService)
        //{
        //    _controlService = controlService ?? throw new ArgumentNullException(nameof(controlService));
        //    _periodicTaskService = periodicTaskService ?? throw new ArgumentNullException(nameof(periodicTaskService));
        //}

        //[HttpPost("start")]
        //public IActionResult StartService()
        //{
        //    _controlService.StartService();
        //    return Ok("Service started.");
        //}

        //[HttpPost("stop")]
        //public IActionResult StopService()
        //{
        //    _controlService.StopService();
        //    return Ok("Service stopped.");
        //}

        //[HttpGet("status")]
        //public IActionResult GetStatus()
        //{
        //    //var status = _periodicTaskService.IsRunning ? "running" : "stopped";
        //   // return Ok(new { Status = status });
        //}

        public interface ITaskStateService
        {
            bool IsTrade{ get; set; }
        }
        public class TaskStateService : ITaskStateService
        {
            private bool _isTrade;

            public bool IsTrade
            {
                get => _isTrade;
                set => _isTrade = value;
            }
        }
        //private readonly ITaskStateService _taskStateService;

        //public ControlController(ITaskStateService taskStateService)
        //{
        //    _taskStateService = taskStateService;
        //}

        //[HttpPost("start")]
        //public IActionResult Start()
        //{
        //    _taskStateService.IsRunning = true;
        //    return Ok("Service started.");
        //}

        //[HttpPost("stop")]
        //public IActionResult Stop()
        //{
        //    _taskStateService.IsRunning = false;
        //    return Ok("Service stopped.");
        //}

        private readonly ITaskStateService _taskStateService;
       // private readonly PeriodicTaskService _periodicTaskService;

        public ControlController(ITaskStateService taskStateService, PeriodicTaskService periodicTaskService)
        {
            
            _taskStateService = taskStateService;
            _periodicTaskService = periodicTaskService;
        }

        [HttpPost("start")]
        public IActionResult Start()
        {
            if (!_taskStateService.IsTrade)
            {
                _taskStateService.IsTrade = true;
                // _periodicTaskService.Start();
                // Ensure the service starts
               // Log.Information("Service started at {DateTime.Now}.");
                return Ok($"Service started at {DateTime.Now}.");
            }

            return BadRequest($"Service is already running as of DateTime.Now {DateTime.Now}.");

        }

        [HttpPost("stop")]
        public IActionResult Stop()
        {
            if (_taskStateService.IsTrade)
            {
                //_taskStateService.IsTrade = false;
              //  _periodicTaskService.Stop(); // Ensure the service stops
                return Ok($"Service started at DateTime.UtcNow {DateTime.UtcNow}.");
            }
            return BadRequest("Service is not running.");
        }
    }

}



