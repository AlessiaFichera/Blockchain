using System.Collections.Generic;

namespace Blockchain.Core
{
    public class WalletRoot
    {   
        public List<string>? Addresses { get; set; }
        public int Count { get; set; }
    }

    public class WalletAccount
    {
        public string? Address { get; set; }         
    }
}