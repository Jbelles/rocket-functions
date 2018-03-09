//Created: 2-15-2018 by Jarret Belles
//This is a sample console application that can stream new operations on the stellar network using the .Net Core Stellar SDK.
//This app watches for a successful inflation operation, calculates the earnings of each member of the owner's pool, and pays them out.
using Microsoft.Extensions.Configuration;
using Npgsql;
using Rocket.Common.Responses;
using stellar_dotnetcore_sdk;
using stellar_dotnetcore_sdk.responses;
using stellar_dotnetcore_sdk.responses.effects;
using stellar_dotnetcore_sdk.responses.operations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace TestConsole
{
    public class Program
    {
        //LIVENET SERVER
        //static Server server = new Server("https://horizon.stellar.org");

        //TESTNET SERVER
        static Server server = new Server("https://horizon-testnet.stellar.org");

        static bool inflation_found = false;
        static IConfiguration Configuration;

        public static void Main(string[] args)
        {
            //Build configuration from appsettings
            string path = Directory.GetCurrentDirectory();
            var builder = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);

            Configuration = builder.Build();
            Network.UseTestNetwork();
            Network.UsePublicNetwork();
            
            Console.WriteLine("-- Streaming All New Operations On The Network --");

            //Begin streaming operations
            server.Operations
                .Cursor("now")
                .Stream((sender, response) => { ShowOperationResponse(response); })
                .Connect();

            Console.ReadLine();
        }

        private static async void ShowOperationResponse(OperationResponse op)
        {
            if (op is CreateAccountOperationResponse)
            {
                Console.WriteLine($"Hey I'm a Create Account Operation: {op.Id}");
            }
            else if (op is PaymentOperationResponse)
            {
                Console.WriteLine($"Hey I'm a Payment Operation: {op.Id}");
            }
            else if (op is AllowTrustOperationResponse)
            {
                Console.WriteLine($"Hey I'm an Allow Trust Operation: {op.Id}");
            }
            else if (op is ChangeTrustOperationResponse)
            {
                Console.WriteLine($"Hey I'm a Change Trust Operation: {op.Id}");
            }
            else if (op is SetOptionsOperationResponse)
            {
                Console.WriteLine($"Hey I'm a Set Options Operation: {op.Id}");
            }
            else if (op is AccountMergeOperationResponse)
            {
                Console.WriteLine($"Hey I'm an Account Merge Operation: {op.Id}");
            }
            else if (op is ManageOfferOperationResponse)
            {
                Console.WriteLine($"Hey I'm a Manage Offer Operation: {op.Id}");
            }
            else if (op is PathPaymentOperationResponse)
            {
                Console.WriteLine($"Hey I'm a Path Payment Operation: {op.Id}");
            }
            else if (op is CreatePassiveOfferOperationResponse)
            {
                Console.WriteLine($"Hey I'm a Create Passive Offer Operation: {op.Id}");
            }
            else if (op is InflationOperationResponse)
            {
                Console.WriteLine($"Hey I'm an Inflation Operation: {op.Id}");
                var effects = await server.Effects.ForOperation(op.Id).Execute();
                if (effects.Records != null && effects.Records.Count > 0)
                {
                    foreach (EffectResponse record in effects.Records)
                    {
                        Console.WriteLine(record.Account.AccountId + "earned inflation this week.");

                        //CHECK IF YOUR POOL ACCOUNT IS THERE
                        if (record.Account.AccountId == Configuration["INFLATION_POOL"])
                        //if (record.Account.AccountId == Environment.GetEnvironmentVariable("INFLATION_POOL"))
                        {
                            inflation_found = true;
                            if (record is AccountCreditedEffectResponse)
                            {
                                Console.WriteLine($"Pool {record.Account.AccountId} was credited with { (record as AccountCreditedEffectResponse).Amount}XLM");
                                //InflationAmount = double.Parse((record as AccountCreditedEffectResponse).Amount);
                                Payout(decimal.Parse((record as AccountCreditedEffectResponse).Amount));
                            }
                        }
                    }
                    if (!inflation_found)
                    {
                        Console.WriteLine("Looks like the pool didn't earn inflation this week.");
                        Console.WriteLine($"See all valid pools here: https://horizon.stellar.org/operations/{op.Id}/effects");
                    }
                }
                else
                {
                    Console.WriteLine("It's not time for Inflation Yet");
                }
            }
            else if (op is ManageDataOperationResponse)
            {
                Console.WriteLine("Hey I'm a Manage Data Operation: {op.Id}");
            }

        }

        public static async void Payout(decimal InflationAmount)
        {
            Network.UseTestNetwork();

            var votes = GetTotalVotes();
            var accounts = GetVoterAccounts(InflationAmount, votes);

            decimal sum = 0;
            decimal percentage = 0;

            //KeyPair PoolSource = KeyPair.FromSecretSeed(Configuration["INFLATION_POOL_SECRET"]);
            KeyPair PoolSource = KeyPair.FromSecretSeed(Environment.GetEnvironmentVariable("INFLATION_POOL_SECRET"));

            AccountResponse sourceAccount = await server.Accounts.Account(PoolSource);
            var sequenceNumber = sourceAccount.SequenceNumber;
            Account PoolAccount = new Account(PoolSource, sequenceNumber);

            Transaction.Builder BatchTransaction = new Transaction.Builder(PoolAccount);
            List<Transaction> Transactions = new List<Transaction>();

            int batch = 0;
            foreach (var account in accounts)
            {
                if (batch < 100)
                {
                    var payout = RoundDown(account.Payout, 7);
                    var payoutPercent = RoundDown(account.Percentage, 7);
                    var operation = new PaymentOperation.Builder(KeyPair.FromAccountId(account.AccountId), new AssetTypeNative(), (payout - .0000100m).ToString())
                        .SetSourceAccount(PoolSource)
                        .Build();
                    BatchTransaction.AddOperation(operation);

                    Console.WriteLine($"Paying out: {payout}XLM (%{payoutPercent}) to Account: {account.AccountId}");
                    sum += payout;
                    percentage += payoutPercent;
                }
                if (batch == 99 || account.Equals(accounts.LastOrDefault()))
                {
                    var t = BatchTransaction.AddMemo(Memo.Text($"Sample Memo")).Build();
                    t.Sign(PoolSource);
                    Transactions.Add(t);
                    BatchTransaction = new Transaction.Builder(PoolAccount);
                }
                batch = batch == 99 ? 0 : ++batch;
            }

            Console.WriteLine("Submitting batches to the stellar network...");
            foreach (var t in Transactions)
            {
                Console.WriteLine($"Submitting batch: {Transactions.IndexOf(t) + 1}...");
                var response = await server.SubmitTransaction(t);
                Console.WriteLine($"Batch submitted.");
                //Console.WriteLine($"Envelope XDR is:");
                //Console.WriteLine($"{response.EnvelopeXdr}");
            }

            Console.WriteLine($"Payed out: {sum} (%{percentage})");
            //Exists the console app after it has found inflation and payed out.
            Console.WriteLine("Exiting");
            Environment.Exit(0);
        }

        public static decimal GetTotalVotes()
        {
            var connString = Configuration["ConnectionString"];
            //var connString = Environment.GetEnvironmentVariable("ConnectionString");

            decimal totalVotes = 0;
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                List<VoterAccountResponse> Accounts = new List<VoterAccountResponse>();

                // Retrieve all rows
                var cmd = new NpgsqlCommand("SELECT SUM(balance) FROM accounts WHERE inflationdest = @inflationdest", conn);

                //THIS IS SOME OTHER POOL
                cmd.Parameters.AddWithValue("inflationdest", Configuration["INFLATION_POOL"]);
                //cmd.Parameters.AddWithValue("inflationdest", Environment.GetEnvironmentVariable("INFLATION_POOL"));

                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        totalVotes = reader.IsDBNull(0) ? 0 : reader.GetFieldValue<decimal>(0);
                    }

                return totalVotes;
            }
        }
        public static List<VoterAccountResponse> GetVoterAccounts(decimal InflationAmount, decimal TotalVotes)
        {
            var connString = Configuration["ConnectionString"];
            //var connString = Environment.GetEnvironmentVariable("ConnectionString");

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                List<VoterAccountResponse> Accounts = new List<VoterAccountResponse>();

                // Retrieve all rows
                var cmd = new NpgsqlCommand("SELECT accounts.accountid, balance FROM accounts WHERE inflationdest = @inflationdest", conn);

                cmd.Parameters.AddWithValue("inflationdest", Configuration["INFLATION_POOL"]);
                //cmd.Parameters.AddWithValue("inflationdest", Environment.GetEnvironmentVariable("INFLATION_POOL"));


                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {

                        VoterAccountResponse account = new VoterAccountResponse(
                            reader.GetString(0),
                            reader.IsDBNull(1) ? 0 : reader.GetFieldValue<decimal>(1),
                            InflationAmount, 
                            TotalVotes
                        );
                        Accounts.Add(account);
                    }

                return Accounts;
            }

        }
        public static decimal RoundDown(decimal number, int decimalPlaces)
        {
            return Math.Floor(number * ((decimal) Math.Pow(10, decimalPlaces))) / ((decimal) Math.Pow(10, decimalPlaces));
        }
    }
}