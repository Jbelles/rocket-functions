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
using System.Threading.Tasks;

namespace Rocket.AzureFunctions
{
    public static class InflationStreamFunction
    {
        //static Server server = new Server("https://horizon.stellar.org");
        static Server server_test = new Server("https://horizon-testnet.stellar.org");
        //static string POOL = "SAOT4K2SGA4F73EVIILQDH6OHWITZHSRDE72LBEMABPAKT3P2TNTCZ7E";
        static string POOL = "SBPY5DMF5NCQ7OQ4QTYJRWQKILJ6PACEF6S373J6QS7RPW4OKDKFCLLN";
        static int stream_count = 0;
        static TraceWriter log;
        static bool inflation_found = false;
        //public static readonly string _getStreamKey = "";
        //public static readonly string _getSreamSecret = "";

        [FunctionName("InflationStreamFunction")]
        public static Task RunAsync([TimerTrigger("0 */5 * * * *")] TimerInfo myTimer, TraceWriter Log)
        {
            log = Log;
            log.Info(DateTime.UtcNow.ToString());
            Network.UseTestNetwork();

            //Streams are Maybe fixed? in this API until a resolution is found for the HttpClient issue
            log.Info("-- Streaming All New Operations On The Network --");

            server_test.Operations
                .Cursor("now")
                .Stream(async (sender, response) => { await ShowOperationResponse(response); })
                .Connect();
            return null;
        }

        public static async Task ShowOperationResponse(OperationResponse op)
        {
            if (inflation_found)
            {
                return;
            }
            if (op is CreateAccountOperationResponse)
            {
                log.Info($"Hey I'm a Create Account Operation: {op.Id}");
            }
            else if (op is PaymentOperationResponse)
            {
                log.Info($"Hey I'm a Payment Operation: {op.Id}");
            }
            else if (op is AllowTrustOperationResponse)
            {
                log.Info($"Hey I'm an Allow Trust Operation: {op.Id}");
            }
            else if (op is ChangeTrustOperationResponse)
            {
                log.Info($"Hey I'm a Change Trust Operation: {op.Id}");
            }
            else if (op is SetOptionsOperationResponse)
            {
                log.Info($"Hey I'm a Set Options Operation: {op.Id}");
            }
            else if (op is AccountMergeOperationResponse)
            {
                log.Info($"Hey I'm an Account Merge Operation: {op.Id}");
            }
            else if (op is ManageOfferOperationResponse)
            {
                log.Info($"Hey I'm a Manage Offer Operation: {op.Id}");
            }
            else if (op is PathPaymentOperationResponse)
            {
                log.Info($"Hey I'm a Path Payment Operation: {op.Id}");
            }
            else if (op is CreatePassiveOfferOperationResponse)
            {
                log.Info($"Hey I'm a Create Passive Offer Operation: {op.Id}");
            }
            else if (op is InflationOperationResponse)
            {
                log.Info("$Hey I'm an Inflation Operation: {op.Id}");
                var effects = await server_test.Effects.ForOperation(op.Id).Execute();
                //Page<EffectResponse> effects = await server.Effects.ForOperation(op.Id).Execute();
                if (effects.Records != null && effects.Records.Count > 0)
                {
                    foreach (EffectResponse record in effects.Records)
                    {
                        //CHECK IF THE POOL ACCOUNT IS THERE
                        if (record.Account.AccountId == "GCFXD4OBX4TZ5GGBWIXLIJHTU2Z6OWVPYYU44QSKCCU7P2RGFOOHTEST")
                        {
                            inflation_found = true;
                            if (record is AccountCreditedEffectResponse)
                            {
                                log.Info($"Pool {record.Account.AccountId} was credited with { (record as AccountCreditedEffectResponse).Amount}XLM");
                                //InflationAmount = double.Parse((record as AccountCreditedEffectResponse).Amount);
                                Payout(decimal.Parse((record as AccountCreditedEffectResponse).Amount));
                            }
                        }
                    }
                    if (!inflation_found)
                    {
                        log.Info("Looks like the pool didn't earn inflation this week.");
                        log.Info($"See all pools here: https://horizon.stellar.org/operations/{op.Id}/effects");
                    }
                }
                else
                {
                    log.Info("It's not time for Inflation Yet");
                }
            }
            else if (op is ManageDataOperationResponse)
            {
                log.Info("Hey I'm a Manage Data Operation: {op.Id}");
            }
            //log.Info($"Id: {op.Id}, Source: {op.SourceAccount.AccountId}");

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

                //if (batch == 0)
                //{
                //    BatchTransaction = new Transaction.Builder(PoolAccount);
                //}
                if (batch < 100)
                {
                    //CHECK IF AARON'S ACCOUNT IS A VOTER
                    if (account.AccountId == "GC4KCZO3ZCY6HWT2ZLBNHJZWWXIWCNB6TNBQLBFU4SH36MTX2DYCMW6P")
                    {
                        var payout = Math.Round(account.Payout, 7, MidpointRounding.AwayFromZero);
                        var payoutPercent = Math.Round(account.Percentage, 7, MidpointRounding.AwayFromZero);
                        var operation = new PaymentOperation.Builder(KeyPair.FromAccountId(account.AccountId), new AssetTypeNative(), (payout-.0000001m).ToString())
                            .SetSourceAccount(PoolSource)
                            .Build();
                        //var operation = new PaymentOperation.Builder(KeyPair.FromAccountId("GA2YP5PU6OZ5S3Y445DWTPMEYZDL3Z4OTGKV7IZNCIFKKMXJJU34J5EE"), new AssetTypeNative(), (payout - .0000001m).ToString())
                        //    .SetSourceAccount(PoolSource)
                        //    .Build();
                        BatchTransaction.AddOperation(operation);
                        var t = BatchTransaction.AddMemo(Memo.Text($"Rocket:{DateTime.UtcNow.ToShortTimeString()}")).Build();
                        t.Sign(PoolSource);
                        Transactions.Add(t);

                        log.Info($"Paying out: {payout}XLM (%{payoutPercent}) to Account: {account.AccountId}");

                        sum += account.Payout;
                        percentage += account.Percentage;
                    }
                }
                //if (batch == 99 || account.Equals(accounts.LastOrDefault()))
                //{
                //var t = BatchTransaction.AddMemo(Memo.Text($"Rocket:{DateTime.UtcNow.ToShortTimeString()}")).Build();
                //t.Sign(PoolSource);
                //Transactions.Add(t);
                //    BatchTransaction = new Transaction.Builder(PoolAccount);
                //}
                //batch = batch == 99 ? 0 : ++batch;
            }

            log.Info("Submitting batches to the stellar network.");
            foreach (var t in Transactions)
            {
                log.Info($"Batch: {Transactions.IndexOf(t) + 1} submitted");
                var response = await server_test.SubmitTransaction(t);
                log.Info($"Envelope XDR is:");
                log.Info($"{response.EnvelopeXdr}");
            }


            var TotalPayoutPercentgepercent = Math.Round(sum, 7, MidpointRounding.AwayFromZero);
            var TotalPayout = Math.Round(percentage, 7, MidpointRounding.AwayFromZero);
            log.Info($"Payed out: {sum} (%{percentage})");
            log.Info("Exiting");
            Console.ReadLine();
            //Environment.Exit(0);
        }

        public static decimal GetTotalVotes()
        {
            var connString = "Host=127.0.0.1;Username=postgres;Password=BusterPostgres7979;Database=Core-Testnet";
            decimal totalVotes = 0;
            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                List<VoterAccountResponse> Accounts = new List<VoterAccountResponse>();

                // Retrieve all rows
                var cmd = new NpgsqlCommand("SELECT SUM(balance) FROM accounts WHERE inflationdest = @inflationdest", conn);

                //THIS IS SOME OTHER POOL
                cmd.Parameters.AddWithValue("inflationdest", "GCFXD4OBX4TZ5GGBWIXLIJHTU2Z6OWVPYYU44QSKCCU7P2RGFOOHTEST");

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
            var connString = "Host=127.0.0.1;Username=postgres;Password=BusterPostgres7979;Database=Core-Testnet";

            using (var conn = new NpgsqlConnection(connString))
            {
                conn.Open();
                List<VoterAccountResponse> Accounts = new List<VoterAccountResponse>();

                // Retrieve all rows
                var cmd = new NpgsqlCommand("SELECT accounts.accountid, balance FROM accounts WHERE inflationdest = @inflationdest", conn);

                //THIS IS SOME OTHER POOL
                cmd.Parameters.AddWithValue("inflationdest", "GCFXD4OBX4TZ5GGBWIXLIJHTU2Z6OWVPYYU44QSKCCU7P2RGFOOHTEST");

                using (var reader = cmd.ExecuteReader())
                    while (reader.Read())
                    {

                        VoterAccountResponse account = new VoterAccountResponse(
                            reader.GetString(0),
                            reader.IsDBNull(1) ? 0 : reader.GetFieldValue<decimal>(1),
                            InflationAmount, TotalVotes

                        );
                        Accounts.Add(account);
                    }

                return Accounts;
            }
        }
    }

}
