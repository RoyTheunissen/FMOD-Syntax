namespace RoyTheunissen.AudioSyntax
{
    public static partial class AudioCodeGenerator
    {
        private const string SnapshotsTemplatePath = TemplatePathBase + "Snapshots/";
        private static string SnapshotsScriptPath => ScriptPathBase + "AudioSnapshots.g.cs";
        private static string SnapshotTypesScriptPath => ScriptPathBase + "AudioSnapshotTypes.g.cs";

        private static readonly CodeGenerator snapshotsScriptGenerator =
            new(SnapshotsTemplatePath + "AudioSnapshots.g.cs");

        private static readonly CodeGenerator snapshotTypesScriptGenerator =
            new(SnapshotsTemplatePath + "AudioSnapshotTypes.g.cs");

        private static readonly CodeGenerator snapshotTypeGenerator =
            new(SnapshotsTemplatePath + "FmodSnapshotType.g.cs"); // Currently FMOD-specific

        private static readonly CodeGenerator snapshotFieldsGenerator =
            new(SnapshotsTemplatePath + "FmodSnapshotFields.g.cs"); // Currently FMOD-specific
    }
}