﻿using System.Text.RegularExpressions;
using Helpers.Interfaces;

namespace Helpers
{
    public class AllowedUrls : IAllowedUrls
    {
        private static readonly string[] Patterns =
        {
            @"^users/\d+$",
            @"^api/passwords/users/otp$",
            @"^users/create$",
            @"^users/login$", // Added new allowed endpoint
            @"^api/passwords/user/\+\d{1,15}/otp/\d{4}/verify$",
            @"^api/passwords/changepassword$",
            @"^anonymous/authenticate$" 
        };

        private static readonly Regex[] CompiledPatterns = Patterns.Select(pattern => new Regex(pattern, RegexOptions.Compiled)).ToArray();

        public bool IsAllowed(string resource)
        {
            if (string.IsNullOrEmpty(resource))
            {
                return false;
            }

            return CompiledPatterns.Any(regex => regex.IsMatch(resource));
        }
    }
}