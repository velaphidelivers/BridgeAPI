using System.ComponentModel.DataAnnotations;

namespace Models
{
    /// <summary>
    /// Represents contact information for a user.
    /// </summary>
    public class UserContacts
    {
        /// <summary>
        /// Gets or sets the email address of the user.
        /// </summary>
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the mobile number of the user.
        /// This should be a valid phone number.
        /// </summary>
        [Phone]
        public string MobileNumber { get; set; } = string.Empty;
    }
}
