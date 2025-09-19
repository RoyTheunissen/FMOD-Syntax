using UnityEngine;

namespace RoyTheunissen.AudioSyntax
{
    public static class MigrationVersion
    {
        public const int TargetVersion = 1;

        public static int CurrentVersion
        {
            get
            {
                // TODO: Somehow ascertain this from the Scriptable Object despite us not having a reference.
                // I can think of some way to hack it in :)
                // OR
                // We uhhhh, put it in a text file. Maybe that's better.
                return 0;
            }
            set
            {
                // TODO: Save in text file?
            }
        }
    }
}
