using HRM.backend.src.HRM.API.Extensions;
using Microsoft.AspNetCore.Mvc;

namespace HRM.backend.src.HRM.API.Controllers.PersonnelChanges
{
    public abstract class PersonnelChangeControllerBase : ControllerBase
    {
        protected int ActorAccountId => User.GetAccountIdOrThrow();

        protected static async Task<IActionResult> ExecuteAsync(Func<Task<IActionResult>> action)
        {
            try
            {
                return await action();
            }
            catch (KeyNotFoundException ex)
            {
                return new NotFoundObjectResult(new { Success = false, Message = ex.Message });
            }
            catch (UnauthorizedAccessException ex)
            {
                return new ObjectResult(new { Success = false, Message = ex.Message }) { StatusCode = StatusCodes.Status403Forbidden };
            }
            catch (ArgumentException ex)
            {
                return new BadRequestObjectResult(new { Success = false, Message = ex.Message });
            }
            catch (InvalidOperationException ex)
            {
                return new UnprocessableEntityObjectResult(new { Success = false, Message = ex.Message });
            }
            catch (NotImplementedException ex)
            {
                return new ObjectResult(new { Success = false, Message = ex.Message }) { StatusCode = StatusCodes.Status501NotImplemented };
            }
        }
    }
}
