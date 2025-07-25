using System;
using System.Runtime.InteropServices;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.CredUI;

namespace TicketSalesApp.Core
{
    public static class WindowsCredPicker
    {
        [DllImport("ole32.dll", SetLastError = true)]
        public static extern void CoTaskMemFree(IntPtr ptr);

        public static (string Domain, string Username, string Password)? Prompt(string caption, string message)
        {
            var info = new CREDUI_INFO
            {
                pszCaptionText = caption,
                pszMessageText = message,
                cbSize = (int)(uint)Marshal.SizeOf<CREDUI_INFO>()
            };

            uint authPkg = 0;
            bool save = false;

            var flags = WindowsCredentialsDialogOptions.CREDUIWIN_GENERIC | WindowsCredentialsDialogOptions.CREDUIWIN_SECURE_PROMPT;

            var result = CredUIPromptForWindowsCredentials(
                ref info,
                0,
                ref authPkg,
                IntPtr.Zero,
                0,
                out var outCred,
                out var outCredSize,
                ref save,
                flags);

            if (result.Failed || outCred == IntPtr.Zero)
                return null;

            var user = new StringBuilder(100);
            var domain = new StringBuilder(100);
            var pass = new StringBuilder(100);
            int uLen = user.Capacity, dLen = domain.Capacity, pLen = pass.Capacity;

            if (!CredUnPackAuthenticationBuffer(0, outCred, (int)outCredSize, user, ref uLen, domain, ref dLen, pass, ref pLen))
            {
                CoTaskMemFree(outCred);
                return null;
            }

            CoTaskMemFree(outCred);

            return (domain.ToString(), user.ToString(), pass.ToString());
        }
    }
}
