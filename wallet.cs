using System.Collections.Generic;

namespace Blockchain.Core
{
    public class WalletRoot
    {   
        // Usiamo '?' perché se il file Go è vuoto, Accounts sarà null
        public List<WalletAccount>? Accounts { get; set; }
    }

    public class WalletAccount
    {
        // Il '?' risolve il warning CS8618 e rende il software più robusto
        public string? Address { get; set; }
        
        // Per i numeri usiamo il valore di default 0.0
        public double Balance { get; set; } = 0.0;          
    }
}