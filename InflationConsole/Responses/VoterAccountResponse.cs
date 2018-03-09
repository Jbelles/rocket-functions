using System;
using System.Collections.Generic;
using System.Text;
namespace Responses
{
    public class VoterAccountResponse
    {
        public VoterAccountResponse(string Id, decimal Amount, decimal InflationAmount, decimal TotalVotes)
        {
            AccountId = Id;
            Votes = Amount;
            Percentage = Votes / TotalVotes;
            Payout = InflationAmount * Percentage;

        }
        public string AccountId { get; set; }
        public decimal Votes { get; set; }
        public decimal Percentage { get; set; }
        public decimal Payout { get; set; }
    }
}
