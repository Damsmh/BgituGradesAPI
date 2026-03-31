using Asp.Versioning;
using BgituGrades.Models.Student;
using BgituGrades.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;


namespace BgituGrades.Controllers
{
    [Route("api/student")]
    [ApiController]
    public class StudentController(IStudentService studentService) : ControllerBase
    {
        private readonly IStudentService _studentService = studentService;

        [HttpGet]
        [ApiVersion("2.0")]
        [Authorize(Policy = "ViewOnly")]
        [ProducesResponseType(typeof(IEnumerable<StudentResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<StudentResponse>>> GetStudents([FromQuery] GetStudentsByGroupRequest request, CancellationToken cancellationToken)
        {
            var students = await _studentService.GetStudentsByGroupAsync(request, cancellationToken: cancellationToken);
            return Ok(students);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Edit")]
        [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<StudentResponse>> CreateStudent([FromBody] CreateStudentRequest request, CancellationToken cancellationToken)
        {
            var student = await _studentService.CreateStudentAsync(request, cancellationToken: cancellationToken);
            return CreatedAtAction(nameof(GetStudent), new { id = student.Id }, student);
        }

        [HttpGet("{id}")]
        [ApiVersion("1.0")]
        [Obsolete("deprecated")]
        [ProducesResponseType(typeof(StudentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<StudentResponse>> GetStudent([FromRoute] int id, CancellationToken cancellationToken)
        {
            var student = await _studentService.GetStudentByIdAsync(id, cancellationToken: cancellationToken);
            if (student == null)
                return NotFound(id);
            return Ok(student);
        }

        [HttpPut]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Edit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateStudent([FromBody] UpdateStudentRequest request, CancellationToken cancellationToken)
        {
            var success = await _studentService.UpdateStudentAsync(request, cancellationToken: cancellationToken);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpDelete]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Edit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteStudent([FromQuery] DeleteStudentRequest request, CancellationToken cancellationToken)
        {
            var success = await _studentService.DeleteStudentAsync(request.Id, cancellationToken: cancellationToken);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpDelete("all")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteAllStudents(CancellationToken cancellationToken)
        {
            await _studentService.DeleteAllAsync(cancellationToken: cancellationToken);
            return NoContent();
        }

        [HttpPost("import")]
        [ApiVersion("2.0")]
        [Authorize(Policy = "Admin")]
        [ProducesResponseType(typeof(ImportResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> ImportStudents(IFormFile file, CancellationToken cancellationToken)
        {
            if (file is null || file.Length == 0)
                return BadRequest("Файл пустой");

            if (!file.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))
                return BadRequest("Поддерживается только .xlsx формат");

            await using var stream = file.OpenReadStream();
            var result = await studentService.ImportStudentsFromXlsxAsync(stream, cancellationToken: cancellationToken);

            return Ok(result);
        }
    }
}
