using System.ComponentModel.DataAnnotations;
using POS.Application.Common.Models;

namespace POS.Application.Common.DataTransferObjects.UserDtos;
public class UserDto : BaseModel
{
    [Required, StringLength(50)]
    public string FirstName { get; set; } = string.Empty;
    [StringLength(50)]
    public string LastName { get; set; } = string.Empty;
    [StringLength(15)]
    public string PhoneNumber { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
