using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace YourNamespace.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")]
    public class BackupController : ControllerBase
    {
        private readonly string backupPath = @"C:\backups\eventEase1.2.bak";
        private readonly string connectionString;

        public BackupController(IConfiguration configuration)
        {
            connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        [HttpGet("download")]
        public IActionResult DownloadBackup()
        {
            if (!System.IO.File.Exists(backupPath))
                return NotFound("Backup not found.");

            var fileBytes = System.IO.File.ReadAllBytes(backupPath);
            return File(fileBytes, "application/octet-stream", "eventEase1.2.bak");
        }

        [HttpPost("restore")]
        public async Task<IActionResult> RestoreBackup([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest("No file uploaded.");

            var tempPath = Path.Combine(Path.GetDirectoryName(backupPath), "temp.bak");

            using (var stream = new FileStream(tempPath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Run SQL Restore using SqlCommand
            try
            {
                var restoreQuery = $@"
USE master;
ALTER DATABASE [eventEase1.2] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [eventEase1.2]
FROM DISK = N'{tempPath}'
WITH REPLACE;
ALTER DATABASE [eventEase1.2] SET MULTI_USER;
";

                using (var connection = new SqlConnection(connectionString))
                {
                    using (var command = new SqlCommand(restoreQuery, connection))
                    {
                        await connection.OpenAsync();
                        await command.ExecuteNonQueryAsync();
                    }
                }

                // Replace the old backup with the new one
                System.IO.File.Copy(tempPath, backupPath, overwrite:true);
                System.IO.File.Delete(tempPath);

                return Ok("Database restored successfully.");
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, $"Restore failed: {ex.Message}");
            }
        }
    }
}
