using System;
using System.IO;
using System.Net.NetworkInformation;
using Standard.Licensing;

namespace Face_Matcher_UI
{
    public class check
    {
        public bool checklisence(string pathToLicFile)
        {
            string licenseText = File.ReadAllText(pathToLicFile);
            var license = Standard.Licensing.License.Load(licenseText);

            // Load your public key (from file or embedded resource)
            string publicKey = File.ReadAllText("public_key.pem");

            // Verify the license signature
            if (!license.VerifySignature(publicKey))
            {
                return false;
                // Invalid license
               // throw new UnauthorizedAccessException("License verification failed.");
            }

            // Check license expiration
            if (license.Expiration.Date <= DateTime.Now.Date)
            {
                return false;
                //throw new Exception("License has expired.");
            }

            // Check license type
            if (license.Type == LicenseType.Trial)
            {
                // Handle trial license differently if needed
                Console.WriteLine("Running in trial mode");
            }

            // Access customer information
            string customerName = license.Customer.Name;
            string customerEmail = license.Customer.Email;
            // Access custom attributes
            var machineKey = license.AdditionalAttributes?.Get("MachineKey");
            var attrs = license.AdditionalAttributes;
            var MacAddress = GetCurrentMachineId();
            if (machineKey != MacAddress)
            {
                /// throw new UnauthorizedAccessException("MachineKey mismatch");
                 return false;
            }
            else
            {
                return true;
            }

        }
        public bool checklisence()
        {
            string licenseText = File.ReadAllText("mylicense.lic");
            var license = Standard.Licensing.License.Load(licenseText);

            // Load your public key (from file or embedded resource)
            string publicKey = File.ReadAllText("public_key.pem");

            // Verify the license signature
            if (!license.VerifySignature(publicKey))
            {
                return false;
                // Invalid license
                // throw new UnauthorizedAccessException("License verification failed.");
            }

            // Check license expiration
            if (license.Expiration.Date <= DateTime.Now.Date)
            {
                return false;
                //throw new Exception("License has expired.");
            }

            // Check license type
            if (license.Type == LicenseType.Trial)
            {
                // Handle trial license differently if needed
                Console.WriteLine("Running in trial mode");
            }

            // Access customer information
            string customerName = license.Customer.Name;
            string customerEmail = license.Customer.Email;
            // Access custom attributes
            var machineKey = license.AdditionalAttributes?.Get("MachineKey");
            var attrs = license.AdditionalAttributes;
            var MacAddress = GetCurrentMachineId();
            if (machineKey != MacAddress)
            {
                /// throw new UnauthorizedAccessException("MachineKey mismatch");
                return false;
            }
            else
            {
                return true;
            }

        }
        public static string GetCurrentMachineId()
        {
            return NetworkInterface
            .GetAllNetworkInterfaces()
            .Where(nic =>
                nic.OperationalStatus == OperationalStatus.Up &&
                nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                !nic.Description.ToLower().Contains("virtual") &&
                !nic.Description.ToLower().Contains("pseudo"))
            .Select(nic => nic.GetPhysicalAddress().ToString())
            .FirstOrDefault();
        
    }
    }
}


