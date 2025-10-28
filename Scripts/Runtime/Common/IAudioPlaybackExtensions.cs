using System;

#if FMOD_AUDIO_SYNTAX
namespace RoyTheunissen.AudioSyntax
{
    public static class IAudioPlaybackExtensions
    {
        public static IAudioPlayback SetParameter(this IAudioPlayback playback,
            string name, float value, bool ignoreSeekSpeed = false)
        {
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, value, ignoreSeekSpeed);

            return playback;
        }

        public static IAudioPlayback SetParameter(this IAudioPlayback playback,
            string name, int value, bool ignoreSeekSpeed = false)
        {
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, value, ignoreSeekSpeed);

            return playback;
        }

        public static IAudioPlayback SetParameter(this IAudioPlayback playback,
            string name, bool value, bool ignoreSeekSpeed = false)
        {
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, value ? 1f : 0f, ignoreSeekSpeed);

            return playback;
        }

        public static IAudioPlayback SetParameterLabel(this IAudioPlayback playback,
            string name, string label, bool ignoreSeekSpeed = false)
        {
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameterLabel(name, label, ignoreSeekSpeed);

            return playback;
        }

        public static IAudioPlayback SetParameter<TEnum>(this IAudioPlayback playback,
            string name, TEnum value, bool ignoreSeekSpeed = false)
            where TEnum : struct, Enum
        {
            if (playback is FmodAudioPlayback fmod)
                fmod.SetParameter(name, Convert.ToInt32(value), ignoreSeekSpeed);

            return playback;
        }

        public static IAudioPlayback SetParameters(this IAudioPlayback playback,
            params (string name, float value)[] parameters)
        {
            for (int i = 0; i < parameters.Length; i++)
                playback.SetParameter(parameters[i].name, parameters[i].value);
            return playback;
        }

        public static IAudioPlayback SetVolume(this IAudioPlayback playback, float volume)
        {
            playback.Volume = volume;
            return playback;
        }
    }
}
#endif

