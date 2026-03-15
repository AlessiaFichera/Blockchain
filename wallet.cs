using System.Collections.Generic;

namespace Blockchain.Core
{
    public class WalletRoot
    {   
        public required List<string> Addresses { get; set; }
        public int Count { get; set; }
    }

    public class WalletAccount
    {
        public required string Address { get; set; }         
    }
    
}