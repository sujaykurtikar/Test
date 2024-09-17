using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class LogsController : ControllerBase
{
    private readonly string _logFilePath = "Logs"; // Adjust the path as needed

    [HttpGet("date")]
    public async Task<IActionResult> GetLogsByDate(string date = null)
    {
        try
        {
            DateTime logDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);
            string fileName = $"log-{logDate:yyyyMMdd}.txt"; // Adjust based on your file naming convention
            string filePath = Path.Combine(_logFilePath, fileName);
            string tempFilePath = Path.GetTempFileName(); // Create a temporary file

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Log file for {logDate:yyyy-MM-dd} not found.");
            }

            // Copy the log file to a temporary location
            System.IO.File.Copy(filePath, tempFilePath, overwrite: true);

            string logContent;
            using (var fileStream = new FileStream(tempFilePath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (var reader = new StreamReader(fileStream))
            {
                logContent = await reader.ReadToEndAsync();
            }

            // Delete the temporary file after reading
            System.IO.File.Delete(tempFilePath);

            return Content(logContent, "application/json; charset=utf-8"); // Adjust content type as needed
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving logs: {ex.Message}");
        }
    }


    [HttpGet("download")]
    public async Task<IActionResult> DownloadLog(string date = null)
    {
        try
        {
            DateTime logDate = string.IsNullOrEmpty(date) ? DateTime.Today : DateTime.Parse(date);
            string fileName = $"log-{logDate:yyyyMMdd}.txt"; // Adjust based on your file naming convention
            string filePath = Path.Combine("Logs", fileName);
            string tempFilePath = Path.GetTempFileName(); // Create a temporary file

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound($"Log file for {logDate:yyyy-MM-dd} not found.");
            }

            // Copy the file to a temporary location
            System.IO.File.Copy(filePath, tempFilePath, true);

            var fileBytes = await System.IO.File.ReadAllBytesAsync(tempFilePath);
            System.IO.File.Delete(tempFilePath); // Delete the temporary file

            return File(fileBytes, "application/octet-stream", fileName);
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Error retrieving log file: {ex.Message}");
        }
    }
}
