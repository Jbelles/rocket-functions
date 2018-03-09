using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Npgsql;
using Rocket.Common.Responses;
using stellar_dotnetcore_sdk;
using stellar_dotnetcore_sdk.requests;
using stellar_dotnetcore_sdk.responses;
using stellar_dotnetcore_sdk.responses.effects;
using stellar_dotnetcore_sdk.responses.operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rocket.AzureFunctions
{
    public static class InflationStreamFunction
    {
        static Server server = new Server("https://horizon.stellar.org");
        static Server server_test = new Server("https://horizon-testnet.stellar.org");
        static string POOL = "SAOT4K2SGA4F73EVIILQDH6OHWITZHSRDE72LBEMABPAKT3P2TNTCZ7E";
        //static string POOL = "SBPY5DMF5NCQ7OQ4QTYJRWQKILJ6PACEF6S373J6QS7RPW4OKDKFCLLN";
        static int stream_count = 0;

        //private static readonly string _getStreamKey = "";
        //private static readonly string _getSreamSecret = "";

        [FunctionName("InflationStreamFunction")]
        public static Task RunAsync([TimerTrigger("0 1 * * * *")] TimerInfo myTimer, TraceWriter log)
        {
            Network.UsePublicNetwork();

            //Streams are Maybe fixed? in this API until a resolution is found for the HttpClient issue
            Console.WriteLine("-- Streaming All New Operations On The Network --");

            server.Operations
                .Cursor("now")
                .Order(OrderDirection.ASC)
                .Limit(1)
                .Stream((sender, response) => { ShowOperationResponse(response); })
                .Connect();

            Console.ReadLine();
            return null;
        }

        private static async Task ShowAccountTransactions(Server server)
        {
            Console.WriteLine("-- Show Account Transactions (ForAccount) --");

            var transactions = await server.Transactions
                .ForAccount(KeyPair.FromAccountId("SAOT4K2SGA4F73EVIILQDH6OHWITZHSRDE72LBEMABPAKT3P2TNTCZ7E"))
                //.ForAccount(KeyPair.FromAccountId("GAZHWW2NBPDVJ6PEEOZ2X43QV5JUDYS3XN4OWOTBR6WUACTUML2CCJLI"))
                .Execute();

            ShowTransactionRecords(transactions.Records);
            Console.WriteLine();
        }

        private static async Task GetLedgerTransactions(Server server)
        {
            Console.WriteLine("-- Show Ledger Transactions (ForLedger) --");
            // get a list of transactions that occurred in ledger 1400
            var transactions = await server.Transactions
                .ForLedger(2365)
                .Execute();

            ShowTransactionRecords(transactions.Records);
            Console.WriteLine();
        }

        private static void ShowTransactionRecords(List<TransactionResponse> transactions)
        {
            foreach (var tran in transactions)
                ShowTransactionRecord(tran);
        }

        private static void ShowTransactionRecord(TransactionResponse tran)
        {
            Console.WriteLine($"Ledger: {tran.Ledger}, Hash: {tran.Hash}, Fee Paid: {tran.FeePaid}");
        }

        private static async void ShowOperationResponse(OperationResponse op)
        {
            if (stream_count > 0)
            {
                return;
            }
            stream_count++;
            var effects = await server.Effects.ForOperation(67968810941947905).Execute();
            bool found = false;
            //Page<EffectResponse> effects = await server.Effects.ForOperation(op.Id).Execute();
            if (effects.Records != null && effects.Records.Count > 0)
            {
                foreach (EffectResponse record in effects.Records)
                {
                    if (record.Account.AccountId == "GAREELUB43IRHWEASCFBLKHURCGMHE5IF6XSE7EXDLACYHGRHM43RFOX")
                    {
                        found = true;
                        if (record is AccountCreditedEffectResponse)
                        {
                            Console.WriteLine($"Pool {record.Account.AccountId} was credited with { (record as AccountCreditedEffectResponse).Amount}XLM");
                            //InflationAmount = double.Parse((record as AccountCreditedEffectResponse).Amount);
                            Payout(decimal.Parse((record as AccountCreditedEffectResponse).Amount));
                        }
                    }
                }
                if (!found)
                {
                    Console.WriteLine("Looks like the pool didn't earn inflation this week.");
                    Console.WriteLine($"See all pools here: https://horizon.stellar.org/operations/{op.Id}/effects");
                }
            }
            else
            {
                Console.WriteLine("It's not time for Inflation Yet");
            }
            if (op is CreateAccountOperationResponse)
            {
                Console.WriteLine("Hey I'm a Create Account Operation");
            }
            else if (op is PaymentOperationResponse)
            {
                Console.WriteLine("Hey I'm a Payment Operation");
            }
            else if (op is AllowTrustOperationResponse)
            {
                Console.WriteLine("Hey I'm an Allow Trust Operation");
            }
            else if (op is ChangeTrustOperationResponse)
            {
                Console.WriteLine("Hey I'm a Change Trust Operation");
            }
            else if (op is SetOptionsOperationResponse)
            {
                Console.WriteLine("Hey I'm a Set Options Operation");
            }
            else if (op is AccountMergeOperationResponse)
            {
                Console.WriteLine("Hey I'm an Account Merge Operation");
            }
            else if (op is ManageOfferOperationResponse)
            {
                Console.WriteLine("Hey I'm a Manage Offer Operation");
            }
            else if (op is PathPaymentOperationResponse)
            {
                Console.WriteLine("Hey I'm a Path Payment Operation");
            }
            else if (op is CreatePassiveOfferOperationResponse)
            {
                Console.WriteLine("Hey I'm a Create Passive Offer Operation");
            }
            else if (op is InflationOperationResponse)
            {
                Console.WriteLine("Hey I'm an Inflation Operation");
                ////var effects = await server.Effects.ForOperation(67968810941947905).Execute();
                //bool found = false;
                //Page<EffectResponse> effects = await server.Effects.ForOperation(op.Id).Execute();
                //if (effects.Records != null && effects.Records.Count > 0)
                //{
                //    foreach (EffectResponse record in effects.Records)
                //    {
                //        if (record.Account.AccountId == "GAREELUB43IRHWEASCFBLKHURCGMHE5IF6XSE7EXDLACYHGRHM43RFOX")
                //        {
                //            found = true;
                //            if (record is AccountCreditedEffectResponse)
                //            {
                //                Console.WriteLine($"Pool {record.Account.AccountId} was credited with { (record as AccountCreditedEffectResponse).Amount}XLM");
                //                //InflationAmount = double.Parse((record as AccountCreditedEffectResponse).Amount);
                //                Payout(decimal.Parse((record as AccountCreditedEffectResponse).Amount));
                //            }
                //        }
                //    }
                //    if (!found)
                //    {
                //        Console.WriteLine("Looks like the pool didn't earn inflation this week.");
                //        Console.WriteLine($"See all pools here: https://horizon.stellar.org/operations/{op.Id}/effects");
                //    }
                //}
                //else
                //{
                //    Console.WriteLine("It's not time for Inflation Yet");
                //}
            }
            else if (op is ManageDataOperationResponse)
            {
                Console.WriteLine("Hey I'm a Manage Data Operation");
            }
            //Console.WriteLine($"Id: {op.Id}, Source: {op.SourceAccount.AccountId}");

        }

        public static async void Payout(decimal InflationAmount)
        {
            Network.UseTestNetwork();

            var votes = GetTotalVotes();
            var accounts = GetVoterAccounts(InflationAmount, votes);

            decimal sum = 0;
            decimal percentage = 0;

            KeyPair PoolSource = KeyPair.FromSecretSeed(POOL);
            AccountResponse sourceAccount = await server_test.Accounts.Account(PoolSource);
            var sequenceNumber = sourceAccount.SequenceNumber;
            Account PoolAccount = new Account(PoolSource, sequenceNumber);

            Transaction.Builder BatchTransaction = new Transaction.Builder(PoolAccount);
            List<Transaction> Transactions = new List<Transaction>();

            int batch = 0;
            foreach (var account in accounts)
            {

                if (batch == 0)
                {
                    BatchTransaction = new Transaction.Builder(PoolAccount);
                }
                if (batch < 100)
                {
                    var payout = Math.Round(account.Payout, 7, MidpointRounding.AwayFromZero);
                    var payoutPercent = Math.Round(account.Percentage, 7, MidpointRounding.AwayFromZero);
                    //var operation = new PaymentOperation.Builder(KeyPair.FromAccountId("GDWJ5JOTJV37PNKXYXZJSUX57LJST3LJGGWRFU3VOK6VXLWXTI3JAVSR"), new AssetTypeNative(), payout.ToString())
                    //    .SetSourceAccount(PoolSource)
                    //    .Build();
                    var operation = new PaymentOperation.Builder(KeyPair.FromAccountId("GA2YP5PU6OZ5S3Y445DWTPMEYZDL3Z4OTGKV7IZNCIFKKMXJJU34J5EE"), new AssetTypeNative(), payout.ToString())
                        .SetSourceAccount(PoolSource)
                        .Build();
                    BatchTransaction.AddOperation(operation);


                    Console.WriteLine($"Paying out: {payout}XLM (%{payoutPercent}) to Account: {account.AccountId}");

                    sum += account.Payout;
                    percentage += account.Percentage;
                }
                if (batch == 99 || account.Equals(accounts.LastOrDefault()))
                {
                    var t = BatchTransaction.AddMemo(Memo.Text($"Rocket:{DateTime.UtcNow.ToShortTimeString()}")).Build();
                    t.Sign(PoolSource);
                    Transactions.Add(t);
                    BatchTransaction = new Transaction.Builder(PoolAccount);

                }
                batch = batch == 99 ? 0 : ++batch;
            }

            Console.WriteLine("Submitting batches to the stellar network.");
            foreach (var t in Transactions)
            {
                Console.WriteLine($"Batch: {Transactions.IndexOf(t) + 1} submitted");
                var response = await server_test.SubmitTransaction(t);
                Console.WriteLine($"Envelope XDR is:");
                Console.WriteLine($"{response.EnvelopeXdr}");
            }


            var TotalPayoutPercentgepercent = Math.Round(sum, 7, MidpointRounding.AwayFromZero);
            var TotalPayout = Math.Round(percentage, 7, MidpointRounding.AwayFromZero);
            Console.WriteLine($"Payed out: {sum} (%{percentage})");
        }

        public static decimal GetTotalVotes()
        {
            var connString = "Host=127.0.0.1;Username=postgres;Password=BusterPostgres7979;Database=Core-Live";
            decimal totalVotes = 0;
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                List<VoterAccountResponse> Accounts = new List<VoterAccountResponse>();

                // Retrieve all rows
                var cmd = new NpgsqlCommand("SELECT SUM(balance) FROM accounts WHERE inflationdest = @inflationdest", conn);

                //THIS IS SOME OTHER POOL
                cmd.Parameters.AddWithValue("inflationdest", "GAREELUB43IRHWEASCFBLKHURCGMHE5IF6XSE7EXDLACYHGRHM43RFOX");

                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {
                        totalVotes = reader.GetFieldValue<decimal>(0);
                    }

                return totalVotes;
            }
        }
        public static List<VoterAccountResponse> GetVoterAccounts(decimal InflationAmount, decimal TotalVotes)
        {
            var connString = "Host=127.0.0.1;Username=postgres;Password=BusterPostgres7979;Database=Core-Live";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                List<VoterAccountResponse> Accounts = new List<VoterAccountResponse>();

                // Retrieve all rows
                var cmd = new NpgsqlCommand("SELECT accounts.accountid, balance FROM accounts WHERE inflationdest = @inflationdest", conn);

                //THIS IS SOME OTHER POOL
                cmd.Parameters.AddWithValue("inflationdest", "GAREELUB43IRHWEASCFBLKHURCGMHE5IF6XSE7EXDLACYHGRHM43RFOX");

                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {

                        VoterAccountResponse account = new VoterAccountResponse(
                            reader.GetString(0),
                            reader.GetFieldValue<decimal>(1),
                            InflationAmount, TotalVotes

                        );
                        Accounts.Add(account);
                    }

                return Accounts;
            }
        }
    }

}
