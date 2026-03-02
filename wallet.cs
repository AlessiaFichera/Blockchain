using System.Collections.Generic;

namespace Blockchain.Core
{
    public class WalletRoot
    {   
        public List<WalletAccount>? Accounts { get; set; }
    }

    public class WalletAccount
    {
        public string? Address { get; set; }         
    }
}