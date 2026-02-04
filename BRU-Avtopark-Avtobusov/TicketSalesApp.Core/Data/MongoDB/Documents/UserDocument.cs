#if MODERN
using System;
using System.Collections.Generic;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using TicketSalesApp.Core.Data.MongoDB;

namespace TicketSalesApp.Core.Data.MongoDB.Documents
{
    /// <summary>
    /// MongoDB document representation of User entity
    /// This allows MongoDB to be used as an alternative main database
    /// </summary>
    [BsonCollection("users")]
    public class UserDocument : BaseDocument
    {
        [BsonElement("userId")]
        public long UserId { get; set; }
        
        [BsonElement("guidId")]
        public Guid GuidId { get; set; }
        
        [BsonElement("login")]
        public string Login { get; set; } = string.Empty;
        
        [BsonElement("passwordHash")]
        public string PasswordHash { get; set; } = string.Empty;
        
        [BsonElement("role")]
        public int Role { get; set; }
        
        [BsonElement("phoneNumber")]
        public string? PhoneNumber { get; set; }
        
        [BsonElement("email")]
        public string? Email { get; set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
        
        [BsonElement("lastLoginAt")]
        public DateTime? LastLoginAt { get; set; }
        
        [BsonElement("windowsIdentity")]
        public string? WindowsIdentity { get; set; }
        
        [BsonElement("isWindowsAuth")]
        public bool IsWindowsAuth { get; set; }
        
        [BsonElement("doesWindowsAccountNeedLinking")]
        public bool DoesWindowsAccountNeedLinking { get; set; }
        
        [BsonElement("linkedRegularAccountUsername")]
        public string? LinkedRegularAccountUsername { get; set; }
        
        [BsonElement("linkedAccountToken")]
        public string? LinkedAccountToken { get; set; }
        
        [BsonElement("twoFactorEnabled")]
        public bool TwoFactorEnabled { get; set; }
        
        [BsonElement("totpSecret")]
        public string? TotpSecret { get; set; }
        
        [BsonElement("recoveryCodes")]
        public List<string>? RecoveryCodes { get; set; }
        
        [BsonElement("lastPasswordChange")]
        public DateTime? LastPasswordChange { get; set; }
        
        [BsonElement("failedLoginAttempts")]
        public int FailedLoginAttempts { get; set; }
        
        [BsonElement("lockoutEnd")]
        public DateTime? LockoutEnd { get; set; }
        
        [BsonElement("userRoles")]
        public List<UserRoleDocument>? UserRoles { get; set; }
        
        [BsonElement("webAuthnCredentials")]
        public List<WebAuthnCredentialDocument>? WebAuthnCredentials { get; set; }
    }
    
    public class UserRoleDocument
    {
        [BsonElement("roleId")]
        public long RoleId { get; set; }
        
        [BsonElement("roleName")]
        public string RoleName { get; set; } = string.Empty;
        
        [BsonElement("assignedAt")]
        public DateTime AssignedAt { get; set; }
        
        [BsonElement("assignedBy")]
        public long? AssignedBy { get; set; }
    }
    
    public class WebAuthnCredentialDocument
    {
        [BsonElement("id")]
        public Guid Id { get; set; }
        
        [BsonElement("credentialId")]
        public string CredentialId { get; set; } = string.Empty;
        
        [BsonElement("publicKey")]
        public string PublicKey { get; set; } = string.Empty;
        
        [BsonElement("userHandle")]
        public string UserHandle { get; set; } = string.Empty;
        
        [BsonElement("deviceName")]
        public string? DeviceName { get; set; }
        
        [BsonElement("createdAt")]
        public DateTime CreatedAt { get; set; }
        
        [BsonElement("lastUsedAt")]
        public DateTime? LastUsedAt { get; set; }
        
        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
#endif