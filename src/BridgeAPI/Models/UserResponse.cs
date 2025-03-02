using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Represents a user in the system.
    /// </summary>
    public class UserResponse
    {
        /// <summary>
        /// Gets or sets the first name of the user.
        /// This field is required.
        /// </summary>
        [Required(ErrorMessage = "UserId name is required.")]
        public int UserId { get; set; }
        /// <summary>
        /// Gets or sets the first name of the user.
        /// This field is required.
        /// </summary>
        [Required(ErrorMessage = "First name is required.")]
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the last name of the user.
        /// This field is required.
        /// </summary>
        [Required(ErrorMessage = "Last name is required.")]
        public string LastName { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the date of birth of the user.
        /// This should be in a valid date format.
        /// </summary>
        [Required(ErrorMessage = "Date of birth is required.")]
        [DataType(DataType.Date, ErrorMessage = "Date of birth must be a valid date.")]
        public string DateOfBirth { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the contact information of the user.
        /// This field is required.
        /// </summary>
        [Required(ErrorMessage = "User contacts are required.")]
        public UserContacts UserContacts { get; set; } = new UserContacts();

        /// <summary>
        /// Gets or sets the session identifier associated with the user.
        /// This field is required.
        /// </summary>
        [Required(ErrorMessage = "Session ID is required.")]
        public int SessionId { get; set; }

        /// <summary>
        /// Gets or sets the security token for the user.
        /// This is optional and may be null if not applicable.
        /// </summary>
        public string? AccountCreationDate { get; set; }
    }
}
