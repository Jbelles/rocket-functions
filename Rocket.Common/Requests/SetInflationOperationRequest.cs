using System;

namespace Rocket.Common
{
    public class SetInflationDestinationRequest
    {
        public string AccountId { get; set; }
        public string Seed { get; set; }
        public string InflationDestination { get; set; }
    }
}
