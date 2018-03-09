//Demo Stellar API setup with an example of one use of setoptions operation
// 2-15-2018 Jarret Belles
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rocket.Common;
using stellar_dotnetcore_sdk;
using stellar_dotnetcore_sdk.requests;
using stellar_dotnetcore_sdk.responses;

namespace rocket_wallet_api.Controllers
{
    [Route("api/[controller]")]
    public class StellarController : Controller
    {
        public StellarController()
        {
        }
        // POST api/values
        [HttpPost("InflationDestination")]
        //I DO NOT RECOMMEND SENDING SECRET SEEDS OVER A NETWORK (ESPECIALLY UNENCRYPTED), THIS IS JUST AN EXAMPLE OF HOW YOU MIGHT FORM AN API USING THE SDK
        public async Task<IActionResult> SetInflationDestination([FromBody] SetInflationDestinationRequest request)
        {
            try
            {
                Network.UseTestNetwork();
                var source = KeyPair.FromAccountId(request.AccountId);
                var signer = KeyPair.FromSecretSeed(request.Seed);
                var inflationDestination = KeyPair.FromAccountId(request.InflationDestination);


                //For Livenet use https://horizon.stellar.org
                using (var server = new Server("https://horizon-testnet.stellar.org"))
                {
                    AccountResponse sourceAccount = await server.Accounts.Account(source);

                    var sequenceNumber = sourceAccount.SequenceNumber;
                    var account = new Account(source, sequenceNumber);

                    var operation = new SetOptionsOperation.Builder()
                        .SetInflationDestination(inflationDestination)
                        .SetSourceAccount(source)
                        .Build();

                    var memo = Memo.Text("Sample Memo");

                    Transaction transaction = new Transaction.Builder(account).AddOperation(operation).AddMemo(memo).Build();

                    var transactionXDR = transaction.ToUnsignedEnvelopeXdr();
                    transaction.Sign(signer);
                    var test = transaction.ToEnvelopeXdrBase64();

                    await server.SubmitTransaction(test);
                    return Ok();
                }
            }
            catch(Exception Ex)
            {
                return StatusCode(500, "Something went wrong");
            }
            
        }
    }
}
