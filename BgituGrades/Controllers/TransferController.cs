using Asp.Versioning;
using BgituGrades.Models.Student;
using BgituGrades.Models.Transfer;
using BgituGrades.Services;
using Microsoft.AspNetCore.Mvc;

namespace BgituGrades.Controllers
{
    [Route("api/transfer")]
    [ApiController]
    public class TransferController(ITransferService TransferService) : ControllerBase
    {
        private readonly ITransferService _transferService = TransferService;

        [HttpGet]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(IEnumerable<TransferResponse>), StatusCodes.Status200OK)]
        public async Task<ActionResult<IEnumerable<TransferResponse>>> GetTransfers()
        {
            var transfers = await _transferService.GetAllTransfersAsync();
            return Ok(transfers);
        }

        [HttpPost]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status201Created)]
        public async Task<ActionResult<TransferResponse>> CreateTransfer([FromBody] CreateTransferRequest request)
        {
            var transfer = await _transferService.CreateTransferAsync(request);
            return CreatedAtAction(nameof(GetTransfer), new { id = transfer.Id }, transfer);
        }

        [HttpGet("{id}")]
        [ApiVersion("2.0")]
        [ProducesResponseType(typeof(TransferResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<TransferResponse>> GetTransfer([FromRoute] int id)
        {
            var transfer = await _transferService.GetTransferByIdAsync(id);
            if (transfer == null)
                return NotFound(id);
            return Ok(transfer);
        }

        [HttpPut]
        [ApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(UpdateTransferRequest), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateTransfer([FromBody] UpdateTransferRequest request)
        {
            var success = await _transferService.UpdateTransferAsync(request);
            if (!success)
                return NotFound(request.Id);

            return NoContent();
        }

        [HttpDelete]
        [ApiVersion("2.0")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(NotFoundResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteTransfer([FromQuery] int id)
        {
            var success = await _transferService.DeleteTransferAsync(id);
            if (!success)
                return NotFound(id);

            return NoContent();
        }
    }
}
