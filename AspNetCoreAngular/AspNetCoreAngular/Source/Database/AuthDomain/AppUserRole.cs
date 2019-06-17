using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;

namespace Database.AuthDomain
{
    public class AppUserRole : IdentityRole
    {
        /// <summary>
        /// Initializes a new instance of <see cref="AppUserRole"/>.
        /// </summary>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public AppUserRole()
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="AppUserRole"/>.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public AppUserRole(string roleName) : base(roleName)
        {

        }

        /// <summary>
        /// Initializes a new instance of <see cref="AppUserRole"/>.
        /// </summary>
        /// <param name="roleName">The role name.</param>
        /// <param name="description">Description of the role.</param>
        /// <remarks>
        /// The Id property is initialized to from a new GUID string value.
        /// </remarks>
        public AppUserRole(string roleName, string description) : base(roleName)
        {
            Description = description;
        }


        /// <summary>
        /// Gets or sets the description for this role.
        /// </summary>
        public string Description { get; set; }


        /// <summary>
        /// Navigation property for the users in this role.
        /// </summary>
        public virtual ICollection<IdentityUserRole<string>> Users { get; set; }

        /// <summary>
        /// Navigation property for claims in this role.
        /// </summary>
        public virtual ICollection<IdentityRoleClaim<string>> Claims { get; set; }

    }
}
