namespace Blockchain.Core
{
    public class BlockAddedEventArgs : EventArgs
    {
        public Blocks NewBlock { get; }
        public BlockAddedEventArgs(Blocks newBlock)
        {
            NewBlock = newBlock;
        }
    }
}