using System;
using Crayon;

namespace DarkRift.Cli
{
    /// <summary>
    /// Handles the user's invoice details.
    /// </summary>
    internal class InvoiceManager
    {
        /// <summary>
        /// Returns the user's invoice number, or prompts for it if not set.
        /// </summary>
        /// <returns>The user's invoice number, or null if they do not have one.</returns>
        public string GetInvoiceNumber()
        {
            Profile profile = Profile.Load();

            if (string.IsNullOrWhiteSpace(profile.InvoiceNumber))
            {
                Console.WriteLine("To download a Pro release you must provide an invoice number to verify your purchase. This will usually be found in your receipt from the Unity Asset Store.");
                Console.Write("Please enter it: ");
                string invoiceNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    Console.Error.WriteLine(Output.Red("No invoice number passed, no changes made."));
                    return null;
                }

                profile.InvoiceNumber = invoiceNumber;
                profile.Save();
            }

            return profile.InvoiceNumber;
        }
    }
}
