using RoyTheunissen.FMODSyntax;

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for bank to get the path without an out parameter because to make lambda functions easy to write.
    /// </summary>
    public static class BankEditorExtensions 
    {
        public static string GetName(this Bank bank)
        {
            return FmodSyntaxUtilities.GetFilteredNameFromPath(bank.getPath());
        }
    }
}
