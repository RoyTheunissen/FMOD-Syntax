#if FMOD_AUDIO_SYNTAX

namespace FMOD.Studio
{
    /// <summary>
    /// Extensions for bank to get the path without an out parameter because to make lambda functions easy to write.
    /// </summary>
    public static class BankExtensions 
    {
        public static string getPath(this Bank bank)
        {
            bank.getPath(out string path);
            return path;
        }
    }
}

#endif // FMOD_AUDIO_SYNTAX
