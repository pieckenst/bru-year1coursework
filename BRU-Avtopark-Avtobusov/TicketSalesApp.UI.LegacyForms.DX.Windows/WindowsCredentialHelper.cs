using System;
using System.Runtime.InteropServices;
using System.Text;

namespace TicketSalesApp.UI.LegacyForms.DX.Windows
{
    /// <summary>
    /// Helper class to show Windows XP/Vista-style credential dialog using CredUI.dll
    /// Compatible with .NET Framework 4.0
    /// </summary>
    public static class WindowsCredentialHelper
    {
        [DllImport("credui.dll", CharSet = CharSet.Unicode)]
        private static extern CredUIReturnCodes CredUIPromptForCredentials(
            ref CREDUI_INFO creditUR,
            string targetName,
            IntPtr reserved1,
            int iError,
            StringBuilder userName,
            int maxUserName,
            StringBuilder password,
            int maxPassword,
            [MarshalAs(UnmanagedType.Bool)] ref bool pfSave,
            CREDUI_FLAGS flags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDUI_INFO
        {
            public int cbSize;
            public IntPtr hwndParent;
            public string pszMessageText;
            public string pszCaptionText;
            public IntPtr hbmBanner;
        }

        [Flags]
        private enum CREDUI_FLAGS
        {
            INCORRECT_PASSWORD = 0x1,
            DO_NOT_PERSIST = 0x2,
            REQUEST_ADMINISTRATOR = 0x4,
            EXCLUDE_CERTIFICATES = 0x8,
            REQUIRE_CERTIFICATE = 0x10,
            SHOW_SAVE_CHECK_BOX = 0x40,
            ALWAYS_SHOW_UI = 0x80,
            REQUIRE_SMARTCARD = 0x100,
            PASSWORD_ONLY_OK = 0x200,
            VALIDATE_USERNAME = 0x400,
            COMPLETE_USERNAME = 0x800,
            PERSIST = 0x1000,
            SERVER_CREDENTIAL = 0x4000,
            EXPECT_CONFIRMATION = 0x20000,
            GENERIC_CREDENTIALS = 0x40000,
            USERNAME_TARGET_CREDENTIALS = 0x80000,
            KEEP_USERNAME = 0x100000,
        }

        private enum CredUIReturnCodes
        {
            NO_ERROR = 0,
            ERROR_CANCELLED = 1223,
            ERROR_NO_SUCH_LOGON_SESSION = 1312,
            ERROR_NOT_FOUND = 1168,
            ERROR_INVALID_ACCOUNT_NAME = 1315,
            ERROR_INSUFFICIENT_BUFFER = 122,
            ERROR_INVALID_PARAMETER = 87,
            ERROR_INVALID_FLAGS = 1004,
        }

        /// <summary>
        /// Shows the classic Windows credential dialog
        /// </summary>
        /// <param name="caption">Dialog caption</param>
        /// <param name="message">Message to display</param>
        /// <param name="parentHandle">Parent window handle (optional)</param>
        /// <returns>Tuple with (Domain, Username, Password) or null if cancelled</returns>
        public static Tuple<string, string, string> PromptForCredentials(
            string caption, 
            string message, 
            IntPtr parentHandle = default(IntPtr))
        {
            // Initialize credential info
            CREDUI_INFO credInfo = new CREDUI_INFO();
            credInfo.cbSize = Marshal.SizeOf(credInfo);
            credInfo.pszCaptionText = caption;
            credInfo.pszMessageText = message;
            credInfo.hwndParent = parentHandle;

            // Set up buffers
            StringBuilder userNameBuffer = new StringBuilder(256);
            StringBuilder passwordBuffer = new StringBuilder(256);
            bool save = false;

            // Set flags for Windows authentication
            CREDUI_FLAGS flags = CREDUI_FLAGS.GENERIC_CREDENTIALS |
                                CREDUI_FLAGS.ALWAYS_SHOW_UI |
                                CREDUI_FLAGS.DO_NOT_PERSIST |
                                CREDUI_FLAGS.EXCLUDE_CERTIFICATES;

            // Show dialog
            CredUIReturnCodes result = CredUIPromptForCredentials(
                ref credInfo,
                string.Empty,
                IntPtr.Zero,
                0,
                userNameBuffer,
                256,
                passwordBuffer,
                256,
                ref save,
                flags);

            if (result == CredUIReturnCodes.NO_ERROR)
            {
                string fullUsername = userNameBuffer.ToString();
                string password = passwordBuffer.ToString();
                string domain = string.Empty;
                string username = fullUsername;

                // Parse domain\username format
                if (fullUsername.Contains("\\"))
                {
                    string[] parts = fullUsername.Split('\\');
                    if (parts.Length == 2)
                    {
                        domain = parts[0];
                        username = parts[1];
                    }
                }
                else if (fullUsername.Contains("@"))
                {
                    // Handle UPN format (username@domain)
                    string[] parts = fullUsername.Split('@');
                    if (parts.Length == 2)
                    {
                        username = parts[0];
                        domain = parts[1];
                    }
                }

                return new Tuple<string, string, string>(domain, username, password);
            }

            return null; // User cancelled or error
        }
    }
}
