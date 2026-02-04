using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TicketSalesApp.Core.Models
{
    /// <summary>
    /// Represents a WebAuthn (FIDO2) credential associated with a user
    /// </summary>
    public class WebAuthnCredential
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        /// <summary>
        /// The credential ID as returned by the authenticator
        /// </summary>
        [Required]
        public byte[] CredentialId { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The public key of the credential
        /// </summary>
        [Required]
        public byte[] PublicKey { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The user handle (user ID) associated with this credential
        /// </summary>
        [Required]
        public byte[] UserHandle { get; set; } = Array.Empty<byte>();

        /// <summary>
        /// The signature counter from the authenticator
        /// </summary>
        public uint SignatureCounter { get; set; }

        /// <summary>
        /// The credential type (currently always "public-key")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string CredType { get; set; } = "public-key";

        /// <summary>
        /// When this credential was registered
        /// </summary>
        [Required]
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// When this credential was last used for authentication
        /// </summary>
        public DateTime? LastUsedAt { get; set; }

        /// <summary>
        /// User-friendly name for this credential (e.g., "iPhone Touch ID", "YubiKey")
        /// </summary>
        [MaxLength(100)]
        public string? FriendlyName { get; set; }

        /// <summary>
        /// Whether this credential is currently active
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// The AAGUID (Authenticator Attestation GUID) of the authenticator
        /// </summary>
        public byte[]? AaGuid { get; set; }

        /// <summary>
        /// The attestation format used during registration
        /// </summary>
        [MaxLength(50)]
        public string? AttestationFormat { get; set; }

        /// <summary>
        /// Additional attestation data (JSON format)
        /// </summary>
        public string? AttestationData { get; set; }

        // Foreign key to User
        [Required]
        public Guid UserId { get; set; }

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;
    }
}