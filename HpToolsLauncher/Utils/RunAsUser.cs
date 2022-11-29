
using System.ComponentModel;
using System.Runtime.InteropServices;

namespace HpToolsLauncher.Utils
{
    public class RunAsUser
    {
        private string _username;
        private string _password;
        private string _domain;

        [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool LogonUser(string lpszUsername, string lpszDomain, string lpszPassword, int dwLogonType, int dwLogonProvider, out SafeTokenHandle phToken);
        public RunAsUser(string username, string domain, string pwd)
        {
            _username = username;
            _password = pwd;
            _domain = domain;
        }
        public string Username
        {
            get { return _username; }
        }
        public string Password
        {
            get { return _password; }
        }
        public string Domain
        {
            get { return _domain; }
        }

        public SafeTokenHandle LogonUser()
        {
            SafeTokenHandle safeTokenHandle;
            const int LOGON32_PROVIDER_DEFAULT = 0;
            //This parameter causes LogonUser to create a primary token.
            const int LOGON32_LOGON_INTERACTIVE = 2;

            // Call LogonUser to obtain a handle to an access token.
            bool isLoggedIn = LogonUser(_username, _domain, _password, LOGON32_LOGON_INTERACTIVE, LOGON32_PROVIDER_DEFAULT, out safeTokenHandle);

            if (!isLoggedIn)
            {
                int ret = Marshal.GetLastWin32Error();
                ConsoleWriter.WriteLine(string.Format("LogonUser failed with error code : {0}", ret));
                throw new Win32Exception(ret);
            }
            return safeTokenHandle;
        }
    }
}
