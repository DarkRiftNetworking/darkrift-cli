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
        /// The application's context.
        /// </summary>
        private readonly Context context;

        /// <summary>
        /// Creates a new invoice manager.
        /// </summary>
        /// <param name="context">The application's context.</param>
        public InvoiceManager(Context context)
        {
            this.context = context;
        }

        /// <summary>
        /// Returns the user's invoice number, or prompts for it if not set.
        /// </summary>
        /// <returns>The user's invoice number, or null if they do not have one.</returns>
        public string GetInvoiceNumber()
        {
            if (string.IsNullOrWhiteSpace(context.Profile.InvoiceNumber))
            {
                Console.WriteLine("To download a Pro release you must provide an invoice number to verify your purchase. This will usually be found in your receipt from the Unity Asset Store.");
                Console.Write("Please enter it: ");
                string invoiceNumber = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(invoiceNumber))
                {
                    Console.Error.WriteLine(Output.Red("No invoice number passed, no changes made."));
                    return null;
                }

                context.Profile.InvoiceNumber = invoiceNumber;
                context.Save();
            }

            return context.Profile.InvoiceNumber;
        }
    }
}
